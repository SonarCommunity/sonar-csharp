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
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Common;
using SonarAnalyzer.ControlFlowGraph;
using SonarAnalyzer.ControlFlowGraph.CSharp;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.LiveVariableAnalysis.CSharp;
using SonarAnalyzer.ShimLayer.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class DeadStores : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1854";
        private const string MessageFormat = "Remove this useless assignment to local variable '{0}'.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            // No need to check for ExpressionBody as it can't contain variable assignment

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var declaration = (BaseMethodDeclarationSyntax)c.Node;
                    CheckForDeadStores(declaration.Body, c.SemanticModel.GetDeclaredSymbol(declaration), c);
                },
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DestructorDeclaration,
                SyntaxKind.ConversionOperatorDeclaration,
                SyntaxKind.OperatorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var declaration = (AccessorDeclarationSyntax)c.Node;
                    CheckForDeadStores(declaration.Body, c.SemanticModel.GetDeclaredSymbol(declaration), c);
                },
                SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveAccessorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var declaration = (AnonymousFunctionExpressionSyntax)c.Node;
                    CheckForDeadStores(declaration.Body, c.SemanticModel.GetSymbolInfo(declaration).Symbol, c);
                },
                SyntaxKind.AnonymousMethodExpression,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var declaration = (LocalFunctionStatementSyntaxWrapper)c.Node;
                    CheckForDeadStores(declaration.Body, c.SemanticModel.GetDeclaredSymbol(declaration), c);
                },
                SyntaxKindEx.LocalFunctionStatement);
        }

        private static void CheckForDeadStores(CSharpSyntaxNode node, ISymbol declaration, SyntaxNodeAnalysisContext context)
        {
            if (declaration == null ||
                node == null ||
                // Currently the tuple expressions are not supported and this is known to cause false positives.
                // Please check:
                // - community feedback: https://github.com/SonarSource/sonar-dotnet/issues/3094
                // - implementation ticket: https://github.com/SonarSource/sonar-dotnet/issues/2933
                node.DescendantNodes().AnyOfKind(SyntaxKindEx.TupleExpression) ||
                !CSharpControlFlowGraph.TryGet(node, context.SemanticModel, out var cfg))
            {
                return;
            }

            var lva = CSharpLiveVariableAnalysis.Analyze(cfg, declaration, context.SemanticModel);

            foreach (var block in cfg.Blocks)
            {
                CheckCfgBlockForDeadStores(block, lva.GetLiveOut(block), lva.CapturedVariables, node, declaration, context);
            }
        }

        private static void CheckCfgBlockForDeadStores(Block block, IEnumerable<ISymbol> blockOutState, IEnumerable<ISymbol> excludedLocals, CSharpSyntaxNode node,
            ISymbol declaration, SyntaxNodeAnalysisContext context)
        {
            var lva = new InBlockLivenessAnalysis(block, blockOutState, excludedLocals, node, declaration, context);
            lva.Analyze();
        }

        private class InBlockLivenessAnalysis
        {
            private readonly Block block;
            private readonly IEnumerable<ISymbol> blockOutState;
            private readonly SyntaxNodeAnalysisContext context;
            private readonly ISymbol declaration;
            private readonly IEnumerable<ISymbol> excludedLocals;
            private readonly CSharpSyntaxNode node;

            private static readonly ISet<string> AllowedNumericValues = new HashSet<string> { "-1", "0", "1" };
            private static readonly ISet<string> AllowedStringValues = new HashSet<string> { "" };

            public InBlockLivenessAnalysis(Block block, IEnumerable<ISymbol> blockOutState, IEnumerable<ISymbol> excludedLocals, CSharpSyntaxNode node, ISymbol declaration,
                SyntaxNodeAnalysisContext context)
            {
                this.block = block;
                this.blockOutState = blockOutState;
                this.node = node;
                this.declaration = declaration;
                this.context = context;
                this.excludedLocals = excludedLocals;
            }

            public void Analyze()
            {
                var assignmentLhs = new HashSet<SyntaxNode>();
                var liveOut = new HashSet<ISymbol>(this.blockOutState);

                foreach (var instruction in this.block.Instructions.Reverse())
                {
                    switch (instruction.Kind())
                    {
                        case SyntaxKind.IdentifierName:
                            ProcessIdentifier(instruction, assignmentLhs, liveOut);
                            break;

                        case SyntaxKind.AddAssignmentExpression:
                        case SyntaxKind.SubtractAssignmentExpression:
                        case SyntaxKind.MultiplyAssignmentExpression:
                        case SyntaxKind.DivideAssignmentExpression:
                        case SyntaxKind.ModuloAssignmentExpression:
                        case SyntaxKind.AndAssignmentExpression:
                        case SyntaxKind.ExclusiveOrAssignmentExpression:
                        case SyntaxKind.OrAssignmentExpression:
                        case SyntaxKind.LeftShiftAssignmentExpression:
                        case SyntaxKind.RightShiftAssignmentExpression:
                        case SyntaxKindEx.CoalesceAssignmentExpression:
                            ProcessOpAssignment(instruction, assignmentLhs, liveOut);
                            break;

                        case SyntaxKind.SimpleAssignmentExpression:
                            ProcessSimpleAssignment(instruction, assignmentLhs, liveOut);
                            break;

                        case SyntaxKind.VariableDeclarator:
                            ProcessVariableDeclarator(instruction, liveOut);
                            break;

                        case SyntaxKind.PreIncrementExpression:
                        case SyntaxKind.PreDecrementExpression:
                            ProcessPrefixExpression(instruction, liveOut);
                            break;

                        case SyntaxKind.PostIncrementExpression:
                        case SyntaxKind.PostDecrementExpression:
                            ProcessPostfixExpression(instruction, liveOut);
                            break;
                    }
                }
            }

            private void ProcessIdentifier(SyntaxNode instruction, HashSet<SyntaxNode> assignmentLhs, HashSet<ISymbol> liveOut)
            {
                var identifier = (IdentifierNameSyntax)instruction;
                var symbol = this.context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (!IsSymbolRelevant(symbol))
                {
                    return;
                }

                if (!identifier.GetSelfOrTopParenthesizedExpression().IsInNameOfArgument(this.context.SemanticModel) &&
                    CSharpLiveVariableAnalysis.IsLocalScoped(symbol, this.declaration))
                {
                    if (CSharpLiveVariableAnalysis.IsOutArgument(identifier))
                    {
                        liveOut.Remove(symbol);
                    }
                    else
                    {
                        if (!assignmentLhs.Contains(identifier))
                        {
                            liveOut.Add(symbol);
                        }
                    }
                }
            }

            private void ProcessOpAssignment(SyntaxNode instruction, HashSet<SyntaxNode> assignmentLhs, HashSet<ISymbol> liveOut)
            {
                var assignment = (AssignmentExpressionSyntax)instruction;
                var left = assignment.Left.RemoveParentheses();
                if (left.IsKind(SyntaxKind.IdentifierName))
                {
                    var symbol = this.context.SemanticModel.GetSymbolInfo(left).Symbol;
                    if (!IsSymbolRelevant(symbol))
                    {
                        return;
                    }

                    ReportOnAssignment(assignment, left, symbol, assignmentLhs, liveOut);
                }
            }

            private void ProcessSimpleAssignment(SyntaxNode instruction, HashSet<SyntaxNode> assignmentLhs, HashSet<ISymbol> liveOut)
            {
                var assignment = (AssignmentExpressionSyntax)instruction;
                var left = assignment.Left.RemoveParentheses();
                if (left.IsKind(SyntaxKind.IdentifierName))
                {
                    var symbol = this.context.SemanticModel.GetSymbolInfo(left).Symbol;
                    if (!IsSymbolRelevant(symbol))
                    {
                        return;
                    }

                    ReportOnAssignment(assignment, left, symbol, assignmentLhs, liveOut);
                    liveOut.Remove(symbol);
                }
            }

            private void ProcessVariableDeclarator(SyntaxNode instruction, HashSet<ISymbol> liveOut)
            {
                var declarator = (VariableDeclaratorSyntax)instruction;
                if (context.SemanticModel.GetDeclaredSymbol(declarator) is ILocalSymbol symbol && IsSymbolRelevant(symbol))
                {
                    if (declarator.Initializer != null
                        && !IsAllowedInitialization(declarator.Initializer)
                        && !symbol.IsConst
                        && symbol.RefKind() == RefKind.None
                        && !liveOut.Contains(symbol)
                        && !IsUnusedLocal(symbol)
                        && !new MutedSyntaxWalker(context.SemanticModel, declarator, symbol).IsMuted())
                    {
                        var location = GetFirstLineLocationFromToken(declarator.Initializer.EqualsToken, declarator.Initializer);
                        context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, location, symbol.Name));
                    }
                    liveOut.Remove(symbol);
                }
            }

            private bool IsAllowedInitialization(EqualsValueClauseSyntax initializer) =>
                initializer.Value.IsKind(SyntaxKind.DefaultExpression)
                || IsAllowedObjectInitialization(initializer.Value)
                || IsAllowedBooleanInitialization(initializer.Value)
                || IsAllowedNumericInitialization(initializer.Value)
                || IsAllowedUnaryNumericInitialization(initializer.Value)
                || IsAllowedStringInitialization(initializer.Value);

            private static bool IsAllowedObjectInitialization(ExpressionSyntax expression) =>
                expression.IsNullLiteral();

            private static bool IsAllowedBooleanInitialization(ExpressionSyntax expression) =>
                expression.IsKind(SyntaxKind.TrueLiteralExpression) || expression.IsKind(SyntaxKind.FalseLiteralExpression);

            private static bool IsAllowedNumericInitialization(ExpressionSyntax expression) =>
                expression.IsKind(SyntaxKind.NumericLiteralExpression) && AllowedNumericValues.Contains(((LiteralExpressionSyntax)expression).Token.ValueText);  // 0 or 1

            private static bool IsAllowedUnaryNumericInitialization(ExpressionSyntax expression) =>
                expression.IsAnyKind(SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression)
                && IsAllowedNumericInitialization(((PrefixUnaryExpressionSyntax)expression).Operand); // +1 or -1

            private bool IsAllowedStringInitialization(ExpressionSyntax expression) =>
                (expression.IsKind(SyntaxKind.StringLiteralExpression) && AllowedStringValues.Contains(((LiteralExpressionSyntax)expression).Token.ValueText))
                || expression.IsStringEmpty(context.SemanticModel);

            private bool IsUnusedLocal(ISymbol declaredSymbol)
            {
                return this.node.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .SelectMany(identifier => VariableUnused.GetUsedSymbols(identifier, this.context.SemanticModel))
                    .All(s => !s.Equals(declaredSymbol));
            }

            private void ProcessPrefixExpression(SyntaxNode instruction, HashSet<ISymbol> liveOut)
            {
                var prefixExpression = (PrefixUnaryExpressionSyntax)instruction;
                var parent = prefixExpression.GetSelfOrTopParenthesizedExpression();
                var operand = prefixExpression.Operand.RemoveParentheses();
                if (parent.Parent is ExpressionStatementSyntax
                    && operand.IsKind(SyntaxKind.IdentifierName)
                    && context.SemanticModel.GetSymbolInfo(operand).Symbol is { } symbol
                    && IsSymbolRelevant(symbol)
                    && CSharpLiveVariableAnalysis.IsLocalScoped(symbol, declaration)
                    && !liveOut.Contains(symbol)
                    && !IsMuted(operand))
                {
                    context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, prefixExpression.GetLocation(), symbol.Name));
                }
            }

            private void ProcessPostfixExpression(SyntaxNode instruction, HashSet<ISymbol> liveOut)
            {
                var postfixExpression = (PostfixUnaryExpressionSyntax)instruction;
                var operand = postfixExpression.Operand.RemoveParentheses();
                if (operand.IsKind(SyntaxKind.IdentifierName)
                    && context.SemanticModel.GetSymbolInfo(operand).Symbol is { } symbol
                    && IsSymbolRelevant(symbol)
                    && CSharpLiveVariableAnalysis.IsLocalScoped(symbol, declaration)
                    && !liveOut.Contains(symbol)
                    && !IsMuted(operand))
                {
                    context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, postfixExpression.GetLocation(), symbol.Name));
                }
            }

            private void ReportOnAssignment(AssignmentExpressionSyntax assignment, ExpressionSyntax left, ISymbol symbol, HashSet<SyntaxNode> assignmentLhs, HashSet<ISymbol> outState)
            {
                if (CSharpLiveVariableAnalysis.IsLocalScoped(symbol, declaration)
                    && !outState.Contains(symbol)
                    && !IsMuted(left))
                {
                    var location = GetFirstLineLocationFromToken(assignment.OperatorToken, assignment.Right);
                    context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, location, symbol.Name));
                }

                assignmentLhs.Add(left);
            }

            private bool IsMuted(SyntaxNode node) =>
                new MutedSyntaxWalker(context.SemanticModel, node).IsMuted();

            private static Location GetFirstLineLocationFromToken(SyntaxToken issueStartToken, SyntaxNode wholeIssue)
            {
                var line = GetLineOfToken(issueStartToken, wholeIssue.SyntaxTree);
                var rightSingleLine = line.Span.Intersection(
                    TextSpan.FromBounds(issueStartToken.SpanStart, wholeIssue.Span.End));

                return Location.Create(wholeIssue.SyntaxTree,
                    TextSpan.FromBounds(
                        issueStartToken.SpanStart,
                        rightSingleLine.HasValue ? rightSingleLine.Value.End : issueStartToken.Span.End));
            }

            private bool IsSymbolRelevant(ISymbol symbol)
            {
                return symbol != null &&
                    !this.excludedLocals.Contains(symbol);
            }

            private static TextLine GetLineOfToken(SyntaxToken token, SyntaxTree tree)
            {
                return tree.GetText().Lines[token.GetLocation().GetLineSpan().StartLinePosition.Line];
            }
        }
    }
}
