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

using Moq;
using SonarAnalyzer.AnalysisContext;

namespace SonarAnalyzer.UnitTest.AnalysisContext;

[TestClass]
public class SonarSyntaxNodeReportingContextTest
{
    [TestMethod]
    public void Properties_ArePropagated()
    {
        var cancel = new CancellationToken(true);
        var (tree, model) = TestHelper.CompileCS("// Nothing to see here");
        var node = tree.GetRoot();
        var options = AnalysisScaffolding.CreateOptions();
        var containingSymbol = Mock.Of<ISymbol>();
        var context = new SyntaxNodeAnalysisContext(node, containingSymbol, model, options, _ => { }, _ => true, cancel);
        var sut = new SonarSyntaxNodeReportingContext(AnalysisScaffolding.CreateSonarAnalysisContext(), context);

        sut.Tree.Should().Be(tree);
        sut.Compilation.Should().Be(model.Compilation);
        sut.Options.Should().Be(options);
        sut.Cancel.Should().Be(cancel);
        sut.Node.Should().Be(node);
        sut.SemanticModel.Should().Be(model);
        sut.ContainingSymbol.Should().Be(containingSymbol);
    }

#if NET // .NET Fx shows the message box directly, the exception cannot be caught

    [DataTestMethod]
    [DataRow(true)] // Purpose of this is to make sure that the scaffolding works successfully end-to-end
    [DataRow(false)]
    public void ReportIssue_TreeNotInCompilation_DoNotReport(bool reportOnCorrectTree)
    {
        var analysisContext = AnalysisScaffolding.CreateSonarAnalysisContext();
        var (tree, model) = TestHelper.CompileCS("// Nothing to see here");
        var nodeFromCorrectCompilation = tree.GetRoot();
        var nodeFromAnotherCompilation = TestHelper.CompileCS("// This is another Compilation with another Tree").Tree.GetRoot();
        var rule = AnalysisScaffolding.CreateDescriptorMain();
        var node = tree.GetRoot();
        var wasReported = false;
        var context = new SyntaxNodeAnalysisContext(node, model, AnalysisScaffolding.CreateOptions(), _ => wasReported = true, _ => true, default);
        var sut = new SonarSyntaxNodeReportingContext(analysisContext, context);
        try
        {
            sut.ReportIssue(Diagnostic.Create(rule, (reportOnCorrectTree ? nodeFromCorrectCompilation : nodeFromAnotherCompilation).GetLocation()));
        }
        catch (Exception ex)    // Can't catch internal DebugAssertException
        {
            if (reportOnCorrectTree)
            {
                throw;  // This should not happen => fail the test
            }
            else
            {
                ex.GetType().Name.Should().Be("DebugAssertException");
                ex.Message.Should().Contain("Primary location should be part of the compilation. An AD0001 is raised if this is not the case.");
            }
        }

        wasReported.Should().Be(reportOnCorrectTree);
    }
#endif
}
