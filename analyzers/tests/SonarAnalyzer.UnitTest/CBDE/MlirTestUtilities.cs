﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.CBDE;
using SonarAnalyzer.CFG;
using SonarAnalyzer.CFG.Sonar;

namespace SonarAnalyzer.UnitTest.CBDE
{
    public static class MlirTestUtilities
    {
        private static readonly string CbdeDialectCheckerPath =
            Path.Combine(
                Path.GetDirectoryName(typeof(MlirTestUtilities).Assembly.Location),
                @"CBDE\windows\cbde-dialect-checker.exe");

        public static void CheckExecutableExists() =>
            Assert.IsTrue(File.Exists(CbdeDialectCheckerPath),
                $"We need cbde-dialect-checker.exe to validate the generated IR, searched in path {CbdeDialectCheckerPath}");

        public static string ValidateCodeGeneration(string code, string testName, bool withLoc)
        {
            var locPath = withLoc ? ".loc" : string.Empty;
            var path = Path.Combine(Path.GetTempPath(), $"csharp.{testName}{locPath}.mlir");
            using (var writer = new StreamWriter(path))
            {
                ExportAllMethods(code, writer, withLoc);
            }
            return ValidateIR(path);
        }

        public static void ValidateCodeGeneration(string code, string testName)
        {
            ValidateCodeGeneration(code, testName, false);
            ValidateCodeGeneration(code, testName, true);
        }

        public static void ValidateWithReference(string code, string expected, string testName)
        {
            var actual = ValidateCodeGeneration(code, testName, false);
            var trimmedExpected = expected.Trim('\n', '\r', ' ', '\t').Replace("\r\n", "\n");
            var trimmedActual = actual.Trim('\n', '\r', ' ', '\t');
            var expectedLines = trimmedExpected.Split('\n');
            var actualLines = trimmedActual.Split('\n');
            var maxLines = Math.Min(expectedLines.Length, actualLines.Length);
            Console.WriteLine("Expected:");
            Console.WriteLine(trimmedExpected);
            Console.WriteLine();
            Console.WriteLine("Actual:");
            Console.WriteLine(trimmedActual);
            for (var i = 0; i < maxLines; ++i)
            {
                if (expectedLines[i] != actualLines[i])
                {
                    Assert.Fail($"First different line: {i}\nExpected: \'{expectedLines[i]}\'\nActual:   \'{actualLines[i]}\'");
                }
            }
            Assert.AreEqual(trimmedExpected.Trim(), trimmedActual.Trim());
        }

        public static IControlFlowGraph GetCfgForMethod(string code, string methodName)
        {
            (var method, var semanticModel) = TestHelper.CompileCS(code).GetMethod(methodName);
            return CSharpControlFlowGraph.Create(method.Body, semanticModel);
        }

        public static string GetCfgGraph(string code, string methodName) =>
            CfgSerializer.Serialize(GetCfgForMethod(code, methodName), methodName);

        public static void ExportAllMethods(string code, TextWriter writer, bool withLoc)
        {
            var (tree, model) = TestHelper.CompileIgnoreErrorsCS(code);
            var exporterMetrics = new MlirExporterMetrics();
            var exporter = new MlirExporter(writer, model, exporterMetrics, withLoc);
            foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                exporter.ExportFunction(method);
            }
        }

        private static string ValidateIR(string path)
        {
            var pi = new ProcessStartInfo
            {
                FileName = CbdeDialectCheckerPath,
                Arguments = '"' + path + '"',
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var p = Process.Start(pi);
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Assert.Fail(p.StandardError.ReadToEnd());
                return string.Empty;
            }
            else
            {
                return p.StandardOutput.ReadToEnd();
            }
        }
    }
}
