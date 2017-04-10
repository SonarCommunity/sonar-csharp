﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class OptionalParameterNotPassedToBaseCall : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3466";
        private const string MessageFormat = "Pass the missing user-supplied parameter value{0} to this 'base' call.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var invocation = (InvocationExpressionSyntax)c.Node;
                    if (!IsOnBase(invocation) ||
                        invocation.ArgumentList == null)
                    {
                        return;
                    }

                    var calledMethod = c.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (calledMethod == null ||
                        !calledMethod.IsVirtual ||
                        invocation.ArgumentList.Arguments.Count == calledMethod.Parameters.Length ||
                        !IsCallInsideOverride(invocation, calledMethod, c.SemanticModel))
                    {
                        return;
                    }

                    var pluralize = calledMethod.Parameters.Length - invocation.ArgumentList.Arguments.Count > 1
                        ? "s"
                        : string.Empty;
                    c.ReportDiagnostic(Diagnostic.Create(rule, invocation.GetLocation(), pluralize));
                },
                SyntaxKind.InvocationExpression);
        }

        private static bool IsCallInsideOverride(InvocationExpressionSyntax invocation, IMethodSymbol calledMethod,
            SemanticModel semanticModel)
        {
            var enclosingSymbol = semanticModel.GetEnclosingSymbol(invocation.SpanStart) as IMethodSymbol;

            return enclosingSymbol != null &&
                enclosingSymbol.IsOverride &&
                object.Equals(enclosingSymbol.OverriddenMethod, calledMethod);
        }

        private static bool IsOnBase(InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            return memberAccess != null &&
                memberAccess.Expression.IsKind(SyntaxKind.BaseExpression);
        }
    }
}
