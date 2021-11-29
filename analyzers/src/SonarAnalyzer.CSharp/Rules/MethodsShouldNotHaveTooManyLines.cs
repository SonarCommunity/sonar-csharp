﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class MethodsShouldNotHaveTooManyLines
        : MethodsShouldNotHaveTooManyLinesBase<SyntaxKind, BaseMethodDeclarationSyntax>
    {
        private const string TopLevelFunctionMessageFormat = "This top level function body has {0} lines, which is greater than the {1} lines authorized.";

        private static readonly DiagnosticDescriptor DefaultRule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager, false);
        private static readonly DiagnosticDescriptor TopLevelRule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, TopLevelFunctionMessageFormat, RspecStrings.ResourceManager, false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DefaultRule, TopLevelRule);

        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer =>
            CSharpGeneratedCodeRecognizer.Instance;

        protected override SyntaxKind[] SyntaxKinds { get; } =
            {
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DestructorDeclaration
            };

        protected override string MethodKeyword => "methods";

        protected override void Initialize(ParameterLoadingAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    if (c.ContainingSymbol.IsTopLevelMain())
                    {
                        var compilationUnit = (CompilationUnitSyntax)c.Node;
                        var linesCount = CountLines(compilationUnit.GetTopLevelMainBody());

                        if (linesCount > Max)
                        {
                            c.ReportIssue(Diagnostic.Create(TopLevelRule, null, linesCount, Max, MethodKeyword));
                        }
                    }
                },
                SyntaxKind.CompilationUnit);

            base.Initialize(context);
        }

        protected override IEnumerable<SyntaxToken> GetMethodTokens(BaseMethodDeclarationSyntax baseMethodDeclaration) =>
            baseMethodDeclaration.ExpressionBody()?.Expression?.DescendantTokens()
                ?? baseMethodDeclaration.Body?.Statements.SelectMany(s => s.DescendantTokens())
                ?? Enumerable.Empty<SyntaxToken>();

        protected override SyntaxToken? GetMethodIdentifierToken(BaseMethodDeclarationSyntax baseMethodDeclaration) =>
            baseMethodDeclaration.GetIdentifierOrDefault();

        protected override string GetMethodKindAndName(SyntaxToken identifierToken)
        {
            var identifierName = identifierToken.ValueText;
            if (string.IsNullOrEmpty(identifierName))
            {
                return "method";
            }

            var declaration = identifierToken.Parent;
            if (declaration.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return $"constructor '{identifierName}'";
            }

            if (declaration.IsKind(SyntaxKind.DestructorDeclaration))
            {
                return $"finalizer '~{identifierName}'";
            }

            if (declaration is MethodDeclarationSyntax)
            {
                return $"method '{identifierName}'";
            }

            return "method";
        }

        private static long CountLines(IEnumerable<SyntaxNode> nodes) =>
            nodes.SelectMany(x => x.DescendantTokens())
                 .SelectMany(x => x.GetLineNumbers())
                 .Distinct()
                 .LongCount();
    }
}
