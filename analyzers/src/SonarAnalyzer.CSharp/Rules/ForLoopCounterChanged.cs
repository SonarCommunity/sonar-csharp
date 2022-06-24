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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ForLoopCounterChanged : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S127";
        private const string MessageFormat = "Do not update the loop counter '{0}' within the loop body.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        private sealed class SideEffectExpression
        {
            public IImmutableList<SyntaxKind> Kinds { get; set; }
            public Func<SyntaxNode, ImmutableArray<SyntaxNode>> AffectedExpressions { get; set; }
        }

        private static readonly IImmutableList<SideEffectExpression> SideEffectExpressions = ImmutableArray.Create(
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(SyntaxKind.PreIncrementExpression, SyntaxKind.PreDecrementExpression),
                AffectedExpressions = node => ImmutableArray.Create<SyntaxNode>(((PrefixUnaryExpressionSyntax)node).Operand)
            },
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(SyntaxKind.PostIncrementExpression, SyntaxKind.PostDecrementExpression),
                AffectedExpressions = node => ImmutableArray.Create<SyntaxNode>(((PostfixUnaryExpressionSyntax)node).Operand)
            },
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(SyntaxKindEx.TupleExpression),
                AffectedExpressions = node => ImmutableArray.Create<SyntaxNode>(((TupleExpressionSyntaxWrapper)node).Arguments.Select(x => x.Expression).ToArray())
            },
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxKind.AddAssignmentExpression,
                    SyntaxKind.SubtractAssignmentExpression,
                    SyntaxKind.MultiplyAssignmentExpression,
                    SyntaxKind.DivideAssignmentExpression,
                    SyntaxKind.ModuloAssignmentExpression,
                    SyntaxKind.AndAssignmentExpression,
                    SyntaxKind.ExclusiveOrAssignmentExpression,
                    SyntaxKind.OrAssignmentExpression,
                    SyntaxKind.LeftShiftAssignmentExpression,
                    SyntaxKind.RightShiftAssignmentExpression),
                AffectedExpressions = node => ImmutableArray.Create<SyntaxNode>(((AssignmentExpressionSyntax)node).Left)
            });

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var forNode = (ForStatementSyntax)c.Node;
                    var loopCounters = LoopCounters(forNode, c.SemanticModel).ToList();

                    foreach (var affectedExpression in AffectedExpressions(forNode.Statement))
                    {
                        var symbol = c.SemanticModel.GetSymbolInfo(affectedExpression).Symbol;

                        if (symbol != null && loopCounters.Contains(symbol))
                        {
                            c.ReportIssue(Diagnostic.Create(Rule, affectedExpression.GetLocation(), symbol.Name));
                        }
                    }
                },
                SyntaxKind.ForStatement);

        private static IEnumerable<ISymbol> LoopCounters(ForStatementSyntax node, SemanticModel semanticModel)
        {
            var declaredVariables = node.Declaration == null
                ? Enumerable.Empty<ISymbol>()
                : node.Declaration.Variables
                    .Select(v => semanticModel.GetDeclaredSymbol(v))
                    .WhereNotNull();

            var initializedVariables = node.Initializers
                .Where(i => i.IsKind(SyntaxKind.SimpleAssignmentExpression))
                .Select(i => semanticModel.GetSymbolInfo(((AssignmentExpressionSyntax)i).Left).Symbol);

            return declaredVariables.Union(initializedVariables);
        }

        private static SyntaxNode[] AffectedExpressions(SyntaxNode node) =>
            node.DescendantNodesAndSelf()
                .Where(n => SideEffectExpressions.Any(s => s.Kinds.Any(n.IsKind)))
                .SelectMany(n => SideEffectExpressions.Single(s => s.Kinds.Any(n.IsKind)).AffectedExpressions(n))
                .ToArray();

        private static ISymbol[] ComputeSymbols(SyntaxNode[] nodes, SemanticModel model) =>
            nodes.Select(x => model.GetSymbolInfo(x).Symbol).ToArray();
        private static ISymbol TupleArgumentSymbolMatchingLoopCounter(TupleExpressionSyntaxWrapper expression,
                                                                      IEnumerable<ISymbol> loopCounters,
                                                                      SemanticModel model)
        {
            var tupleSymbols = expression.Arguments.Select(x => x.Expression)
                .OfType<ExpressionSyntax>()
                .Select(x => model.GetSymbolInfo(x).Symbol).ToArray();
            return loopCounters.Intersect(tupleSymbols).FirstOrDefault();
        }
    }
}
