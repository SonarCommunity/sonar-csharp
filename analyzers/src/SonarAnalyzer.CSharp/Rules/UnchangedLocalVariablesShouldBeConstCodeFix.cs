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

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class UnchangedLocalVariablesShouldBeConstCodeFix : SonarCodeFix
{
    private const string Title = "Convert to constant.";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(UnchangedLocalVariablesShouldBeConst.DiagnosticId);

    protected override async Task RegisterCodeFixesAsync(SyntaxNode root, CodeFixContext context)
    {
        if (VariableDeclaration(root, context) is { } variable
            && variable.Parent is LocalDeclarationStatementSyntax oldNode)
        {
            var declaration = variable.Type.IsVar
                ? WithExplictType(variable, await context.Document.GetSemanticModelAsync().ConfigureAwait(false))
                : variable;
            var newNode = root.ReplaceNode(oldNode, ConstantDeclaration(declaration));

            context.RegisterCodeFix(
                CodeAction.Create(
                Title,
                token => Task.FromResult(context.Document.WithSyntaxRoot(newNode))),
                context.Diagnostics);
        }
    }

    private static VariableDeclarationSyntax VariableDeclaration(SyntaxNode root, CodeFixContext context) =>
        root.FindNode(context.Diagnostics.First().Location.SourceSpan)?.Parent as VariableDeclarationSyntax;

    private static LocalDeclarationStatementSyntax ConstantDeclaration(VariableDeclarationSyntax declaration)
    {
        var prefix = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword));
        return SyntaxFactory.LocalDeclarationStatement(prefix, declaration);
    }

    private static VariableDeclarationSyntax WithExplictType(VariableDeclarationSyntax declaration, SemanticModel semanticModel)
    {
        var type = SyntaxFactory.IdentifierName(semanticModel.GetTypeInfo(declaration.Type).Type.ToMinimalDisplayString(semanticModel, declaration.GetLocation().SourceSpan.Start));
        return declaration.ReplaceNode(declaration.Type, type);
    }
}
