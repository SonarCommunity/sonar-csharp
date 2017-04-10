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
    public class MethodOverrideChangedDefaultValue : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1006";
        private const string MessageFormat = "{0} the default parameter value {1}.";
        internal const string MessageAdd = "defined in the overridden method";
        internal const string MessageRemove = "to match the signature of overridden method";
        internal const string MessageUseSame = "defined in the overridden method";
        internal const string MessageRemoveExplicit = "from this explicit interface implementation";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var method = (MethodDeclarationSyntax)c.Node;
                    var methodSymbol = c.SemanticModel.GetDeclaredSymbol(method);

                    IMethodSymbol overriddenMember;
                    if (methodSymbol == null ||
                        !methodSymbol.TryGetOverriddenOrInterfaceMember(out overriddenMember))
                    {
                        return;
                    }

                    for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                    {
                        var overridingParameter = methodSymbol.Parameters[i];
                        var overriddenParameter = overriddenMember.Parameters[i];

                        var parameterSyntax = method.ParameterList.Parameters[i];

                        ReportParameterIfNeeded(overridingParameter, overriddenParameter, parameterSyntax,
                            isExplicitImplementation: methodSymbol.ExplicitInterfaceImplementations.Any(),
                            context: c);
                    }
                },
                SyntaxKind.MethodDeclaration);
        }

        private static void ReportParameterIfNeeded(IParameterSymbol overridingParameter, IParameterSymbol overriddenParameter,
            ParameterSyntax parameterSyntax, bool isExplicitImplementation, SyntaxNodeAnalysisContext context)
        {
            if (isExplicitImplementation)
            {
                if (overridingParameter.HasExplicitDefaultValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(rule, parameterSyntax.Default.GetLocation(), "Remove", MessageRemoveExplicit));
                }

                return;
            }

            if (overridingParameter.HasExplicitDefaultValue &&
                !overriddenParameter.HasExplicitDefaultValue)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, parameterSyntax.Default.GetLocation(), "Remove", MessageRemove));
                return;
            }

            if (!overridingParameter.HasExplicitDefaultValue &&
                overriddenParameter.HasExplicitDefaultValue)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, parameterSyntax.Identifier.GetLocation(), "Add", MessageAdd));
                return;
            }

            if (overridingParameter.HasExplicitDefaultValue &&
                overriddenParameter.HasExplicitDefaultValue &&
                !object.Equals(overridingParameter.ExplicitDefaultValue, overriddenParameter.ExplicitDefaultValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, parameterSyntax.Default.Value.GetLocation(), "Use", MessageUseSame));
                return;
            }
        }
    }
}
