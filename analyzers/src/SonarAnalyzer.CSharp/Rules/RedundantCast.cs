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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedundantCast : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1905";
        private const string MessageFormat = "Remove this unnecessary cast to '{0}'.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        private static readonly ISet<string> CastIEnumerableMethods = new HashSet<string> { "Cast", "OfType" };

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var castExpression = (CastExpressionSyntax)c.Node;
                    CheckCastExpression(c, castExpression.Expression, castExpression.Type, castExpression.Type.GetLocation());
                },
                SyntaxKind.CastExpression);

            context.RegisterNodeAction(
                c =>
                {
                    var castExpression = (BinaryExpressionSyntax)c.Node;
                    CheckCastExpression(c, castExpression.Left, castExpression.Right,
                        castExpression.OperatorToken.CreateLocation(castExpression.Right));
                },
                SyntaxKind.AsExpression);

            context.RegisterNodeAction(
                CheckExtensionMethodInvocation,
                SyntaxKind.InvocationExpression);
        }

        private static void CheckCastExpression(SonarSyntaxNodeReportingContext context, ExpressionSyntax expression, ExpressionSyntax type, Location location)
        {
            if (expression.IsKind(SyntaxKindEx.DefaultLiteralExpression))
            {
                return;
            }

            var expressionType = context.SemanticModel.GetTypeInfo(expression).Type;
            if (expressionType == null)
            {
                return;
            }

            var castType = context.SemanticModel.GetTypeInfo(type).Type;
            if (castType == null)
            {
                return;
            }

            if (expressionType.Equals(castType))
            {
                context.ReportIssue(Diagnostic.Create(rule, location,
                    castType.ToMinimalDisplayString(context.SemanticModel, expression.SpanStart)));
            }
        }

        private static void CheckExtensionMethodInvocation(SonarSyntaxNodeReportingContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (GetEnumerableExtensionSymbol(invocation, context.SemanticModel) is { } methodSymbol)
            {
                var returnType = methodSymbol.ReturnType;
                if (GetGenericTypeArgument(returnType) is { } castType)
                {
                    if (methodSymbol.Name == "OfType" && CanHaveNullValue(castType))
                    {
                        // OfType() filters 'null' values from enumerables
                        return;
                    }

                    var elementType = GetElementType(invocation, methodSymbol, context.SemanticModel);
                    // Generic types {T} and {T?} are equal and there is no way to access NullableAnnotation field right now
                    // See https://github.com/SonarSource/sonar-dotnet/issues/3273
                    if (elementType != null && elementType.Equals(castType) && string.Equals(elementType.ToString(), castType.ToString(), System.StringComparison.Ordinal))
                    {
                        var methodCalledAsStatic = methodSymbol.MethodKind == MethodKind.Ordinary;
                        context.ReportIssue(Diagnostic.Create(rule, GetReportLocation(invocation, methodCalledAsStatic),
                            returnType.ToMinimalDisplayString(context.SemanticModel, invocation.SpanStart)));
                    }
                }
            }
        }

        /// If the invocation one of the <see cref="CastIEnumerableMethods"/> extensions, returns the method symbol.
        private static IMethodSymbol GetEnumerableExtensionSymbol(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
            invocation.GetMethodCallIdentifier() is { } methodName
            && CastIEnumerableMethods.Contains(methodName.ValueText)
            && semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol
            && methodSymbol.IsExtensionOn(KnownType.System_Collections_IEnumerable)
                ? methodSymbol
                : null;

        private static ITypeSymbol GetGenericTypeArgument(ITypeSymbol type) =>
            type is INamedTypeSymbol returnType && returnType.Is(KnownType.System_Collections_Generic_IEnumerable_T)
                ? returnType.TypeArguments.Single()
                : null;

        private static bool CanHaveNullValue(ITypeSymbol type) => type.IsReferenceType || type.Name == "Nullable";

        private static Location GetReportLocation(InvocationExpressionSyntax invocation, bool methodCalledAsStatic)
        {
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return invocation.Expression.GetLocation();
            }

            return methodCalledAsStatic
                ? memberAccess.GetLocation()
                : memberAccess.OperatorToken.CreateLocation(invocation);
        }

        private static ITypeSymbol GetElementType(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax collection;
            if (methodSymbol.MethodKind == MethodKind.Ordinary)
            {
                if (!invocation.ArgumentList.Arguments.Any())
                {
                    return null;
                }
                collection = invocation.ArgumentList.Arguments.First().Expression;
            }
            else
            {
                if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                {
                    return null;
                }
                collection = memberAccess.Expression;
            }

            var typeInfo = semanticModel.GetTypeInfo(collection);
            if (typeInfo.Type is INamedTypeSymbol collectionType &&
                collectionType.TypeArguments.Length == 1)
            {
                return collectionType.TypeArguments.First();
            }

            if (typeInfo.Type is IArrayTypeSymbol arrayType &&
                arrayType.Rank == 1) // casting is necessary for multidimensional arrays
            {
                return arrayType.ElementType;
            }

            return null;
        }
    }
}
