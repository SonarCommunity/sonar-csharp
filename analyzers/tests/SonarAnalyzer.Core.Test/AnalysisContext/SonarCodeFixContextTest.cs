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

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.AnalysisContext;

namespace SonarAnalyzer.Core.Test.AnalysisContext;

[TestClass]
public class SonarCodeFixContextTest
{
    [TestMethod]
    public async Task SonarCodeFixContext_Properties_ReturnRoslynCodeFixContextProperties()
    {
        var document = CreateProject().FindDocument("MyFile.cs");
        var tree = await document.GetSyntaxTreeAsync();
        var literal = tree.GetRoot().DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Single();
        var cancel = new CancellationToken(true);
        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("1", "title", "format", "category", DiagnosticSeverity.Hidden, false), literal.GetLocation());
        var sonarCodefix = new SonarCodeFixContext(new CodeFixContext(document, diagnostic, (_, _) => { }, cancel));

        sonarCodefix.CancellationToken.Should().Be(cancel);
        sonarCodefix.Document.Should().Be(document);
        sonarCodefix.Diagnostics.Should().Contain(diagnostic);
        sonarCodefix.Span.Should().Be(new TextSpan(18, 13));
    }

    [TestMethod]
    public void SonarCodeFixContext_RegisterDocumentCodeFix_CodeFixRegistered()
    {
        var isActionRegistered = false;
        var document = CreateProject().FindDocument("MyFile.cs");
        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("1", "title", "format", "category", DiagnosticSeverity.Hidden, false), Location.None);
        var sonarCodefix = new SonarCodeFixContext(new CodeFixContext(document, diagnostic, (_, _) => isActionRegistered = true, CancellationToken.None));
        sonarCodefix.RegisterCodeFix("Title", _ => Task.FromResult(document), [diagnostic]);

        isActionRegistered.Should().BeTrue();
    }

    [TestMethod]
    public void SonarCodeFixContext_RegisterSolutionCodeFix_CodeFixRegistered()
    {
        var isActionRegistered = false;
        var document = CreateProject().FindDocument("MyFile.cs");
        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("1", "title", "format", "category", DiagnosticSeverity.Hidden, false), Location.None);
        var sonarCodefix = new SonarCodeFixContext(new CodeFixContext(document, diagnostic, (_, _) => isActionRegistered = true, CancellationToken.None));
        sonarCodefix.RegisterCodeFix("Title", _ => Task.FromResult(new AdhocWorkspace().CurrentSolution), [diagnostic]);

        isActionRegistered.Should().BeTrue();
    }

    private static ProjectBuilder CreateProject() =>
        SolutionBuilder.Create()
            .AddProject(AnalyzerLanguage.CSharp)
            .AddSnippet(@"Console.WriteLine(""Hello World"")", "MyFile.cs");
}
