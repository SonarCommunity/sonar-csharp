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
    public sealed class InvocationResolvesToOverrideWithParams : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3220";
        private const string MessageFormat = "Review this call, which partially matches an overload without 'params'. The partial match is '{0}'.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var invocation = (InvocationExpressionSyntax)c.Node;
                    CheckCall(c, invocation, invocation.ArgumentList);
                },
                SyntaxKind.InvocationExpression);

            context.RegisterNodeAction(
                c =>
                {
                    var objectCreation = (ObjectCreationExpressionSyntax)c.Node;
                    CheckCall(c, objectCreation, objectCreation.ArgumentList);
                },
                SyntaxKind.ObjectCreationExpression);
        }

        private static void CheckCall(SonarSyntaxNodeReportingContext context, SyntaxNode node, ArgumentListSyntax argumentList)
        {
            if (argumentList == null
                || argumentList.Arguments.Count == 0
                || context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol invokedMethodSymbol
                || !invokedMethodSymbol.Parameters.Any()
                || !invokedMethodSymbol.Parameters.Last().IsParams
                || IsInvocationWithExplicitArray(argumentList, invokedMethodSymbol, context.SemanticModel))
            {
                return;
            }

            var argumentTypes = argumentList.Arguments
                .Select(arg => context.SemanticModel.GetTypeInfo(arg.Expression))
                .Select(typeInfo => typeInfo.Type ?? typeInfo.ConvertedType) // Action and Func won't always resolve properly with Type
                .ToList();
            if (argumentTypes.Any(type => type is IErrorTypeSymbol))
            {
                return;
            }

            var possibleOtherMethods = invokedMethodSymbol.ContainingType.GetMembers(invokedMethodSymbol.Name)
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsVararg)
                .Where(m => m.MethodKind == invokedMethodSymbol.MethodKind)
                .Where(m => !invokedMethodSymbol.Equals(m))
                .Where(m => m.Parameters.Any() && !m.Parameters.Last().IsParams);

            var otherMethod = possibleOtherMethods.FirstOrDefault(possibleOtherMethod =>
                    ArgumentsMatchParameters(
                        argumentList,
                        argumentTypes.Select(t => t as INamedTypeSymbol).ToList(),
                        possibleOtherMethod,
                        context.SemanticModel));

            if (otherMethod != null)
            {
                context.ReportIssue(Diagnostic.Create(
                    Rule,
                    node.GetLocation(),
                    otherMethod.ToMinimalDisplayString(context.SemanticModel, node.SpanStart)));
            }
        }

        private static bool IsInvocationWithExplicitArray(ArgumentListSyntax argumentList, IMethodSymbol invokedMethodSymbol, SemanticModel semanticModel)
        {
            var methodParameterLookup = new CSharpMethodParameterLookup(argumentList, invokedMethodSymbol);

            var allParameterMatches = new List<IParameterSymbol>();
            foreach (var argument in argumentList.Arguments)
            {
                if (!methodParameterLookup.TryGetSymbol(argument, out var parameter))
                {
                    return false;
                }

                allParameterMatches.Add(parameter);

                if (!parameter.IsParams)
                {
                    continue;
                }

                var argType = semanticModel.GetTypeInfo(argument.Expression).Type;
                if (argType is not IArrayTypeSymbol)
                {
                    return false;
                }
            }

            return allParameterMatches.Count(p => p.IsParams) == 1;
        }

        private static bool ArgumentsMatchParameters(ArgumentListSyntax argumentList, List<INamedTypeSymbol> argumentTypes, IMethodSymbol possibleOtherMethod, SemanticModel semanticModel)
        {
            var methodParameterLookup = new CSharpMethodParameterLookup(argumentList, possibleOtherMethod);

            var matchedParameters = new List<IParameterSymbol>();
            for (var i = 0; i < argumentList.Arguments.Count; i++)
            {
                var argument = argumentList.Arguments[i];
                var argumentType = argumentTypes[i];
                if (!methodParameterLookup.TryGetSymbol(argument, out var parameter))
                {
                    return false;
                }

                if (argumentType == null)
                {
                    if (!parameter.Type.IsReferenceType)
                    {
                        return false;
                    }
                }
                else
                {
                    var conversion = semanticModel.ClassifyConversion(argument.Expression, parameter.Type);
                    if (!conversion.IsImplicit)
                    {
                        return false;
                    }
                }

                matchedParameters.Add(parameter);
            }

            var nonMatchedParameters = possibleOtherMethod.Parameters.Except(matchedParameters);
            return nonMatchedParameters.All(p => p.HasExplicitDefaultValue);
        }
    }
}
