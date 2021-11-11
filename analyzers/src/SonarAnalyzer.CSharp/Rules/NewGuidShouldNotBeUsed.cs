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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Wrappers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class NewGuidShouldNotBeUsed : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4581";
        private const string MessageFormat = "Use 'Guid.NewGuid()' or 'Guid.Empty' or add arguments to this Guid instantiation.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            DetectIssueInConstructors(context);
            DetectIssueInDefaultExpressions(context);
        }

        private void DetectIssueInConstructors(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var objectCreationSyntax = ObjectCreationFactory.Create(c.Node);
                if (objectCreationSyntax != null)
                {
                    if (objectCreationSyntax.ArgumentList?.Arguments.Count == 0
                        && objectCreationSyntax.MethodSymbol(c.SemanticModel).ContainingType.Is(KnownType.System_Guid))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, objectCreationSyntax.Expression.GetLocation()));
                    }
                }
            },
            SyntaxKind.ObjectCreationExpression,
            SyntaxKindEx.ImplicitObjectCreationExpression);

        private void DetectIssueInDefaultExpressions(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var expressionSyntax = (ExpressionSyntax)c.Node;
                var symbol = c.SemanticModel.GetTypeInfo(expressionSyntax).ConvertedType;
                if (expressionSyntax.IsKind(SyntaxKindEx.DefaultLiteralExpression)
                    && c.SemanticModel.GetTypeInfo(expressionSyntax).ConvertedType.Is(KnownType.System_Guid))
                {
                    c.ReportIssue(Diagnostic.Create(Rule, expressionSyntax.GetLocation()));
                }
                else if (expressionSyntax.IsKind(SyntaxKind.DefaultExpression))
                {
                    if (DefaultExpressionIdentifierIsGuid((DefaultExpressionSyntax)expressionSyntax))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, expressionSyntax.GetLocation()));
                    }
                }
            },
            SyntaxKind.DefaultExpression,
            SyntaxKindEx.DefaultLiteralExpression);

        private bool DefaultExpressionIdentifierIsGuid(DefaultExpressionSyntax defaultExpression)
        {
            var expressionIdentifier = defaultExpression.Type.GetIdentifier();
            return expressionIdentifier != null
                ? expressionIdentifier.Identifier.Text.Equals("Guid")
                : defaultExpression.Type.ToString().Equals("System.Guid");
        }
    }
}
