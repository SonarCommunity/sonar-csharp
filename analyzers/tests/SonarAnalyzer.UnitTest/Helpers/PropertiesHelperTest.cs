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

using System.IO;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Extensions;

namespace SonarAnalyzer.UnitTest.Helpers
{
    [TestClass]
    public class PropertiesHelperTest
    {
        [TestMethod]
        [DataRow("a/SonarLint.xml")] // unix path
        [DataRow("a\\SonarLint.xml")]
        public void ShouldAnalyzeGeneratedCode_WithTrueSetting_ReturnsTrue(string filePath) =>
            GetSetting(SourceText.From(File.ReadAllText("ResourceTests\\AnalyzeGeneratedTrue\\SonarLint.xml")), filePath).Should().BeTrue();

        [TestMethod]
        public void ShouldAnalyzeGeneratedCode_WithFalseSetting_ReturnsFalse() =>
            GetSetting(SourceText.From(File.ReadAllText("ResourceTests\\AnalyzeGeneratedFalse\\SonarLint.xml"))).Should().BeFalse();

        [TestMethod]
        public void ShouldAnalyzeGeneratedCode_WithNoSetting_ReturnsFalse() =>
            GetSetting(SourceText.From(File.ReadAllText("ResourceTests\\NoSettings\\SonarLint.xml"))).Should().BeFalse();

        [TestMethod]
        [DataRow("")]
        [DataRow("this is not an xml")]
        [DataRow(@"<?xml version=""1.0"" encoding=""UTF - 8""?><AnalysisInput><Settings>")]
        public void ShouldAnalyzeGeneratedCode_WithMalformedXml_ReturnsFalse(string sonarLintXmlContent) =>
            GetSetting(SourceText.From(sonarLintXmlContent)).Should().BeFalse();

        [TestMethod]
        public void ShouldAnalyzeGeneratedCode_WithNotBooleanValue_ReturnsFalse() =>
            GetSetting(SourceText.From(File.ReadAllText("ResourceTests\\NotBoolean\\SonarLint.xml"))).Should().BeFalse();

        [TestMethod]
        [DataRow("path//aSonarLint.xml")] // different name
        [DataRow("path//SonarLint.xmla")] // different extension
        public void ShouldAnalyzeGeneratedCode_NonSonarLintXmlPath_ReturnsFalse(string filePath) =>
            GetSetting(SourceText.From(File.ReadAllText("ResourceTests\\AnalyzeGeneratedTrue\\SonarLint.xml")), filePath).Should().BeFalse();

        private static bool GetSetting(SourceText text, string path = "fakePath\\SonarLint.xml")
        {
            var options = AnalysisScaffolding.CreateOptions(path, text);
            return PropertiesHelper.ReadAnalyzeGeneratedCodeProperty(options.ParseSonarLintXmlSettings(), LanguageNames.CSharp);
        }
    }
}
