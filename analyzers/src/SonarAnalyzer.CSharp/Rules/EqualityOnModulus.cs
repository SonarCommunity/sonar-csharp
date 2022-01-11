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
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EqualityOnModulus : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2197";
        private const string MessageFormat = "The result of this modulus operation may not be {0}.";

        private static readonly CSharpExpressionNumericConverter ExpressionNumericConverter = new CSharpExpressionNumericConverter();

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(VisitEquality, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);

        private static void VisitEquality(SyntaxNodeAnalysisContext c)
        {
            var equalsExpression = (BinaryExpressionSyntax)c.Node;

            if (CheckExpression(equalsExpression.Left, equalsExpression.Right, c.SemanticModel, out var constantValue)
                || CheckExpression(equalsExpression.Right, equalsExpression.Left, c.SemanticModel, out constantValue))
            {
                c.ReportIssue(Diagnostic.Create(Rule,
                                                               equalsExpression.GetLocation(),
                                                               constantValue < 0 ? "negative" : "positive"));
            }
        }

        private static bool CheckExpression(SyntaxNode node, ExpressionSyntax modulus, SemanticModel semanticModel, out int constantValue) =>
            ExpressionNumericConverter.TryGetConstantIntValue(node, out constantValue)
            && constantValue != 0
            && IsModulus(modulus)
            && !IsUnsigned(modulus, semanticModel);

        private static bool IsModulus(ExpressionSyntax expression) =>
            expression.RemoveParentheses() is BinaryExpressionSyntax binary
            && binary.IsKind(SyntaxKind.ModuloExpression);

        private static bool IsUnsigned(ExpressionSyntax expression, SemanticModel semantic)
        {
            var type = semantic.GetTypeInfo(expression).Type;
            return type.IsAny(KnownType.UnsignedIntegers)
                   || type.Is(KnownType.System_UIntPtr);
        }
    }
}
