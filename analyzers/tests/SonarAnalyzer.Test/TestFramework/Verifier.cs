﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Google.Protobuf;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using SonarAnalyzer.Rules;
using SonarAnalyzer.Test.Helpers;

namespace SonarAnalyzer.Test.TestFramework
{
    public class Verifier
    {
        private const string TestCases = "TestCases";

        private static readonly Regex ImportsRegexVB = new(@"^\s*Imports\s+.+$", RegexOptions.Multiline | RegexOptions.RightToLeft);
        private readonly string[] razorSupportedFrameworks = new[]
            {
                "net6.0",
                "net7.0"
            };
        private readonly VerifierBuilder builder;
        private readonly DiagnosticAnalyzer[] analyzers;
        private readonly SonarCodeFix codeFix;
        private readonly AnalyzerLanguage language;
        private readonly string[] onlyDiagnosticIds;

        public Verifier(VerifierBuilder builder)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
            onlyDiagnosticIds = builder.OnlyDiagnostics.Select(x => x.Id).ToArray();
            analyzers = builder.Analyzers.Select(x => x()).ToArray();
            if (!analyzers.Any())
            {
                throw new ArgumentException($"{nameof(builder.Analyzers)} cannot be empty. Use {nameof(VerifierBuilder)}<TAnalyzer> instead or add at least one analyzer using {nameof(builder)}.{nameof(builder.AddAnalyzer)}().");
            }
            if (analyzers.Any(x => x == null))
            {
                throw new ArgumentException("Analyzer instance cannot be null.");
            }
            var allLanguages = analyzers.SelectMany(x => x.GetType().GetCustomAttributes<DiagnosticAnalyzerAttribute>()).SelectMany(x => x.Languages).Distinct().ToArray();
            if (allLanguages.Length > 1)
            {
                throw new ArgumentException($"All {nameof(builder.Analyzers)} must declare the same language in their DiagnosticAnalyzerAttribute.");
            }
            language = AnalyzerLanguage.FromName(allLanguages.Single());
            if (!builder.Paths.Any() && !builder.Snippets.Any())
            {
                throw new ArgumentException($"{nameof(builder.Paths)} cannot be empty. Add at least one file using {nameof(builder)}.{nameof(builder.AddPaths)}() or {nameof(builder.AddSnippet)}().");
            }
            foreach (var path in builder.Paths)
            {
                ValidateExtension(path);
            }
            if (builder.ProtobufPath is not null)
            {
                ValidateSingleAnalyzer(nameof(builder.ProtobufPath));
                if (analyzers.Single() is not UtilityAnalyzerBase)
                {
                    throw new ArgumentException($"{analyzers.Single().GetType().Name} does not inherit from {nameof(UtilityAnalyzerBase)}.");
                }
            }
            if (builder.CodeFix is not null)
            {
                codeFix = builder.CodeFix();
                ValidateCodeFix();
            }
        }

        public void Verify()    // This should never have any arguments
        {
            if (codeFix != null)
            {
                throw new InvalidOperationException($"Cannot use {nameof(Verify)} with {nameof(builder.CodeFix)} set.");
            }
            foreach (var compilation in Compile(builder.ConcurrentAnalysis))
            {
                if (builder.IsRazor)
                {
                    DiagnosticVerifier.VerifyRazor(
                        compilation,
                        analyzers,
                        builder.ErrorBehavior,
                        builder.AdditionalFilePath,
                        onlyDiagnosticIds,
                        builder.Paths.Where(builder.IsRazorOrCshtmlFile).Select(TestCasePath),
                        builder.Snippets.Where(x => builder.IsRazorOrCshtmlFile(x.FileName)));
                }
                else
                {
                    DiagnosticVerifier.Verify(compilation, analyzers, builder.ErrorBehavior, builder.AdditionalFilePath, onlyDiagnosticIds);
                }
            }
        }

        public void VerifyNoIssueReported()    // This should never have any arguments
        {
            foreach (var compilation in Compile(builder.ConcurrentAnalysis))
            {
                foreach (var analyzer in analyzers)
                {
                    DiagnosticVerifier.VerifyNoIssueReported(compilation, analyzer, builder.ErrorBehavior, builder.AdditionalFilePath, onlyDiagnosticIds);
                }
            }
        }

        public void VerifyCodeFix()     // This should never have any arguments
        {
            _ = codeFix ?? throw new InvalidOperationException($"{nameof(builder.CodeFix)} was not set.");
            var document = CreateProject(false).FindDocument(Path.GetFileName(builder.Paths.Single()));
            var codeFixVerifier = new CodeFixVerifier(analyzers.Single(), codeFix, document, builder.CodeFixTitle);
            var fixAllProvider = codeFix.GetFixAllProvider();
            foreach (var parseOptions in builder.ParseOptions.OrDefault(language.LanguageName))
            {
                codeFixVerifier.VerifyWhileDocumentChanges(parseOptions, TestCasePath(builder.CodeFixedPath));
                if (fixAllProvider is not null)
                {
                    codeFixVerifier.VerifyFixAllProvider(fixAllProvider, parseOptions, TestCasePath(builder.CodeFixedPathBatch ?? builder.CodeFixedPath));
                }
            }
        }

        public void VerifyUtilityAnalyzerProducesEmptyProtobuf()     // This should never have any arguments
        {
            foreach (var compilation in Compile(false))
            {
                DiagnosticVerifier.Verify(compilation, analyzers.Single(), CompilationErrorBehavior.Default, builder.AdditionalFilePath);
                new FileInfo(builder.ProtobufPath).Length.Should().Be(0, "protobuf file should be empty");
            }
        }

        public void VerifyUtilityAnalyzer<TMessage>(Action<IReadOnlyList<TMessage>> verifyProtobuf)
            where TMessage : IMessage<TMessage>, new()
        {
            foreach (var compilation in Compile(false))
            {
                DiagnosticVerifier.Verify(compilation, analyzers.Single(), builder.ErrorBehavior, builder.AdditionalFilePath);
                verifyProtobuf(ReadProtobuf().ToList());
            }

            IEnumerable<TMessage> ReadProtobuf()
            {
                using var input = File.OpenRead(builder.ProtobufPath);
                var parser = new MessageParser<TMessage>(() => new TMessage());
                while (input.Position < input.Length)
                {
                    yield return parser.ParseDelimitedFrom(input);
                }
            }
        }

        public IEnumerable<Compilation> Compile(bool concurrentAnalysis) =>
            builder.IsRazor ? CompileRazor() : CreateProject(concurrentAnalysis).Solution.Compile(builder.ParseOptions.ToArray());

        private IEnumerable<Compilation> CompileRazor()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            if (!razorSupportedFrameworks.Contains(builder.RazorFramework))
            {
                throw new InvalidOperationException("Razor compilation is supported only for .NET 6 and .NET 7 frameworks.");
            }

            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (_, failure) => Console.WriteLine(failure.Diagnostic);

            // Copy razor project directory and test case files to a temporary build location
            var tempPath = Path.Combine(Path.GetTempPath(), $"ut-razor-{Guid.NewGuid()}");
            try
            {
                Directory.CreateDirectory(tempPath);

                List<string> languages = new();
                if (builder.ParseOptions != null && builder.ParseOptions.Any())
                {
                    foreach (var parseOption in builder.ParseOptions)
                    {
                        if (parseOption is CSharpParseOptions csharpParseOptions)
                        {
                            languages.Add(GetLanguageVersionReference(csharpParseOptions));
                        }
                    }
                }
                else
                {
                    languages.Add("latest");
                }

                foreach (var lang in languages)
                {
                    Directory.CreateDirectory(Path.Combine(tempPath, lang));
                    // Copy all the files
                    foreach (var file in Directory.GetFiles("TestFramework\\Razor\\EmptyProject").Concat(builder.Paths.Select(TestCasePath)))
                    {
                        File.Copy(file, Path.Combine(tempPath, lang, Path.GetFileName(file)));
                    }
                    foreach (var snippet in builder.Snippets)
                    {
                        File.WriteAllText(Path.Combine(tempPath, lang, snippet.FileName), snippet.Content);
                    }
                    var csprojPath = Path.Combine(tempPath, lang, "EmptyProject.csproj");
                    var destinationXml = XElement.Load(csprojPath);

                    // Set TargetFramework
                    destinationXml.Descendants("TargetFramework").Single().Value = builder.RazorFramework;
                    // Add References
                    var itemGroup = destinationXml.Descendants("ItemGroup").Single();
                    foreach (var reference in builder.References)
                    {
                        itemGroup.Add(new XElement("Reference", new XAttribute("Include", reference.Display)));
                    }
                    // Set LangVersion
                    destinationXml.Descendants("LangVersion").Single().Value = lang;
                    destinationXml.Save(csprojPath);

                    yield return workspace.OpenProjectAsync(csprojPath).Result.GetCompilationAsync().Result;
                }
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        private static string GetLanguageVersionReference(CSharpParseOptions parseOption) =>
            parseOption.LanguageVersion switch
            {
                LanguageVersion.CSharp5 => "5.0",
                LanguageVersion.CSharp6 => "6.0",
                LanguageVersion.CSharp7 => "7.0",
                LanguageVersion.CSharp7_1 => "7.1",
                LanguageVersion.CSharp7_2 => "7.2",
                LanguageVersion.CSharp7_3 => "7.3",
                LanguageVersion.CSharp8 => "8.0",
                LanguageVersion.CSharp9 => "9.0",
                LanguageVersion.CSharp10 => "10.0",
                LanguageVersion.CSharp11 => "11.0",
                _ => "latest"
            };

        private ProjectBuilder CreateProject(bool concurrentAnalysis)
        {
            using var scope = new EnvironmentVariableScope { EnableConcurrentAnalysis = concurrentAnalysis };
            var paths = builder.Paths.Select(TestCasePath).ToArray();
            return SolutionBuilder.Create()
                .AddProject(language, true, builder.OutputKind)
                .AddDocuments(paths)
                .AddDocuments(concurrentAnalysis && builder.AutogenerateConcurrentFiles ? CreateConcurrencyTest(paths) : Enumerable.Empty<string>())
                .AddSnippets(builder.Snippets.ToArray())
                .AddReferences(builder.References);
        }

        private IEnumerable<string> CreateConcurrencyTest(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var newPath = Path.ChangeExtension(path, ".Concurrent" + language.FileExtension);
                var content = File.ReadAllText(path, Encoding.UTF8);
                File.WriteAllText(newPath, InsertConcurrentNamespace(content));
                yield return newPath;
            }
        }

        private string InsertConcurrentNamespace(string content)
        {
            return language.LanguageName switch
            {
                LanguageNames.CSharp => $"namespace AppendedNamespaceForConcurrencyTest {{ {content} {Environment.NewLine}}}",  // Last line can be a comment
                LanguageNames.VisualBasic => content.Insert(ImportsIndexVB(), "Namespace AppendedNamespaceForConcurrencyTest : ") + Environment.NewLine + " : End Namespace",
                _ => throw new UnexpectedLanguageException(language)
            };

            int ImportsIndexVB() =>
                ImportsRegexVB.Match(content) is { Success: true } match ? match.Index + match.Length + 1 : 0;
        }

        private string TestCasePath(string fileName) =>
            Path.GetFullPath(builder.BasePath == null ? Path.Combine(TestCases, fileName) : Path.Combine(TestCases, builder.BasePath, fileName));

        private void ValidateSingleAnalyzer(string propertyName)
        {
            if (builder.Analyzers.Length != 1)
            {
                throw new ArgumentException($"When {propertyName} is set, {nameof(builder.Analyzers)} must contain only 1 analyzer, but {analyzers.Length} were found.");
            }
        }

        private void ValidateExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (!extension.Equals(language.FileExtension, StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path '{path}' doesn't match {language.LanguageName} file extension '{language.FileExtension}'.");
            }
        }

        private void ValidateCodeFix()
        {
            _ = builder.CodeFixedPath ?? throw new ArgumentException($"{nameof(builder.CodeFixedPath)} was not set.");
            ValidateSingleAnalyzer(nameof(builder.CodeFix));
            if (builder.Paths.Length != 1)
            {
                throw new ArgumentException($"{nameof(builder.Paths)} must contain only 1 file, but {builder.Paths.Length} were found.");
            }
            if (builder.Snippets.Any())
            {
                throw new ArgumentException($"{nameof(builder.Snippets)} must be empty when {nameof(builder.CodeFix)} is set.");
            }
            ValidateExtension(builder.CodeFixedPath);
            if (builder.CodeFixedPathBatch is not null)
            {
                ValidateExtension(builder.CodeFixedPathBatch);
            }
            if (codeFix.GetType().GetCustomAttribute<ExportCodeFixProviderAttribute>() is { } codeFixAttribute)
            {
                if (codeFixAttribute.Languages.Single() != language.LanguageName)
                {
                    throw new ArgumentException($"{analyzers.Single().GetType().Name} language {language.LanguageName} does not match {codeFix.GetType().Name} language.");
                }
            }
            else
            {
                throw new ArgumentException($"{codeFix.GetType().Name} does not have {nameof(ExportCodeFixProviderAttribute)}.");
            }
            if (!analyzers.Single().SupportedDiagnostics.Select(x => x.Id).Intersect(codeFix.FixableDiagnosticIds).Any())
            {
                throw new ArgumentException($"{analyzers.Single().GetType().Name} does not support diagnostics fixable by the {codeFix.GetType().Name}.");
            }
        }
    }
}
