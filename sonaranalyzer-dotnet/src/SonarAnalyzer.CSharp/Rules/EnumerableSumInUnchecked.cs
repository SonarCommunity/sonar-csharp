﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class EnumerableSumInUnchecked : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2291";
        private const string MessageFormat = "Refactor this code to handle 'OverflowException'.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var invocation = (InvocationExpressionSyntax)c.Node;
                    var methodSymbol = c.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                    if (!IsSumOnInteger(methodSymbol) ||
                        !IsSumInsideUnchecked(invocation))
                    {
                        return;
                    }

                    var expression = invocation.Expression;
                    var memberAccess = expression as MemberAccessExpressionSyntax;
                    if (memberAccess == null)
                    {
                        return;
                    }

                    c.ReportDiagnosticWhenActive(Diagnostic.Create(rule, memberAccess.Name.GetLocation()));
                },
                SyntaxKind.InvocationExpression);
        }

        private static bool IsSumInsideUnchecked(InvocationExpressionSyntax invocation)
        {
            SyntaxNode current = invocation;
            var parent = current.Parent;
            while (parent != null)
            {
                if (parent is TryStatementSyntax tryStatement &&
                    tryStatement.Block == current)
                {
                    return false;
                }

                if (IsUncheckedExpression(parent) ||
                    IsUncheckedStatement(parent))
                {
                    return true;
                }

                current = parent;
                parent = parent.Parent;
            }
            return false;
        }

        private static bool IsUncheckedExpression(SyntaxNode node)
        {
            var uncheckedExpression = node as CheckedExpressionSyntax;
            return uncheckedExpression != null &&
                uncheckedExpression.IsKind(SyntaxKind.UncheckedExpression);
        }

        private static bool IsUncheckedStatement(SyntaxNode node)
        {
            var uncheckedExpression = node as CheckedStatementSyntax;
            return uncheckedExpression != null &&
                uncheckedExpression.IsKind(SyntaxKind.UncheckedStatement);
        }

        private static bool IsSumOnInteger(IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.Name == "Sum" &&
                methodSymbol.IsExtensionOn(KnownType.System_Collections_Generic_IEnumerable_T) &&
                IsReturnTypeCandidate(methodSymbol);
        }

        private static bool IsReturnTypeCandidate(IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;
            if (returnType.OriginalDefinition.Is(KnownType.System_Nullable_T))
            {
                var nullableType = (INamedTypeSymbol)returnType;
                if (nullableType.TypeArguments.Length != 1)
                {
                    return false;
                }
                returnType = nullableType.TypeArguments[0];
            }

            return returnType.IsAny(DisallowedTypes);
        }

        private static readonly ISet<KnownType> DisallowedTypes = new HashSet<KnownType>
        {
            KnownType.System_Int64,
            KnownType.System_Int32,
            KnownType.System_Decimal
        };
    }
}
