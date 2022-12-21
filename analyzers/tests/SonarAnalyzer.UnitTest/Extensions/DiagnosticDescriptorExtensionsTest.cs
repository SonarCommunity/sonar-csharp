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

using SonarAnalyzer.Extensions;

using static SonarAnalyzer.Helpers.DiagnosticDescriptorFactory;

namespace SonarAnalyzer.UnitTest.Extensions;

[TestClass]
public class DiagnosticDescriptorExtensionsTest
{
    private const string MainTag = "MainSourceScope";
    private const string TestTag = "TestSourceScope";
    private const string UtilityTag = "Utility";
    private const string DummyID = "Sxxx";

    [TestMethod]
    public void HasMatchingScope_NoCompilation_IsMatching() =>
        TestHelper.CreateDescriptor(DummyID).HasMatchingScope(null, true, false).Should().BeTrue();

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
        var compilation = new SnippetCompiler("// Nothing to see here").SemanticModel.Compilation;
        var diagnostic = TestHelper.CreateDescriptor(DummyID, ruleTags);
        diagnostic.HasMatchingScope(compilation, projectType == ProjectType.Test, false).Should().Be(expectedResult);
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
        var compilation = new SnippetCompiler("// Nothing to see here").SemanticModel.Compilation;
        var diagnostic = TestHelper.CreateDescriptor(DummyID, ruleTags);
        diagnostic.HasMatchingScope(compilation, projectType == ProjectType.Test, true).Should().Be(expectedResult);
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
        var compilation = new SnippetCompiler("// Nothing to see here").SemanticModel.Compilation;
        var diagnostics = rulesTag.Select(x => TestHelper.CreateDescriptor(DummyID, x));
        diagnostics.Any(x => x.HasMatchingScope(compilation, projectType == ProjectType.Test, false)).Should().Be(expectedResult);
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
        var compilation = new SnippetCompiler("// Nothing to see here").SemanticModel.Compilation;
        var diagnostics = rulesTag.Select(x => TestHelper.CreateDescriptor(DummyID, x));
        diagnostics.Any(x => x.HasMatchingScope(compilation, projectType == ProjectType.Test, true)).Should().Be(expectedResult);
    }
}
