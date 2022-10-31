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

using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.SymbolicExecution.Sonar.Analyzers;
using SonarAnalyzer.UnitTest.Helpers;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class RestrictDeserializedTypesTest
    {
        private readonly VerifierBuilder sonarVerifier = new VerifierBuilder<SymbolicExecutionRunner>().WithBasePath(@"SymbolicExecution\Sonar")
            .AddReferences(AdditionalReferences())
            .WithOnlyDiagnostics(RestrictDeserializedTypes.S5773);

#if NETFRAMEWORK // These serializers are available only when targeting .Net Framework

        [TestMethod]
        public void RestrictDeserializedTypesFormatters_CSharp8()
        {
            using var _ = new AssertIgnoreScope(); // EnsureStackState fails an assertion in this test file
            sonarVerifier.AddPaths("RestrictDeserializedTypes.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();
        }

        [TestMethod]
        public void RestrictDeserializedTypes_DoesNotRaiseIssuesForTestProject_CSharp8() =>
            sonarVerifier.AddPaths("RestrictDeserializedTypes.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .AddTestReference()
                .VerifyNoIssueReported();

        [TestMethod]
        public void RestrictDeserializedTypesJavaScriptSerializer_CSharp8() =>
            sonarVerifier.AddPaths("RestrictDeserializedTypes.JavaScriptSerializer.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();

        [TestMethod]
        public void RestrictDeserializedTypesLosFormatter_CSharp8() =>
            sonarVerifier.AddPaths("RestrictDeserializedTypes.LosFormatter.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();

        private static IEnumerable<MetadataReference> AdditionalReferences() =>
            FrameworkMetadataReference.SystemRuntimeSerialization
            .Union(FrameworkMetadataReference.SystemRuntimeSerializationFormattersSoap)
            .Union(FrameworkMetadataReference.SystemWeb)
            .Union(FrameworkMetadataReference.SystemWebExtensions);

#endif

#if NET

        [TestMethod]
        public void RestrictDeserializedTypesFormatters_CSharp9() =>
            sonarVerifier.AddPaths("RestrictDeserializedTypes.CSharp9.cs")
                .WithTopLevelStatements()
                .Verify();

        private static IEnumerable<MetadataReference> AdditionalReferences() =>
            new[] { CoreMetadataReference.SystemRuntimeSerializationFormatters };

#endif

    }
}
