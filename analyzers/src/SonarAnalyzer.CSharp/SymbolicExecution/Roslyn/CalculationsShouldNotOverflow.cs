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

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks.CSharp;

public sealed class CalculationsShouldNotOverflow : CalculationsShouldNotOverflowBase
{
    public static readonly DiagnosticDescriptor S3949 = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    protected override DiagnosticDescriptor Rule => S3949;

    public override bool ShouldExecute()
    {
        if (ContainingSymbol?.Name == nameof(GetHashCode))
        {
            return false;
        }
        else
        {
            var walker = new SyntaxKindWalker();
            walker.SafeVisit(Node);
            return walker.HasOverflow;
        }
    }

    internal sealed class SyntaxKindWalker : SafeCSharpSyntaxWalker
    {
        public bool HasOverflow { get; private set; }
        private bool IsUnchecked { get; set; }

        public override void Visit(SyntaxNode node)
        {
            if (HasOverflow)
            {
                return; // We have an potential overflow: stop visiting
            }
            if (!IsUnchecked && node.Kind() is
                SyntaxKind.AddExpression or
                SyntaxKind.AddAssignmentExpression or
                SyntaxKind.MultiplyExpression or
                SyntaxKind.MultiplyAssignmentExpression or
                SyntaxKind.SubtractExpression or
                SyntaxKind.SubtractAssignmentExpression or
                SyntaxKind.PostDecrementExpression or
                SyntaxKind.PostIncrementExpression or
                SyntaxKind.PreDecrementExpression or
                SyntaxKind.PreIncrementExpression)
            {
                HasOverflow = true;
                return;
            }
            base.Visit(node);
        }

        public override void VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            var before = SetIsUnchecked(node.Kind() == SyntaxKind.UncheckedExpression);
            base.VisitCheckedExpression(node);
            IsUnchecked = before;
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            var before = SetIsUnchecked(node.Kind() == SyntaxKind.UncheckedStatement);
            base.VisitCheckedStatement(node);
            IsUnchecked = before;
        }

        private bool SetIsUnchecked(bool isUnchecked)
        {
            var before = IsUnchecked;
            IsUnchecked = isUnchecked;
            return before;
        }
    }
}
