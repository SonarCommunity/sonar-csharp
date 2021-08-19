﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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
using Microsoft.CodeAnalysis.Formatting;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class CollectionEmptinessCheckingFixProvider : SonarCodeFixProvider
    {
        internal const string Title = "Use Any() instead";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(CollectionEmptinessChecking.DiagnosticId);

        private static readonly CSharpExpressionNumericConverter ExpressionNumericConverter = new CSharpExpressionNumericConverter();

        protected override Task RegisterCodeFixesAsync(SyntaxNode root, CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var binary = root.FindNode(diagnosticSpan)?.FirstAncestorOrSelf<BinaryExpressionSyntax>();

            if (binary is null)
            {
                return Task.CompletedTask;
            }
            else if (ExpressionNumericConverter.TryGetConstantIntValue(binary.Left, out var left))
            {
                return FixExpression(context, root, binary, binary.Right, left, CSharpFacade.Instance.Syntax.ComparisonKind(binary).Mirror());
            }
            else if (ExpressionNumericConverter.TryGetConstantIntValue(binary.Right, out var right))
            {
                return FixExpression(context, root, binary, binary.Left, right, CSharpFacade.Instance.Syntax.ComparisonKind(binary));
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private static Task FixExpression(
            CodeFixContext context,
            SyntaxNode root,
            BinaryExpressionSyntax binary,
            ExpressionSyntax expression,
            int constant,
            ComparisonKind comparisonKind)
        {
            var countExpression = (InvocationExpressionSyntax)expression;
            var countComparison = comparisonKind.Compare(constant);

            if (countComparison.IsEmptyOrNotEmpty())
            {
                context.RegisterCodeFix(
                CodeAction.Create(
                Title,
                c => Task.FromResult(Simplify(root, binary, countExpression, countComparison, context))),
                context.Diagnostics);
            }
            return Task.CompletedTask;
        }

        private static Document Simplify(
            SyntaxNode root,
            BinaryExpressionSyntax binary,
            InvocationExpressionSyntax countExpression,
            CountComparisonResult countComparison,
            CodeFixContext context)
        {
            var countNode = countExpression.ChildNodes().OfType<MemberAccessExpressionSyntax>().Single();
            var anyNode = countNode.WithName(SyntaxFactory.IdentifierName("Any"));
            ExpressionSyntax anyExpression = countExpression.ReplaceNode(countNode, anyNode);
            if (countComparison == CountComparisonResult.Empty)
            {
                anyExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, anyExpression);
            }
            return context.Document.WithSyntaxRoot(root.ReplaceNode(binary, anyExpression).WithAdditionalAnnotations(Formatter.Annotation));
        }
    }
}
