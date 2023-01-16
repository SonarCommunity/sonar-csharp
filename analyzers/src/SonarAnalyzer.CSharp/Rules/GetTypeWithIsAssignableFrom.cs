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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GetTypeWithIsAssignableFrom : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2219";
        internal const string UseIsOperatorKey = "UseIsOperator";
        internal const string ShouldRemoveGetTypeKey = "ShouldRemoveGetType";
        private const string MessageFormat = "Use {0} instead.";
        private const string MessageIsOperator = "the 'is' operator";
        private const string MessageIsInstanceOfType = "the 'IsInstanceOfType()' method";
        private const string MessageNullCheck = "a 'null' check";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(c =>
                {
                    var invocation = (InvocationExpressionSyntax)c.Node;
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                        && invocation.HasExactlyNArguments(1)
                        && memberAccess.Name.Identifier.ValueText is var methodName
                        && (methodName == "IsInstanceOfType" || methodName == "IsAssignableFrom")
                        && c.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol
                        && methodSymbol.IsInType(KnownType.System_Type))
                    {
                        CheckForIsAssignableFrom(c, memberAccess, methodSymbol, invocation.ArgumentList.Arguments.First().Expression);
                        CheckForIsInstanceOfType(c, memberAccess, methodSymbol);
                    }
                },
                SyntaxKind.InvocationExpression);

            context.RegisterNodeAction(
                c =>
                {
                    var binary = (BinaryExpressionSyntax)c.Node;
                    CheckGetTypeAndTypeOfEquality(c, binary.Left, binary.Right);
                    CheckGetTypeAndTypeOfEquality(c, binary.Right, binary.Left);

                    CheckAsOperatorComparedToNull(c, binary.Left, binary.Right);
                    CheckAsOperatorComparedToNull(c, binary.Right, binary.Left);
                },
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression);

            context.RegisterNodeAction(c =>
                {
                    var isExpression = (BinaryExpressionSyntax)c.Node;
                    if (c.SemanticModel.GetTypeInfo(isExpression.Left).Type is var objectToCast
                        && objectToCast.IsClass()
                        && c.SemanticModel.GetTypeInfo(isExpression.Right).Type is var typeCastTo
                        && typeCastTo.IsClass()
                        && !typeCastTo.Is(KnownType.System_Object)
                        && objectToCast.DerivesOrImplements(typeCastTo))
                    {
                        ReportDiagnostic(c, MessageNullCheck);
                    }
                },
                SyntaxKind.IsExpression);

            context.RegisterNodeAction(c =>
            {
                var isPattern = (IsPatternExpressionSyntaxWrapper)c.Node;
                if (ConstantPatternExpression(isPattern.Pattern) is { } constantExpression)
                {
                    CheckAsOperatorComparedToNull(c, isPattern.Expression, constantExpression);
                }
            },
            SyntaxKindEx.IsPatternExpression);
        }

        private static void CheckAsOperatorComparedToNull(SonarSyntaxNodeAnalysisContext context, ExpressionSyntax sideA, ExpressionSyntax sideB)
        {
            if (sideA.RemoveParentheses().IsKind(SyntaxKind.AsExpression) && sideB.RemoveParentheses().IsKind(SyntaxKind.NullLiteralExpression))
            {
                ReportDiagnostic(context, MessageIsOperator);
            }
        }

        private static void CheckGetTypeAndTypeOfEquality(SonarSyntaxNodeAnalysisContext context, ExpressionSyntax sideA, ExpressionSyntax sideB)
        {
            if (sideA.ToStringContains("GetType")
                && sideB is TypeOfExpressionSyntax sideBTypeOf
                && (sideA as InvocationExpressionSyntax).IsGetTypeCall(context.SemanticModel)
                && context.SemanticModel.GetTypeInfo(sideBTypeOf.Type).Type is { } typeSymbol // Can be null for empty identifier from 'typeof' unfinished syntax
                && typeSymbol.IsSealed
                && !typeSymbol.OriginalDefinition.Is(KnownType.System_Nullable_T))
            {
                ReportDiagnostic(context, MessageIsOperator);
            }
        }

        private static void CheckForIsInstanceOfType(SonarSyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax memberAccess, IMethodSymbol methodSymbol)
        {
            if (methodSymbol.Name == "IsInstanceOfType" && memberAccess.Expression is TypeOfExpressionSyntax)
            {
                ReportDiagnostic(context, MessageIsOperator, useIsOperator: true);
            }
        }

        private static void CheckForIsAssignableFrom(SonarSyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax memberAccess, IMethodSymbol methodSymbol, ExpressionSyntax argument)
        {
            if (methodSymbol.Name == nameof(Type.IsAssignableFrom) && (argument as InvocationExpressionSyntax).IsGetTypeCall(context.SemanticModel))
            {
                if (memberAccess.Expression is TypeOfExpressionSyntax)
                {
                    ReportDiagnostic(context, MessageIsOperator, useIsOperator: true, shouldRemoveGetType: true);
                }
                else
                {
                    ReportDiagnostic(context, MessageIsInstanceOfType, shouldRemoveGetType: true);
                }
            }
        }

        private static ExpressionSyntax ConstantPatternExpression(SyntaxNode node) =>
            node.Kind() switch
            {
                SyntaxKindEx.ConstantPattern => ((ConstantPatternSyntaxWrapper)node).Expression,
                SyntaxKindEx.NotPattern => ConstantPatternExpression(((UnaryPatternSyntaxWrapper)node).Pattern),
                _ => null
            };

        private static void ReportDiagnostic(SonarSyntaxNodeAnalysisContext context, string messageArg, bool useIsOperator = false, bool shouldRemoveGetType = false)
        {
            var properties = ImmutableDictionary<string, string>.Empty
                .Add(UseIsOperatorKey, useIsOperator.ToString())
                .Add(ShouldRemoveGetTypeKey, shouldRemoveGetType.ToString());
            context.ReportIssue(Diagnostic.Create(Rule, context.Node.GetLocation(), properties, messageArg));
        }
    }
}
