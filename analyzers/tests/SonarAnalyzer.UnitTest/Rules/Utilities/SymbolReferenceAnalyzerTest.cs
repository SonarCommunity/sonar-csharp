﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules;
using SonarAnalyzer.UnitTest.Helpers;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class SymbolReferenceAnalyzerTest
    {
        private const string BasePath = @"Utilities\SymbolReferenceAnalyzer\";

        public TestContext TestContext { get; set; }

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Method_PreciseLocation_CS(ProjectType projectType) =>
            Verify("Method.cs", projectType, references =>
            {
                references.Select(x => x.Declaration.StartLine).Should().BeEquivalentTo(new[] { 1, 3, 5, 7 });   // class 'Sample' on line 1, method 'Method' on line 3, method 'method' on line 5 and method 'Go' on line 7
                var methodDeclaration = references.Single(x => x.Declaration.StartLine == 3);
                methodDeclaration.Declaration.Should().BeEquivalentTo(new TextRange { StartLine = 3, EndLine = 3, StartOffset = 16, EndOffset = 22 });
                methodDeclaration.Reference.Should().Equal(new TextRange { StartLine = 9, EndLine = 9, StartOffset = 8, EndOffset = 14 });
            });

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Method_PreciseLocation_VB(ProjectType projectType) =>
            Verify("Method.vb", projectType, references =>
            {
                references.Select(x => x.Declaration.StartLine).Should().BeEquivalentTo(new[] { 1, 3, 6, 10 });

                var procedureDeclaration = references.Single(x => x.Declaration.StartLine == 3);
                procedureDeclaration.Declaration.Should().BeEquivalentTo(new TextRange { StartLine = 3, EndLine = 3, StartOffset = 15, EndOffset = 21 });
                procedureDeclaration.Reference.Should().Equal(
                    new TextRange { StartLine = 11, EndLine = 11, StartOffset = 8, EndOffset = 14 },
                    new TextRange { StartLine = 13, EndLine = 13, StartOffset = 8, EndOffset = 14 });

                var functionDeclaration = references.Single(x => x.Declaration.StartLine == 6);
                functionDeclaration.Declaration.Should().BeEquivalentTo(new TextRange { StartLine = 6, EndLine = 6, StartOffset = 13, EndOffset = 23 });
                functionDeclaration.Reference.Should().Equal(new TextRange { StartLine = 12, EndLine = 12, StartOffset = 8, EndOffset = 18 });
            });

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Event_CS(ProjectType projectType) =>
            Verify("Event.cs", projectType, 6, 5, 9, 10);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Event_VB(ProjectType projectType) =>
            Verify("Event.vb", projectType, 4, 3, 6, 8, 11);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Field_CS(ProjectType projectType) =>
            Verify("Field.cs", projectType, 4, 3, 7, 8);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_MissingDeclaration_CS(ProjectType projectType) =>
            Verify("MissingDeclaration.cs", projectType, 1, 3);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Field_VB(ProjectType projectType) =>
            Verify("Field.vb", projectType, 4, 3, 6, 7);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Tuples_CS(ProjectType projectType) =>
            Verify("Tuples.cs", projectType, 4, 7, 8);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Tuples_VB(ProjectType projectType) =>
            Verify("Tuples.vb", projectType, 4, 4, 8);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_LocalFunction_CS(ProjectType projectType) =>
            Verify("LocalFunction.cs", projectType, 4, 7, 5);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Method_CS(ProjectType projectType) =>
            Verify("Method.cs", projectType, 4, 3, 9);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_NamedType_CS(ProjectType projectType) =>
            Verify("NamedType.cs", projectType, 4, 3, 7);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_NamedType_VB(ProjectType projectType) =>
            Verify("NamedType.vb", projectType, 5, 1, 4, 4, 5);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Parameter_CS(ProjectType projectType) =>
            Verify("Parameter.cs", projectType, 4, 4, 6, 7);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Parameter_VB(ProjectType projectType) =>
            Verify("Parameter.vb", projectType, 4, 4, 5, 6);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Property_CS(ProjectType projectType) =>
            Verify("Property.cs", projectType, 5, 3, 9, 10);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_Property_VB(ProjectType projectType) =>
            Verify("Property.vb", projectType, 5, 3, 6, 7, 8);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_TypeParameter_CS(ProjectType projectType) =>
            Verify("TypeParameter.cs", projectType, 5, 2, 4, 6);

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void Verify_TypeParameter_VB(ProjectType projectType) =>
            Verify("TypeParameter.vb", projectType, 5, 2, 4, 5);

        [TestMethod]
        public void Verify_TokenThreshold() =>
            // In TokenThreshold.cs there are 40009 tokens which is more than the current limit of 40000
            Verify("TokenThreshold.cs", ProjectType.Product, _ => { }, false);

        [DataTestMethod]
        [DataRow("Method.cs", true)]
        [DataRow("SomethingElse.cs", false)]
        public void Verify_UnchangedFiles(string unchangedFileName, bool expectedProtobufIsEmpty)
        {
            var builder = CreateBuilder(ProjectType.Product, "Method.cs").WithAdditionalFilePath(AnalysisScaffolding.CreateSonarProjectConfigWithUnchangedFiles(TestContext, BasePath + unchangedFileName));
            if (expectedProtobufIsEmpty)
            {
                builder.VerifyUtilityAnalyzerProducesEmptyProtobuf();
            }
            else
            {
                builder.VerifyUtilityAnalyzer<SymbolReferenceInfo>(x => x.Should().NotBeEmpty());
            }
        }

#if NET

        [TestMethod]
        public void Verify_Razor() =>
            CreateBuilder(ProjectType.Product, "Razor.razor", "ToDo.cs", "Razor.cshtml")
                .WithConcurrentAnalysis(false)
                .VerifyUtilityAnalyzer<SymbolReferenceInfo>(symbols =>
                    {
                        var orderedSymbols = symbols.OrderBy(x => x.FilePath, StringComparer.InvariantCulture).ToArray();
                        orderedSymbols.Select(x => Path.GetFileName(x.FilePath)).Should().BeEquivalentTo("_Imports.razor", "Razor.razor", "ToDo.cs");
                        orderedSymbols[0].FilePath.Should().EndWith("_Imports.razor");
                        orderedSymbols[1].FilePath.Should().EndWith("Razor.razor");

                        VerifyReferences(orderedSymbols[1].Reference, 9, 13, 4, 6, 20);     // currentCount
                        VerifyReferences(orderedSymbols[1].Reference, 9, 16, 10, 20, 21);   // IncrementAmount
                        VerifyReferences(orderedSymbols[1].Reference, 9, 18, 8);            // IncrementCount
                        VerifyReferences(orderedSymbols[1].Reference, 9, 34, 34);           // x
                        VerifyReferences(orderedSymbols[1].Reference, 9, 37, 28, 34);       // todos
                        VerifyReferences(orderedSymbols[1].Reference, 9, 39, 25);           // AddTodo
                        VerifyReferences(orderedSymbols[1].Reference, 9, 41);               // x
                        VerifyReferences(orderedSymbols[1].Reference, 9, 42);               // y
                        VerifyReferences(orderedSymbols[1].Reference, 9, 44, 41);           // LocalMethod
                    });

#endif

        private void Verify(string fileName, ProjectType projectType, int expectedDeclarationCount, int assertedDeclarationLine, params int[] assertedDeclarationLineReferences) =>
            Verify(fileName, projectType, references => VerifyReferences(references, expectedDeclarationCount, assertedDeclarationLine, assertedDeclarationLineReferences));

        private void Verify(string fileName,
                            ProjectType projectType,
                            Action<IReadOnlyList<SymbolReferenceInfo.Types.SymbolReference>> verifyReference,
                            bool isMessageExpected = true) =>
            CreateBuilder(projectType, fileName)
                .WithAdditionalFilePath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, projectType))
                .VerifyUtilityAnalyzer<SymbolReferenceInfo>(messages =>
                    {
                        messages.Should().HaveCount(isMessageExpected ? 1 : 0);

                        if (isMessageExpected)
                        {
                            var info = messages.Single();
                            info.FilePath.Should().Be(Path.Combine(BasePath, fileName));
                            verifyReference(info.Reference);
                        }
                    });

        private VerifierBuilder CreateBuilder(ProjectType projectType, params string[] fileNames)
        {
            var testRoot = BasePath + TestContext.TestName;
            var language = AnalyzerLanguage.FromPath(fileNames[0]);
            UtilityAnalyzerBase analyzer = language.LanguageName switch
            {
                LanguageNames.CSharp => new TestSymbolReferenceAnalyzer_CS(testRoot, projectType == ProjectType.Test),
                LanguageNames.VisualBasic => new TestSymbolReferenceAnalyzer_VB(testRoot, projectType == ProjectType.Test),
                _ => throw new UnexpectedLanguageException(language)
            };
            return new VerifierBuilder()
                .AddAnalyzer(() => analyzer)
                .AddPaths(fileNames)
                .WithBasePath(BasePath)
                .WithOptions(ParseOptionsHelper.Latest(language))
                .WithProtobufPath(@$"{testRoot}\symrefs.pb");
        }

        private static void VerifyReferences(IReadOnlyList<SymbolReferenceInfo.Types.SymbolReference> references,
                                             int expectedDeclarationCount,
                                             int assertedDeclarationLine,
                                             params int[] assertedDeclarationLineReferences)
        {
            references.Where(x => x.Declaration is not null).Should().HaveCount(expectedDeclarationCount);
            references.Single(x => x.Declaration.StartLine == assertedDeclarationLine).Reference.Select(x => x.StartLine)
                      .Should().BeEquivalentTo(assertedDeclarationLineReferences);
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private sealed class TestSymbolReferenceAnalyzer_CS : CS.SymbolReferenceAnalyzer
        {
            public TestSymbolReferenceAnalyzer_CS(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }

        private sealed class TestSymbolReferenceAnalyzer_VB : VB.SymbolReferenceAnalyzer
        {
            public TestSymbolReferenceAnalyzer_VB(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }
    }
}
