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

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Metrics.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class CognitiveComplexity : CognitiveComplexityBase
    {
        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager,
                isEnabledByDefault: false);

        private static readonly Func<ICognitiveComplexityWalker> walkerFactory = () => new CSharpCognitiveComplexityWalker();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        public override DiagnosticDescriptor Rule => rule;


        protected override void Initialize(ParameterLoadingAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<MethodDeclarationSyntax>(c, m => m, m => m.Identifier.GetLocation(),
                    walkerFactory, "method", Threshold),
                SyntaxKind.MethodDeclaration);

            // Here, we only care about arrowed properties, others will be handled by the accessor.
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<PropertyDeclarationSyntax>(c, p => p.ExpressionBody, p => p.Identifier.GetLocation(),
                    walkerFactory, "property", PropertyThreshold),
                SyntaxKind.PropertyDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<ConstructorDeclarationSyntax>(c, co => co, co => co.Identifier.GetLocation(),
                    walkerFactory, "constructor", Threshold),
                SyntaxKind.ConstructorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<DestructorDeclarationSyntax>(c, d => d, d => d.Identifier.GetLocation(),
                    walkerFactory, "destructor", Threshold),
                SyntaxKind.DestructorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<OperatorDeclarationSyntax>(c, o => o, o => o.OperatorToken.GetLocation(),
                    walkerFactory, "operator", Threshold),
                SyntaxKind.OperatorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<AccessorDeclarationSyntax>(c, a => a, a => a.Keyword.GetLocation(),
                    walkerFactory, "accessor", PropertyThreshold),
                SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveAccessorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
               c => CheckComplexity<FieldDeclarationSyntax>(c, f => f, f => f.Declaration.Variables[0].Identifier.GetLocation(),
                   walkerFactory, "field", Threshold),
               SyntaxKind.FieldDeclaration);
        }
    }
}
