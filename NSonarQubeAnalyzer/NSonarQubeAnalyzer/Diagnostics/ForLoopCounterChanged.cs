﻿namespace NSonarQubeAnalyzer.Diagnostics
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForLoopCounterChanged : DiagnosticsRule
    {
        internal const string DiagnosticId = "S127";
        internal const string Description = "A loop's counter should not be assigned within the loop body";
        internal const string MessageFormat = "Refactor the code to avoid updating the loop counter \"{0}\" within the loop body.";
        internal const string Category = "SonarQube";
        internal const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, Severity, true);

        /// <summary>
        /// Rule ID
        /// </summary>
        public override string RuleId
        {
            get
            {
                return "S127";
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private class SideEffectExpression
        {
            public IImmutableList<SyntaxKind> Kinds;
            public Func<SyntaxNode, SyntaxNode> AffectedExpression;
        }

        private static readonly IImmutableList<SideEffectExpression> SideEffectExpressions = ImmutableArray.Create(
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(SyntaxKind.PreIncrementExpression, SyntaxKind.PreDecrementExpression),
                AffectedExpression = node => ((PrefixUnaryExpressionSyntax)node).Operand
            },
            new SideEffectExpression
            {
                Kinds = ImmutableArray.Create(SyntaxKind.PostIncrementExpression, SyntaxKind.PostDecrementExpression),
                AffectedExpression = node => ((PostfixUnaryExpressionSyntax)node).Operand
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
                AffectedExpression = node => ((AssignmentExpressionSyntax)node).Left
            });

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    ForStatementSyntax forNode = (ForStatementSyntax)c.Node;

                    var loopCounters = LoopCounters(forNode, c.SemanticModel);

                    foreach (var affectedExpression in AffectedExpressions(forNode.Statement))
                    {
                        var symbol = c.SemanticModel.GetSymbolInfo(affectedExpression).Symbol;
                        if (symbol != null && loopCounters.Contains(symbol))
                        {
                            c.ReportDiagnostic(Diagnostic.Create(Rule, affectedExpression.GetLocation(), symbol.OriginalDefinition.Name));
                        }
                    }
                },
                SyntaxKind.ForStatement);
        }

        private static IEnumerable<ISymbol> LoopCounters(ForStatementSyntax node, SemanticModel semanticModel)
        {
            var declaredVariables = node.Declaration == null ?
                Enumerable.Empty<ISymbol>() :
                node.Declaration.Variables
                .Select(v => semanticModel.GetDeclaredSymbol(v));

            var initializedVariables = node.Initializers
                .Where(i => i.IsKind(SyntaxKind.SimpleAssignmentExpression))
                .Select(i => semanticModel.GetSymbolInfo(((AssignmentExpressionSyntax)i).Left).Symbol);

            return declaredVariables.Union(initializedVariables);
        }

        private static IEnumerable<SyntaxNode> AffectedExpressions(SyntaxNode node)
        {
            return node
                .DescendantNodesAndSelf()
                .Where(n => SideEffectExpressions.Any(s => s.Kinds.Any(k => n.IsKind(k))))
                .Select(n => SideEffectExpressions.Where(s => s.Kinds.Any(k => n.IsKind(k))).Single().AffectedExpression(n));
        }
    }
}