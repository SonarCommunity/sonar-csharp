﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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
    public sealed class StringConcatenationInLoop
        : StringConcatenationInLoopBase<SyntaxKind, AssignmentExpressionSyntax, BinaryExpressionSyntax>
    {
        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override bool IsExpressionConcatenation(BinaryExpressionSyntax addExpression) =>
            addExpression.IsKind(SyntaxKind.AddExpression);

        protected override SyntaxNode GetLeft(AssignmentExpressionSyntax assignment) => assignment.Left;

        protected override SyntaxNode GetRight(AssignmentExpressionSyntax assignment) => assignment.Right;

        protected override SyntaxNode GetLeft(BinaryExpressionSyntax binary) => binary.Left;

        protected override bool IsInLoop(SyntaxNode node) => LoopKinds.Contains(node.Kind());

        protected override bool AreEquivalent(SyntaxNode node1, SyntaxNode node2) =>
            CSharpEquivalenceChecker.AreEquivalent(node1, node2);

        private static readonly ISet<SyntaxKind> LoopKinds = new HashSet<SyntaxKind>
        {
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement
        };

        private static readonly ISet<SyntaxKind> AddOperators = new HashSet<SyntaxKind>
        {
            SyntaxKind.PlusToken,
            SyntaxKind.PlusEqualsToken
        };

        private static readonly ImmutableArray<SyntaxKind> simpleAssignmentKinds =
            ImmutableArray.Create(SyntaxKind.SimpleAssignmentExpression);

        private static readonly ImmutableArray<SyntaxKind> compoundAssignmentKinds =
            ImmutableArray.Create(SyntaxKind.AddAssignmentExpression);

        protected override ImmutableArray<SyntaxKind> SimpleAssignmentKinds => simpleAssignmentKinds;

        protected override ImmutableArray<SyntaxKind> CompoundAssignmentKinds => compoundAssignmentKinds;

        protected override Helpers.GeneratedCodeRecognizer GeneratedCodeRecognizer => CSharpGeneratedCodeRecognizer.Instance;

        protected override bool IsAddExpression(BinaryExpressionSyntax rightExpression) =>
            rightExpression != null &&
            rightExpression.OperatorToken.IsAnyKind(AddOperators);
    }
}
