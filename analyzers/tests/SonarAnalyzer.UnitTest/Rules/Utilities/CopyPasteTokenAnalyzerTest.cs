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
using System.Runtime.CompilerServices;
using SonarAnalyzer.Common;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CopyPasteTokenAnalyzerTest
    {
        private const string BasePath = @"Utilities\CopyPasteTokenAnalyzer\";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Verify_Unique_CS() =>
            Verify("Unique.cs", info =>
            {
                info.Should().HaveCount(102);
                info.Count(x => x.TokenValue == "$str").Should().Be(9);
                info.Count(x => x.TokenValue == "$num").Should().Be(1);
                info.Count(x => x.TokenValue == "$char").Should().Be(2);
            });

#if NET

        [TestMethod]
        public void Verify_Unique_CSharp11() =>
            Verify("Unique.Csharp11.cs", info =>
            {
                info.Should().HaveCount(155);
                info.Count(x => x.TokenValue == "$str").Should().Be(16);
                info.Count(x => x.TokenValue == "$num").Should().Be(1);
                info.Count(x => x.TokenValue == "$char").Should().Be(2);
            });

#endif

        [TestMethod]
        public void Verify_Unique_VB() =>
            Verify("Unique.vb", info =>
            {
                info.Should().HaveCount(88);
                info.Where(x => x.TokenValue == "$str").Should().HaveCount(3);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(7);
                info.Should().ContainSingle(x => x.TokenValue == "$char");
            });

        [TestMethod]
        public void Verify_Duplicated_CS() =>
            Verify("Duplicated.cs", info =>
            {
                info.Should().HaveCount(39);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
            });

        [TestMethod]
        public void Verify_Duplicated_CS_GlobalUsings() =>
            CreateBuilder(ProjectType.Product, "Duplicated.CSharp10.cs")
                .WithSonarProjectConfigPath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Product))
                .VerifyUtilityAnalyzer<CopyPasteTokenInfo>(messages =>
                    {
                        messages.Should().HaveCount(1);
                        var info = messages.Single();
                        info.FilePath.Should().Be(Path.Combine(BasePath, "Duplicated.CSharp10.cs"));
                        info.TokenInfo.Should().HaveCount(39);
                        info.TokenInfo.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
                    });

        [TestMethod]
        public void Verify_DuplicatedDifferentLiterals_CS() =>
            Verify("DuplicatedDifferentLiterals.cs", info =>
            {
                info.Should().HaveCount(39);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
            });

        [TestMethod]
        public void Verify_NotRunForTestProject_CS() =>
            CreateBuilder(ProjectType.Test, "DuplicatedDifferentLiterals.cs").VerifyUtilityAnalyzerProducesEmptyProtobuf();

        [DataTestMethod]
        [DataRow("Unique.cs")]
        [DataRow("SomethingElse.cs")]
        public void Verify_UnchangedFiles(string unchangedFileName) =>
            CreateBuilder(ProjectType.Product, "Unique.cs")
                .WithSonarProjectConfigPath(AnalysisScaffolding.CreateSonarProjectConfigWithUnchangedFiles(TestContext, BasePath + unchangedFileName))
                .VerifyUtilityAnalyzer<TokenTypeInfo>(x => x.Should().NotBeEmpty());

        private void Verify(string fileName, Action<IReadOnlyList<CopyPasteTokenInfo.Types.TokenInfo>> verifyTokenInfo) =>
            CreateBuilder(ProjectType.Product, fileName)
                .WithSonarProjectConfigPath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Product))
                .VerifyUtilityAnalyzer<CopyPasteTokenInfo>(messages =>
                    {
                        messages.Should().HaveCount(1);
                        var info = messages.Single();
                        info.FilePath.Should().Be(Path.Combine(BasePath, fileName));
                        verifyTokenInfo(info.TokenInfo);
                    });

        private VerifierBuilder CreateBuilder(ProjectType projectType, string fileName)
        {
            var testRoot = BasePath + TestContext.TestName;
            var language = AnalyzerLanguage.FromPath(fileName);
            UtilityAnalyzerBase analyzer = language.LanguageName switch
            {
                LanguageNames.CSharp => new TestCopyPasteTokenAnalyzer_CS(testRoot, projectType == ProjectType.Test),
                LanguageNames.VisualBasic => new TestCopyPasteTokenAnalyzer_VB(testRoot, projectType == ProjectType.Test),
                _ => throw new UnexpectedLanguageException(language)
            };
            return new VerifierBuilder()
                .AddAnalyzer(() => analyzer)
                .AddPaths(fileName)
                .WithBasePath(BasePath)
                .WithOptions(ParseOptionsHelper.Latest(language))
                .WithProtobufPath(@$"{testRoot}\token-cpd.pb");
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private sealed class TestCopyPasteTokenAnalyzer_CS : CS.CopyPasteTokenAnalyzer
        {
            public TestCopyPasteTokenAnalyzer_CS(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }

        private sealed class TestCopyPasteTokenAnalyzer_VB : VB.CopyPasteTokenAnalyzer
        {
            public TestCopyPasteTokenAnalyzer_VB(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }
    }
}
