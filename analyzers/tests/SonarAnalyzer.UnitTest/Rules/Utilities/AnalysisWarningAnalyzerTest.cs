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
using SonarAnalyzer.CFG.Helpers;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules;

[TestClass]
public class AnalysisWarningAnalyzerTest
{
    public TestContext TestContext { get; set; }

    [DataTestMethod]
    [DataRow(LanguageNames.CSharp, true)]
    [DataRow(LanguageNames.CSharp, false)]
    [DataRow(LanguageNames.VisualBasic, true)]
    [DataRow(LanguageNames.VisualBasic, false)]
    public void SupportedRoslyn(string languageName, bool isAnalyzerEnabled)
    {
        var expectedPath = ExecuteAnalyzer(languageName, isAnalyzerEnabled, RoslynHelper.MinimalSupportedMajorVersion); // Using production value that is lower than our UT Roslyn version
        File.Exists(expectedPath).Should().BeFalse("Analysis warning file should not be generated.");
    }

    [DataTestMethod]
    [DataRow(LanguageNames.CSharp)]
    [DataRow(LanguageNames.VisualBasic)]
    public void OldRoslyn(string languageName)
    {
        var expectedPath = ExecuteAnalyzer(languageName, true, 1000);  // Requiring too high Roslyn version => we're under unsupported scenario
        File.Exists(expectedPath).Should().BeTrue();
        File.ReadAllText(expectedPath).Should().Be("""[{"text": "Analysis using MsBuild 14 and 15 build tools is deprecated. Please update your pipeline to MsBuild 16 or higher."}]""");

        // Lock file and run it for 2nd time
        using var lockedFile = new FileStream(expectedPath, FileMode.Open, FileAccess.Write, FileShare.None);
        ExecuteAnalyzer(languageName, true, 1000).Should().Be(expectedPath, "path should be reused and analyzer should not fail");
    }

    [DataTestMethod]
    [DataRow(LanguageNames.CSharp)]
    [DataRow(LanguageNames.VisualBasic)]
    public void FileExceptions_AreIgnored(string languageName)
    {
        // This will not create the output directory, causing an exception in the File.WriteAllText(...)
        var expectedPath = ExecuteAnalyzer(languageName, true, 1000, false);  // Requiring too high Roslyn version => we're under unsupported scenario
        File.Exists(expectedPath).Should().BeFalse();
    }

    private string ExecuteAnalyzer(string languageName, bool isAnalyzerEnabled, int minimalSupportedRoslynVersion, bool createDirectory = true)
    {
        var language = AnalyzerLanguage.FromName(languageName);
        var analysisOutPath = TestHelper.TestPath(TestContext, @$"{languageName}\.sonarqube\out");
        var projectOutPath = Path.GetFullPath(Path.Combine(analysisOutPath, "0", "output-language"));
        if (createDirectory)
        {
            Directory.CreateDirectory(analysisOutPath);
        }
        UtilityAnalyzerBase analyzer = language.LanguageName switch
        {
            LanguageNames.CSharp => new TestAnalysisWarningAnalyzer_CS(isAnalyzerEnabled, minimalSupportedRoslynVersion, projectOutPath),
            LanguageNames.VisualBasic => new TestAnalysisWarningAnalyzer_VB(isAnalyzerEnabled, minimalSupportedRoslynVersion, projectOutPath),
            _ => throw new UnexpectedLanguageException(language)
        };
        new VerifierBuilder().AddAnalyzer(() => analyzer).AddSnippet(string.Empty).VerifyNoIssueReported(); // Nothing to analyze, just make it run
        return Path.Combine(analysisOutPath, "AnalysisWarnings.MsBuild.json");
    }

    private sealed class TestAnalysisWarningAnalyzer_CS : CS.AnalysisWarningAnalyzer
    {
        protected override int MinimalSupportedRoslynVersion { get; }

        public TestAnalysisWarningAnalyzer_CS(bool isAnalyzerEnabled, int minimalSupportedRoslynVersion, string outPath)
        {
            IsAnalyzerEnabled = isAnalyzerEnabled;
            MinimalSupportedRoslynVersion = minimalSupportedRoslynVersion;
            OutPath = outPath;
        }
    }

    private sealed class TestAnalysisWarningAnalyzer_VB : VB.AnalysisWarningAnalyzer
    {
        protected override int MinimalSupportedRoslynVersion { get; }

        public TestAnalysisWarningAnalyzer_VB(bool isAnalyzerEnabled, int minimalSupportedRoslynVersion, string outPath)
        {
            IsAnalyzerEnabled = isAnalyzerEnabled;
            MinimalSupportedRoslynVersion = minimalSupportedRoslynVersion;
            OutPath = outPath;
        }
    }
}
