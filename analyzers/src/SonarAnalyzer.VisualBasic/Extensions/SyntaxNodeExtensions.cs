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

using SonarAnalyzer.CFG.Roslyn;

namespace SonarAnalyzer.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        private static readonly ControlFlowGraphCache CfgCache = new();

        public static ControlFlowGraph CreateCfg(this SyntaxNode block, SemanticModel model, CancellationToken cancel) =>
            CfgCache.FindOrCreate(block, model, cancel);

        public static bool IsPartOfBinaryNegationOrCondition(this SyntaxNode node)
        {
            if (node.Parent is not MemberAccessExpressionSyntax)
            {
                return false;
            }

            var current = node;
            while (current.Parent != null && !current.Parent.IsAnyKind(SyntaxKind.IfStatement, SyntaxKind.WhileStatement))
            {
                current = current.Parent;
            }

            return current.Parent switch
            {
                IfStatementSyntax ifStatement => ifStatement.Condition == current,
                WhileStatementSyntax whileStatement => whileStatement.Condition == current,
                _ => false
            };
        }

        public static object FindConstantValue(this SyntaxNode node, SemanticModel semanticModel) =>
            new VisualBasicConstantValueFinder(semanticModel).FindConstant(node);

        public static string FindStringConstant(this SyntaxNode node, SemanticModel semanticModel) =>
            FindConstantValue(node, semanticModel) as string;

        // This is a refactored version of internal Roslyn SyntaxNodeExtensions.IsInExpressionTree
        public static bool IsInExpressionTree(this SyntaxNode node, SemanticModel model)
        {
            return node.AncestorsAndSelf().Any(x => IsExpressionLambda(x) || IsExpressionQuery(x));

            bool IsExpressionLambda(SyntaxNode node) =>
                node is LambdaExpressionSyntax && model.GetTypeInfo(node).ConvertedType.DerivesFrom(KnownType.System_Linq_Expressions_Expression);

            bool IsExpressionQuery(SyntaxNode node) =>
                node is OrderingSyntax or QueryClauseSyntax or FunctionAggregationSyntax or ExpressionRangeVariableSyntax && TakesExpressionTree(model.GetSymbolInfo(node));

            static bool TakesExpressionTree(SymbolInfo info)
            {
                var symbols = info.Symbol is null ? info.CandidateSymbols : ImmutableArray.Create(info.Symbol);
                return symbols.Any(x => x is IMethodSymbol method && method.Parameters.Length > 0 && method.Parameters[0].Type.DerivesFrom(KnownType.System_Linq_Expressions_Expression));
            }
        }

        private sealed class ControlFlowGraphCache : ControlFlowGraphCacheBase
        {
            protected override bool IsLocalFunction(SyntaxNode node) =>
                false;

            protected override bool HasNestedCfg(SyntaxNode node) =>
                node is LambdaExpressionSyntax;
        }
    }
}
