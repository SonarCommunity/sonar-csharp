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

using SonarAnalyzer.AnalysisContext;

namespace SonarAnalyzer.UnitTest.AnalysisContext;

[TestClass]
public partial class SonarAnalysisContextBaseTest
{
    private const string MainTag = "MainSourceScope";
    private const string TestTag = "TestSourceScope";
    private const string UtilityTag = "Utility";
    private const string DummyID = "Sxxx";

    public TestContext TestContext { get; set; }

    [DataTestMethod]
    [DataRow(true, ProjectType.Product, MainTag)]
    [DataRow(true, ProjectType.Product, MainTag, UtilityTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag, UtilityTag)]
    [DataRow(true, ProjectType.Test, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, UtilityTag)]
    [DataRow(true, ProjectType.Test, MainTag, TestTag)]
    [DataRow(true, ProjectType.Test, MainTag, TestTag, UtilityTag)]
    [DataRow(false, ProjectType.Product, TestTag)]
    [DataRow(false, ProjectType.Product, TestTag, TestTag)]
    [DataRow(false, ProjectType.Test, MainTag)]
    [DataRow(false, ProjectType.Test, MainTag, MainTag)]
    public void HasMatchingScope_SingleDiagnostic_WithOneOrMoreScopes_SonarLint(bool expectedResult, ProjectType projectType, params string[] ruleTags)
    {
        var diagnostic = AnalysisScaffolding.CreateDescriptor(DummyID, ruleTags);
        CreateSut(projectType, false).HasMatchingScope(diagnostic).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow(true, ProjectType.Product, MainTag)]
    [DataRow(true, ProjectType.Product, MainTag, UtilityTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag, UtilityTag)]
    [DataRow(true, ProjectType.Test, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, UtilityTag)]
    [DataRow(true, ProjectType.Test, MainTag, TestTag, UtilityTag)]     // Utility rules with scope Test&Main do run on test code under scanner context.
    [DataRow(false, ProjectType.Test, MainTag, TestTag)]                // Rules with scope Test&Main do not run on test code under scanner context for now.
    [DataRow(false, ProjectType.Product, TestTag)]
    [DataRow(false, ProjectType.Product, TestTag, UtilityTag)]
    [DataRow(false, ProjectType.Product, TestTag, TestTag)]
    [DataRow(false, ProjectType.Test, MainTag)]
    [DataRow(false, ProjectType.Test, MainTag, UtilityTag)]
    [DataRow(false, ProjectType.Test, MainTag, MainTag)]
    public void HasMatchingScope_SingleDiagnostic_WithOneOrMoreScopes_Scanner(bool expectedResult, ProjectType projectType, params string[] ruleTags)
    {
        var diagnostic = AnalysisScaffolding.CreateDescriptor(DummyID, ruleTags);
        CreateSut(projectType, true).HasMatchingScope(diagnostic).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow(true, ProjectType.Product, MainTag, MainTag)]
    [DataRow(true, ProjectType.Product, MainTag, MainTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, MainTag)]
    [DataRow(false, ProjectType.Product, TestTag, TestTag)]
    [DataRow(false, ProjectType.Test, MainTag, MainTag)]
    public void HasMatchingScope_MultipleDiagnostics_WithSingleScope_SonarLint(bool expectedResult, ProjectType projectType, params string[] rulesTag)
    {
        var diagnostics = rulesTag.Select(x => AnalysisScaffolding.CreateDescriptor(DummyID, x));
        CreateSut(projectType, false).HasMatchingScope(diagnostics).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow(true, ProjectType.Product, MainTag, MainTag)]
    [DataRow(true, ProjectType.Product, MainTag, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, TestTag)]
    [DataRow(true, ProjectType.Test, TestTag, MainTag)]    // Rules with scope Test&Main will run to let the Test diagnostics to be detected. ReportDiagnostic should filter Main issues out.
    [DataRow(false, ProjectType.Product, TestTag, TestTag)]
    [DataRow(false, ProjectType.Test, MainTag, MainTag)]
    public void HasMatchingScope_MultipleDiagnostics_WithSingleScope_Scanner(bool expectedResult, ProjectType projectType, params string[] rulesTag)
    {
        var diagnostics = rulesTag.Select(x => AnalysisScaffolding.CreateDescriptor(DummyID, x));
        CreateSut(projectType, true).HasMatchingScope(diagnostics).Should().Be(expectedResult);
    }

    [TestMethod]
    public void ProjectConfiguration_LoadsExpectedValues()
    {
        var options = AnalysisScaffolding.CreateOptions($@"ResourceTests\SonarProjectConfig\Path_Windows\SonarProjectConfig.xml");
        var config = CreateSut(options).ProjectConfiguration();

        config.AnalysisConfigPath.Should().Be(@"c:\foo\bar\.sonarqube\conf\SonarQubeAnalysisConfig.xml");
    }

    [TestMethod]
    public void ProjectConfiguration_UsesCachedValue()
    {
        var options = AnalysisScaffolding.CreateOptions($@"ResourceTests\SonarProjectConfig\Path_Windows\SonarProjectConfig.xml");
        var firstSut = CreateSut(options);
        var secondSut = CreateSut(options);
        var firstConfig = firstSut.ProjectConfiguration();
        var secondConfig = secondSut.ProjectConfiguration();

        secondConfig.Should().BeSameAs(firstConfig);
    }

    [TestMethod]
    public void ProjectConfiguration_WhenFileChanges_RebuildsCache()
    {
        var firstOptions = AnalysisScaffolding.CreateOptions($@"ResourceTests\SonarProjectConfig\Path_Windows\SonarProjectConfig.xml");
        var secondOptions = AnalysisScaffolding.CreateOptions($@"ResourceTests\SonarProjectConfig\Path_Unix\SonarProjectConfig.xml");
        var firstConfig = CreateSut(firstOptions).ProjectConfiguration();
        var secondConfig = CreateSut(secondOptions).ProjectConfiguration();

        secondConfig.Should().NotBeSameAs(firstConfig);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("/foo/bar/does-not-exit")]
    [DataRow("/foo/bar/x.xml")]
    public void ProjectConfiguration_WhenAdditionalFileNotPresent_ReturnsEmptyConfig(string folder)
    {
        var options = AnalysisScaffolding.CreateOptions(folder);
        var config = CreateSut(options).ProjectConfiguration();

        config.AnalysisConfigPath.Should().BeNull();
        config.ProjectPath.Should().BeNull();
        config.FilesToAnalyzePath.Should().BeNull();
        config.OutPath.Should().BeNull();
        config.ProjectType.Should().Be(ProjectType.Unknown);
        config.TargetFramework.Should().BeNull();
    }

    [TestMethod]
    public void ProjectConfiguration_WhenFileIsMissing_ThrowException()
    {
        var sut = CreateSut(AnalysisScaffolding.CreateOptions("ThisPathDoesNotExist\\SonarProjectConfig.xml"));

        sut.Invoking(x => x.ProjectConfiguration())
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("File 'SonarProjectConfig.xml' has been added as an AdditionalFile but could not be read and parsed.");
    }

    [TestMethod]
    public void ProjectConfiguration_WhenInvalidXml_ThrowException()
    {
        var sut = CreateSut(AnalysisScaffolding.CreateOptions($@"ResourceTests\SonarProjectConfig\Invalid_Xml\SonarProjectConfig.xml"));

        sut.Invoking(x => x.ProjectConfiguration())
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("File 'SonarProjectConfig.xml' has been added as an AdditionalFile but could not be read and parsed.");
    }

    private SonarCompilationReportingContext CreateSut(ProjectType projectType, bool isScannerRun) =>
        CreateSut(AnalysisScaffolding.CreateOptions(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, projectType, isScannerRun)));

    private static SonarCompilationReportingContext CreateSut(AnalyzerOptions options) =>
        CreateSut(new SnippetCompiler("// Nothing to see here").SemanticModel.Compilation, options);

    private static SonarCompilationReportingContext CreateSut(Compilation compilation, AnalyzerOptions options)
    {
        var compilationContext = new CompilationAnalysisContext(compilation, options, _ => { }, _ => true, default);
        return new(AnalysisScaffolding.CreateSonarAnalysisContext(), compilationContext);
    }
}
