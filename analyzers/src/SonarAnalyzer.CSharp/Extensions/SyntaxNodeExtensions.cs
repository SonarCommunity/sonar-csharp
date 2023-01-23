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

        public static ControlFlowGraph CreateCfg(this SyntaxNode body, SemanticModel model, CancellationToken cancel) =>
            CfgCache.FindOrCreate(body.Parent, model, cancel);

        public static bool ContainsConditionalConstructs(this SyntaxNode node) =>
            node != null &&
            node.DescendantNodes()
                .Any(descendant => descendant.IsAnyKind(SyntaxKind.IfStatement,
                    SyntaxKind.ConditionalExpression,
                    SyntaxKind.CoalesceExpression,
                    SyntaxKind.SwitchStatement,
                    SyntaxKindEx.SwitchExpression,
                    SyntaxKindEx.CoalesceAssignmentExpression));

        public static object FindConstantValue(this SyntaxNode node, SemanticModel semanticModel) =>
            new CSharpConstantValueFinder(semanticModel).FindConstant(node);

        public static string FindStringConstant(this SyntaxNode node, SemanticModel semanticModel) =>
            FindConstantValue(node, semanticModel) as string;

        public static bool IsPartOfBinaryNegationOrCondition(this SyntaxNode node)
        {
            if (!(node.Parent is MemberAccessExpressionSyntax))
            {
                return false;
            }

            var topNode = node.Parent.GetSelfOrTopParenthesizedExpression();
            if (topNode.Parent?.IsKind(SyntaxKind.BitwiseNotExpression) ?? false)
            {
                return true;
            }

            var current = topNode;
            while (!current.Parent?.IsAnyKind(SyntaxKind.BitwiseNotExpression,
                                              SyntaxKind.IfStatement,
                                              SyntaxKind.WhileStatement,
                                              SyntaxKind.ConditionalExpression,
                                              SyntaxKind.MethodDeclaration,
                                              SyntaxKind.SimpleLambdaExpression) ?? false)
            {
                current = current.Parent;
            }

            return current.Parent switch
            {
                IfStatementSyntax ifStatement => ifStatement.Condition == current,
                WhileStatementSyntax whileStatement => whileStatement.Condition == current,
                ConditionalExpressionSyntax condExpr => condExpr.Condition == current,
                _ => false
            };
        }

        public static string GetDeclarationTypeName(this SyntaxNode node) =>
            node.Kind() switch
            {
                SyntaxKind.ClassDeclaration => "class",
                SyntaxKind.ConstructorDeclaration => "constructor",
                SyntaxKind.DelegateDeclaration => "delegate",
                SyntaxKind.DestructorDeclaration => "destructor",
                SyntaxKind.EnumDeclaration => "enum",
                SyntaxKind.EnumMemberDeclaration => "enum",
                SyntaxKind.EventDeclaration => "event",
                SyntaxKind.EventFieldDeclaration => "event",
                SyntaxKind.FieldDeclaration => "field",
                SyntaxKind.IndexerDeclaration => "indexer",
                SyntaxKind.InterfaceDeclaration => "interface",
                SyntaxKindEx.LocalFunctionStatement => "local function",
                SyntaxKind.MethodDeclaration => "method",
                SyntaxKind.PropertyDeclaration => "property",
                SyntaxKindEx.RecordClassDeclaration => "record",
                SyntaxKindEx.RecordStructDeclaration => "record struct",
                SyntaxKind.StructDeclaration => "struct",
                _ => GetUnknownType(node.Kind())
            };

        // Extracts the expression body from an arrow-bodied syntax node.
        public static ArrowExpressionClauseSyntax ArrowExpressionBody(this SyntaxNode node) =>
            node switch
            {
                MethodDeclarationSyntax a => a.ExpressionBody,
                ConstructorDeclarationSyntax b => b.ExpressionBody(),
                OperatorDeclarationSyntax c => c.ExpressionBody,
                AccessorDeclarationSyntax d => d.ExpressionBody(),
                ConversionOperatorDeclarationSyntax e => e.ExpressionBody,
                _ => null
            };

        public static SyntaxNode RemoveParentheses(this SyntaxNode expression)
        {
            var current = expression;
            while (current is { } && current.IsAnyKind(SyntaxKind.ParenthesizedExpression, SyntaxKindEx.ParenthesizedPattern))
            {
                current = current.IsKind(SyntaxKindEx.ParenthesizedPattern)
                    ? ((ParenthesizedPatternSyntaxWrapper)current).Pattern
                    : ((ParenthesizedExpressionSyntax)current).Expression;
            }
            return current;
        }

        public static SyntaxNode WalkUpParentheses(this SyntaxNode node)
        {
            while (node is not null && node.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                node = node.Parent;
            }
            return node;
        }

        /// <summary>
        /// Finds the syntactic complementing <see cref="SyntaxNode"/> of an assignment with tuples.
        /// <code>
        /// var (a, b) = (1, 2);      // if node is "a", "1" is returned and vice versa.
        /// (var a, var b) = (1, 2);  // if node is "2", "var b" is returned and vice versa.
        /// a = 1;                    // if node is "a", "1" is returned and vice versa.
        /// t = (1, 2);               // if node is "t", "(1, 2)" is returned, if node is "1", "null" is returned.
        /// </code>
        /// <paramref name="node"/> must be an <see cref="ArgumentSyntax"/> of a tuple or some variable designation of a <see cref="SyntaxKindEx.DeclarationExpression"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="SyntaxNode"/> on the other side of the assignment or <see langword="null"/> if <paramref name="node"/> is not
        /// a direct child of the assignment, not part of a tuple, not part of a designation, or no corresponding <see cref="SyntaxNode"/>
        /// can be found on the other side.
        /// </returns>
        public static SyntaxNode FindAssignmentComplement(this SyntaxNode node)
        {
            if (node is { Parent: AssignmentExpressionSyntax assigment })
            {
                return OtherSideOfAssignment(node, assigment);
            }
            // can be either outermost tuple, or DeclarationExpression if 'node' is SingleVariableDesignationExpression
            var outermostParenthesesExpression = node.AncestorsAndSelf()
                .TakeWhile(x => x.IsAnyKind(
                    SyntaxKind.Argument,
                    SyntaxKindEx.TupleExpression,
                    SyntaxKindEx.SingleVariableDesignation,
                    SyntaxKindEx.ParenthesizedVariableDesignation,
                    SyntaxKindEx.DiscardDesignation,
                    SyntaxKindEx.DeclarationExpression))
                .LastOrDefault(x => x.IsAnyKind(SyntaxKindEx.DeclarationExpression, SyntaxKindEx.TupleExpression));
            if ((TupleExpressionSyntaxWrapper.IsInstance(outermostParenthesesExpression) || DeclarationExpressionSyntaxWrapper.IsInstance(outermostParenthesesExpression))
                && outermostParenthesesExpression.Parent is AssignmentExpressionSyntax assignment)
            {
                var otherSide = OtherSideOfAssignment(outermostParenthesesExpression, assignment);
                if (TupleExpressionSyntaxWrapper.IsInstance(otherSide) || DeclarationExpressionSyntaxWrapper.IsInstance(otherSide))
                {
                    var stackFromNodeToOutermost = GetNestingPathFromNodeToOutermost(node);
                    return FindMatchingNestedNode(stackFromNodeToOutermost, otherSide);
                }
                else
                {
                    return null;
                }
            }

            return null;

            static ExpressionSyntax OtherSideOfAssignment(SyntaxNode oneSide, AssignmentExpressionSyntax assignment) =>
                assignment switch
                {
                    { Left: { } left, Right: { } right } when left.Equals(oneSide) => right,
                    { Left: { } left, Right: { } right } when right.Equals(oneSide) => left,
                    _ => null,
                };

            static Stack<PathPosition> GetNestingPathFromNodeToOutermost(SyntaxNode node)
            {
                Stack<PathPosition> pathFromNodeToTheTop = new();
                while (TupleExpressionSyntaxWrapper.IsInstance(node?.Parent)
                    || ParenthesizedVariableDesignationSyntaxWrapper.IsInstance(node?.Parent)
                    || DeclarationExpressionSyntaxWrapper.IsInstance(node?.Parent))
                {
                    if (DeclarationExpressionSyntaxWrapper.IsInstance(node?.Parent) && node is { Parent.Parent: ArgumentSyntax { } argument })
                    {
                        node = argument;
                    }
                    node = node switch
                    {
                        ArgumentSyntax tupleArgument when TupleExpressionSyntaxWrapper.IsInstance(node.Parent) =>
                            PushPathPositionForTuple(pathFromNodeToTheTop, (TupleExpressionSyntaxWrapper)node.Parent, tupleArgument),
                        _ when VariableDesignationSyntaxWrapper.IsInstance(node) && ParenthesizedVariableDesignationSyntaxWrapper.IsInstance(node.Parent) =>
                            PushPathPositionForParenthesizedDesignation(pathFromNodeToTheTop, (ParenthesizedVariableDesignationSyntaxWrapper)node.Parent, (VariableDesignationSyntaxWrapper)node),
                        _ => null,
                    };
                }
                return pathFromNodeToTheTop;
            }

            static SyntaxNode FindMatchingNestedNode(Stack<PathPosition> pathFromOutermostToGivenNode, SyntaxNode outermostParenthesesToMatch)
            {
                var matchedNestedNode = outermostParenthesesToMatch;
                while (matchedNestedNode is not null && pathFromOutermostToGivenNode.Count > 0)
                {
                    if (DeclarationExpressionSyntaxWrapper.IsInstance(matchedNestedNode))
                    {
                        matchedNestedNode = ((DeclarationExpressionSyntaxWrapper)matchedNestedNode).Designation;
                    }
                    var expectedPathPosition = pathFromOutermostToGivenNode.Pop();
                    matchedNestedNode = matchedNestedNode switch
                    {
                        _ when TupleExpressionSyntaxWrapper.IsInstance(matchedNestedNode) => StepDownInTuple((TupleExpressionSyntaxWrapper)matchedNestedNode, expectedPathPosition),
                        _ when ParenthesizedVariableDesignationSyntaxWrapper.IsInstance(matchedNestedNode) =>
                            StepDownInParenthesizedVariableDesignation((ParenthesizedVariableDesignationSyntaxWrapper)matchedNestedNode, expectedPathPosition),
                        _ => null,
                    };
                }
                return matchedNestedNode;
            }

            static SyntaxNode PushPathPositionForTuple(Stack<PathPosition> pathPositions, TupleExpressionSyntaxWrapper tuple, ArgumentSyntax argument)
            {
                pathPositions.Push(new(tuple.Arguments.IndexOf(argument), tuple.Arguments.Count));
                return tuple.SyntaxNode.Parent;
            }

            static SyntaxNode PushPathPositionForParenthesizedDesignation(Stack<PathPosition> pathPositions,
                                                                         ParenthesizedVariableDesignationSyntaxWrapper parenthesizedDesignation,
                                                                         VariableDesignationSyntaxWrapper variable)
            {
                pathPositions.Push(new(parenthesizedDesignation.Variables.IndexOf(variable), parenthesizedDesignation.Variables.Count));
                return parenthesizedDesignation.SyntaxNode;
            }

            static SyntaxNode StepDownInParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntaxWrapper parenthesizedVariableDesignation, PathPosition expectedPathPosition) =>
                parenthesizedVariableDesignation.Variables.Count == expectedPathPosition.TupleLength
                    ? (SyntaxNode)parenthesizedVariableDesignation.Variables[expectedPathPosition.Index]
                    : null;

            static SyntaxNode StepDownInTuple(TupleExpressionSyntaxWrapper tupleExpression, PathPosition expectedPathPosition) =>
                tupleExpression.Arguments.Count == expectedPathPosition.TupleLength
                    ? (SyntaxNode)tupleExpression.Arguments[expectedPathPosition.Index].Expression
                    : null;
        }

        // This is a refactored version of internal Roslyn SyntaxNodeExtensions.IsInExpressionTree
        public static bool IsInExpressionTree(this SyntaxNode node, SemanticModel model)
        {
            return node.AncestorsAndSelf().Any(x => IsExpressionLambda(x) || IsExpressionSelectOrOrder(x) || IsExpressionQuery(x));

            bool IsExpressionLambda(SyntaxNode node) =>
                node is LambdaExpressionSyntax && model.GetTypeInfo(node).ConvertedType.DerivesFrom(KnownType.System_Linq_Expressions_Expression);

            bool IsExpressionSelectOrOrder(SyntaxNode node) =>
                node is SelectOrGroupClauseSyntax or OrderingSyntax && TakesExpressionTree(model.GetSymbolInfo(node));

            bool IsExpressionQuery(SyntaxNode node) =>
                node is QueryClauseSyntax queryClause
                && model.GetQueryClauseInfo(queryClause) is var info
                && (TakesExpressionTree(info.CastInfo) || TakesExpressionTree(info.OperationInfo));

            static bool TakesExpressionTree(SymbolInfo info)
            {
                var symbols = info.Symbol is null ? info.CandidateSymbols : ImmutableArray.Create(info.Symbol);
                return symbols.Any(x => x is IMethodSymbol method && method.Parameters.Length > 0 && method.Parameters[0].Type.DerivesFrom(KnownType.System_Linq_Expressions_Expression));
            }
        }

        private static string GetUnknownType(SyntaxKind kind) =>

#if DEBUG

            throw new System.ArgumentException($"Unexpected type {kind}", nameof(kind));

#else

            "type";

#endif

        private readonly record struct PathPosition(int Index, int TupleLength);

        private sealed class ControlFlowGraphCache : ControlFlowGraphCacheBase
        {
            protected override bool IsLocalFunction(SyntaxNode node) =>
                node.IsKind(SyntaxKindEx.LocalFunctionStatement);

            protected override bool HasNestedCfg(SyntaxNode node) =>
                node.IsAnyKind(SyntaxKindEx.LocalFunctionStatement, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }
    }
}
