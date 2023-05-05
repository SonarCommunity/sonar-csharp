﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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

        /// <summary>
        /// Returns the left hand side of a conditional access expression. Returns c in case like a?.b?[0].c?.d.e?.f if d is passed.
        /// </summary>
        /// <remarks>Adapted from <seealso href="https://github.com/dotnet/roslyn/blob/adaa56d/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/SyntaxNodeExtensions.vb#L1003">
        /// Roslyn SyntaxNodeExtensions VB.NET version</seealso></remarks>
        public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
        {
            // Walk upwards based on the grammar/parser rules around ?. expressions (can be seen in
            // LanguageParser.ParseConsequenceSyntax).

            // These are the parts of the expression that the ?... expression can end with.  Specifically:
            //
            //  1.      x?.y.M()            // invocation
            //  2.      x?.y[...];          // element access
            //  3.      x?.y.z              // member access
            //  4.      x?.y                // member binding
            //  5.      x?[y]               // element binding
            if (node.IsAnyMemberAccessExpressionName())
            {
                node = node.Parent;
            }

            // Effectively, if we're on the RHS of the ? we have to walk up the RHS spine first until we hit the first
            // conditional access.
            while (node is InvocationExpressionSyntax or MemberAccessExpressionSyntax or XmlMemberAccessExpressionSyntax
                   && node.Parent is not ConditionalAccessExpressionSyntax)
            {
                node = node.Parent;
            }

            // Two cases we have to care about:
            //
            //      1. a?.b.$$c.d        and
            //      2. a?.b.$$c.d?.e...
            //
            // Note that `a?.b.$$c.d?.e.f?.g.h.i` falls into the same bucket as two.  i.e. the parts after `.e` are
            // lower in the tree and are not seen as we walk upwards.
            //
            //
            // To get the root ?. (the one after the `a`) we have to potentially consume the first ?. on the RHS of the
            // right spine (i.e. the one after `d`).  Once we do this, we then see if that itself is on the RHS of a
            // another conditional, and if so we hten return the one on the left.  i.e. for '2' this goes in this direction:
            //
            //      a?.b.$$c.d?.e           // it will do:
            //           ----->
            //       <---------
            //
            // Note that this only one CAE consumption on both sides.  GetRootConditionalAccessExpression can be used to
            // get the root parent in a case like:
            //
            //      x?.y?.z?.a?.b.$$c.d?.e.f?.g.h.i         // it will do:
            //                    ----->
            //                <---------
            //             <---
            //          <---
            //       <---
            if (node.Parent is ConditionalAccessExpressionSyntax conditional1 && conditional1.Expression == node)
            {
                node = node.Parent;
            }

            if (node.Parent is ConditionalAccessExpressionSyntax conditional2 && conditional2.WhenNotNull == node)
            {
                node = node.Parent;
            }

            return node as ConditionalAccessExpressionSyntax;
        }

        internal static bool IsAnyMemberAccessExpressionName(this SyntaxNode node) =>
            node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node;

        private sealed class ControlFlowGraphCache : ControlFlowGraphCacheBase
        {
            protected override bool IsLocalFunction(SyntaxNode node) =>
                false;

            protected override bool HasNestedCfg(SyntaxNode node) =>
                node is LambdaExpressionSyntax;
        }
    }
}
