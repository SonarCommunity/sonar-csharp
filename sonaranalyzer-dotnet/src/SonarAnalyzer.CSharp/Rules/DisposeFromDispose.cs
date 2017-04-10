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

using System.Linq;
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
    public class DisposeFromDispose : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2952";
        private const string MessageFormat = "Move this 'Dispose' call into this class' own 'Dispose' method.";

        private const string DisposeMethodName = "Dispose";
        private const string DisposeMethodExplicitName = "System.IDisposable.Dispose";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var invocation = (InvocationExpressionSyntax)c.Node;
                    var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                    if (memberAccess == null ||
                        !IsDisposableField(memberAccess.Expression, c.SemanticModel) ||
                        !IsDisposeMethodCalled(invocation, c.SemanticModel))
                    {
                        return;
                    }

                    var enclosingSymbol = c.SemanticModel.GetEnclosingSymbol(invocation.SpanStart);
                    if (enclosingSymbol == null)
                    {
                        return;
                    }

                    var enclosingMethodSymbol = enclosingSymbol as IMethodSymbol;
                    if (enclosingMethodSymbol == null ||
                        !IsMethodMatchingDisposeMethodName(enclosingMethodSymbol))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(rule, memberAccess.Name.GetLocation()));
                    }
                },
                SyntaxKind.InvocationExpression);
        }

        private static bool IsDisposeMethodCalled(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            var disposeMethod = DisposeNotImplementingDispose.GetDisposeMethod(semanticModel.Compilation);
            return disposeMethod != null &&
                methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(disposeMethod));
        }

        private static bool IsDisposableField(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var fieldSymbol = semanticModel.GetSymbolInfo(expression).Symbol as IFieldSymbol;
            return fieldSymbol != null &&
                DisposableMemberInNonDisposableClass.IsNonStaticNonPublicDisposableField(fieldSymbol) &&
                fieldSymbol.ContainingType.Implements(KnownType.System_IDisposable);
        }

        private static bool IsMethodMatchingDisposeMethodName(IMethodSymbol enclosingMethodSymbol)
        {
            return enclosingMethodSymbol.Name == DisposeMethodName ||
                enclosingMethodSymbol.ExplicitInterfaceImplementations.Any() && enclosingMethodSymbol.Name == DisposeMethodExplicitName;
        }
    }
}
