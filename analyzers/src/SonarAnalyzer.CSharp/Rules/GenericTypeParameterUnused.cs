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
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class GenericTypeParameterUnused : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2326";
        private const string MessageFormat = "'{0}' is not used in the {1}.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationStartAction(analysisContext =>
            {
                analysisContext.RegisterSyntaxNodeAction(c =>
                     {
                         var declarationSymbol = c.SemanticModel.GetDeclaredSymbol(c.Node);
                         if (declarationSymbol == null)
                         {
                             return;
                         }

                         CheckGenericTypeParameters(declarationSymbol, c);
                     },
                     SyntaxKind.MethodDeclaration,
                     SyntaxKindEx.LocalFunctionStatement);

                analysisContext.RegisterSyntaxNodeAction(c =>
                     {
                         if (c.ContainingSymbol.Kind != SymbolKind.NamedType)
                         {
                             return;
                         }

                         CheckGenericTypeParameters(c.ContainingSymbol, c);
                     },
                     SyntaxKind.ClassDeclaration,
                     SyntaxKindEx.RecordDeclaration);
            });

        private static void CheckGenericTypeParameters(ISymbol symbol, SyntaxNodeAnalysisContext c)
        {
            var helper = CreateParametersInfo(c.Node, c.SemanticModel);
            if (helper.Parameters == null || helper.Parameters.Parameters.Count == 0)
            {
                return;
            }

            var declarations = symbol.DeclaringSyntaxReferences
                                     .Select(reference => reference.GetSyntax());

            var typeParameterNames = helper.Parameters.Parameters.Select(typeParameter => typeParameter.Identifier.Text).ToArray();

            var usedTypeParameters = GetUsedTypeParameters(declarations, typeParameterNames, c);

            foreach (var typeParameter in typeParameterNames.Where(typeParameter => !usedTypeParameters.Contains(typeParameter)))
            {
                c.ReportDiagnosticWhenActive(Diagnostic.Create(Rule,
                                                               helper.Parameters.Parameters.First(tp => tp.Identifier.Text == typeParameter).GetLocation(),
                                                               typeParameter,
                                                               helper.ContainerName));
            }
        }

        private static ParametersInfo CreateParametersInfo(SyntaxNode node, SemanticModel semanticModel) =>
            node switch
            {
                ClassDeclarationSyntax classDeclaration => new ParametersInfo(classDeclaration.TypeParameterList, "class"),

                MethodDeclarationSyntax methodDeclaration when IsMethodCandidate(methodDeclaration, semanticModel)
                    => new ParametersInfo(methodDeclaration.TypeParameterList, "method"),

                var wrapper when LocalFunctionStatementSyntaxWrapper.IsInstance(wrapper)
                    => new ParametersInfo(((LocalFunctionStatementSyntaxWrapper)node).TypeParameterList, "local function"),

                var wrapper when RecordDeclarationSyntaxWrapper.IsInstance(wrapper)
                    => new ParametersInfo(((RecordDeclarationSyntaxWrapper)node).TypeParameterList, "record"),

                _ => default
            };

        private readonly struct ParametersInfo
        {
            public TypeParameterListSyntax Parameters { get; }

            public string ContainerName { get; }

            public ParametersInfo(TypeParameterListSyntax parameters, string name)
            {
                Parameters = parameters;
                ContainerName = name;
            }
        }

        private static bool IsMethodCandidate(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            var syntaxValid = !methodDeclaration.Modifiers.Any(modifier => MethodModifiersToSkip.Contains(modifier.Kind()))
                              && methodDeclaration.ExplicitInterfaceSpecifier == null
                              && methodDeclaration.HasBodyOrExpressionBody();

            if (!syntaxValid)
            {
                return false;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

            return methodSymbol != null && methodSymbol.IsChangeable();
        }

        private static List<string> GetUsedTypeParameters(IEnumerable<SyntaxNode> declarations, string[] typeParameterNames, SyntaxNodeAnalysisContext context) =>
            declarations.SelectMany(declaration => declaration.DescendantNodes())
                        .OfType<IdentifierNameSyntax>()
                        .Where(identifier => !(identifier.Parent is TypeParameterConstraintClauseSyntax))
                        .Where(identifier => typeParameterNames.Contains(identifier.Identifier.ValueText))
                        .Select(identifier => identifier.EnsureCorrectSemanticModelOrDefault(context.SemanticModel)?.GetSymbolInfo(identifier).Symbol)
                        .Where(symbol => symbol is {Kind: SymbolKind.TypeParameter})
                        .Select(symbol => symbol.Name)
                        .ToList();

        private static readonly ISet<SyntaxKind> MethodModifiersToSkip = new HashSet<SyntaxKind>
        {
            SyntaxKind.AbstractKeyword,
            SyntaxKind.VirtualKeyword,
            SyntaxKind.OverrideKeyword
        };
    }
}
