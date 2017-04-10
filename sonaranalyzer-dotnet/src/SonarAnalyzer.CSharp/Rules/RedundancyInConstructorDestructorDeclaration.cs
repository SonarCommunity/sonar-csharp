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
    public class RedundancyInConstructorDestructorDeclaration : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3253";
        private const string MessageFormat = "Remove this redundant {0}.";
        private const IdeVisibility ideVisibility = IdeVisibility.Hidden;

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, ideVisibility, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckConstructorDeclaration(c),
                SyntaxKind.ConstructorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckDestructorDeclaration(c),
                SyntaxKind.DestructorDeclaration);
        }

        private static void CheckDestructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var destructorDeclaration = (DestructorDeclarationSyntax)context.Node;

            if (IsBodyEmpty(destructorDeclaration.Body))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, destructorDeclaration.GetLocation(), "destructor"));
            }
        }

        private static void CheckConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

            if (IsConstructorRedundant(constructorDeclaration, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, constructorDeclaration.GetLocation(), "constructor"));
                return;
            }

            var initializer = constructorDeclaration.Initializer;
            if (initializer != null &&
                IsInitializerRedundant(initializer))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, initializer.GetLocation(), "'base()' call"));
            }
        }

        private static bool IsInitializerRedundant(ConstructorInitializerSyntax initializer)
        {
            return initializer.IsKind(SyntaxKind.BaseConstructorInitializer) &&
                initializer.ArgumentList != null &&
                !initializer.ArgumentList.Arguments.Any();
        }

        private static bool IsConstructorRedundant(ConstructorDeclarationSyntax constructorDeclaration, SemanticModel semanticModel)
        {
            return IsConstructorParameterless(constructorDeclaration) &&
                IsBodyEmpty(constructorDeclaration.Body) &&
                (IsSinglePublicConstructor(constructorDeclaration, semanticModel) ||
                constructorDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)));
        }

        private static bool IsSinglePublicConstructor(ConstructorDeclarationSyntax constructorDeclaration, SemanticModel semanticModel)
        {
            return constructorDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)) &&
                IsInitializerEmptyOrRedundant(constructorDeclaration.Initializer) &&
                TypeHasExactlyOneConstructor(constructorDeclaration, semanticModel);
        }

        private static bool IsInitializerEmptyOrRedundant(ConstructorInitializerSyntax initializer)
        {
            if (initializer == null)
            {
                return true;
            }

            return initializer.ArgumentList != null &&
                !initializer.ArgumentList.Arguments.Any() &&
                initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword);
        }

        private static bool TypeHasExactlyOneConstructor(ConstructorDeclarationSyntax constructorDeclaration, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
            return symbol != null &&
                symbol.ContainingType.GetMembers().OfType<IMethodSymbol>().Count(m => m.MethodKind == MethodKind.Constructor) == 1;
        }

        private static bool IsBodyEmpty(BlockSyntax block)
        {
            return block != null && !block.Statements.Any();
        }

        private static bool IsConstructorParameterless(ConstructorDeclarationSyntax constructorDeclaration)
        {
            return constructorDeclaration.ParameterList != null &&
                !constructorDeclaration.ParameterList.Parameters.Any();
        }
    }
}
