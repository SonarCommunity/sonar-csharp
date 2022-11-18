﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExpressionComplexity : ExpressionComplexityBase<ExpressionSyntax>
    {
        protected override ILanguageFacade Language { get; } = CSharpFacade.Instance;

        private static readonly ISet<SyntaxKind> CompoundExpressionKinds = new HashSet<SyntaxKind>
        {
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.ArrayInitializerExpression,
            SyntaxKind.CollectionInitializerExpression,
            SyntaxKind.ComplexElementInitializerExpression,
            SyntaxKind.ObjectInitializerExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKindEx.SwitchExpressionArm
        };

        private static readonly ISet<SyntaxKind> ComplexityIncreasingKinds = new HashSet<SyntaxKind>
        {
            SyntaxKind.ConditionalExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKindEx.CoalesceAssignmentExpression,
            SyntaxKindEx.AndPattern,
            SyntaxKindEx.OrPattern
        };

        protected override bool IsComplexityIncreasingKind(SyntaxNode node) =>
            ComplexityIncreasingKinds.Contains(node.Kind());

        protected override bool IsCompoundExpression(SyntaxNode node) =>
            CompoundExpressionKinds.Contains(node.Kind());

        protected override bool IsPatternRoot(SyntaxNode node) =>
            node.Parent.IsAnyKind(SyntaxKindEx.CasePatternSwitchLabel, SyntaxKindEx.SwitchExpressionArm);
    }
}
