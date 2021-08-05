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

using System;
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
    [Obsolete("This rule is deprecated.")]
    public sealed class MethodShouldBeNamedAccordingToSynchronicity : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S4261";
        private const string MessageFormat = "{0}";
        private const string AddAsyncSuffixMessage = "Add the 'Async' suffix to the name of this method.";
        private const string RemoveAsyncSuffixMessage = "Remove the 'Async' suffix to the name of this method.";

        private static readonly ImmutableArray<KnownType> AsyncReturnTypes =
            ImmutableArray.Create(
                KnownType.System_Threading_Tasks_Task,
                KnownType.System_Threading_Tasks_Task_T,
                KnownType.System_Threading_Tasks_ValueTask, // NetCore 2.2+
                KnownType.System_Threading_Tasks_ValueTask_TResult);

        private static readonly ImmutableArray<KnownType> AsyncReturnInterfaces =
            ImmutableArray.Create(KnownType.System_Collections_Generic_IAsyncEnumerable_T);

        private static readonly DiagnosticDescriptor Rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var methodDeclaration = (MethodDeclarationSyntax)c.Node;
                    if (methodDeclaration.Identifier.IsMissing)
                    {
                        return;
                    }

                    var methodSymbol = c.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                    if (methodSymbol == null
                        || methodSymbol.IsMainMethod()
                        || methodSymbol.GetInterfaceMember() != null
                        || methodSymbol.GetOverriddenMember() != null
                        || methodSymbol.IsTestMethod()
                        || methodSymbol.IsControllerMethod()
                        || IsSignalRHubMethod(methodSymbol))
                    {
                        return;
                    }

                    var hasAsyncReturnType = HasAsyncReturnType(methodSymbol);
                    var hasAsyncSuffix = HasAsyncSuffix(methodDeclaration);

                    if (hasAsyncSuffix && !hasAsyncReturnType)
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), RemoveAsyncSuffixMessage));
                    }
                    else if (!hasAsyncSuffix && hasAsyncReturnType)
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), AddAsyncSuffixMessage));
                    }
                },
                SyntaxKind.MethodDeclaration);

        private static bool HasAsyncReturnType(IMethodSymbol methodSymbol)
        {
            var returnSymbol = (methodSymbol.ReturnType as INamedTypeSymbol)?.ConstructedFrom;
            if (returnSymbol == null || returnSymbol.Is(KnownType.Void))
            {
                return false;
            }

            return returnSymbol.DerivesFromAny(AsyncReturnTypes)
                   || returnSymbol.IsAny(AsyncReturnInterfaces)
                   || returnSymbol.ImplementsAny(AsyncReturnInterfaces);

        }

        private static bool HasAsyncSuffix(MethodDeclarationSyntax methodDeclaration) =>
            methodDeclaration.Identifier.ValueText.ToUpper().EndsWith("ASYNC");

        private static bool IsSignalRHubMethod(ISymbol methodSymbol) =>
            methodSymbol.GetEffectiveAccessibility() == Accessibility.Public
            && IsSignalRHubMethod(methodSymbol.ContainingType);

        private static bool IsSignalRHubMethod(ITypeSymbol typeSymbol) =>
            typeSymbol.DerivesFrom(KnownType.Microsoft_AspNet_SignalR_Hub);
    }
}
