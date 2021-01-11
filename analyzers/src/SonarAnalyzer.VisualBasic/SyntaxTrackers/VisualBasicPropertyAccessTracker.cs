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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers.VisualBasic;

namespace SonarAnalyzer.Helpers
{
    public class VisualBasicPropertyAccessTracker : PropertyAccessTracker<SyntaxKind>
    {
        protected override SyntaxKind[] TrackedSyntaxKinds { get; } =
            new[]
            {
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.IdentifierName
            };

        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer { get; } = VisualBasicGeneratedCodeRecognizer.Instance;

        public VisualBasicPropertyAccessTracker(IAnalyzerConfiguration analyzerConfiguration, DiagnosticDescriptor rule) : base(analyzerConfiguration, rule, true) { }

        public override PropertyAccessCondition MatchGetter() =>
            context => !((ExpressionSyntax)context.Expression).IsLeftSideOfAssignment();

        public override PropertyAccessCondition MatchSetter() =>
            context => ((ExpressionSyntax)context.Expression).IsLeftSideOfAssignment();

        public override PropertyAccessCondition AssignedValueIsConstant() =>
            context =>
            {
                var assignment = (AssignmentStatementSyntax)context.Expression.Ancestors()
                    .FirstOrDefault(ancestor => ancestor.IsKind(SyntaxKind.SimpleAssignmentStatement));

                return assignment != null && assignment.Right.HasConstantValue(context.SemanticModel);
            };

        protected override string GetPropertyName(SyntaxNode expression) =>
            ((ExpressionSyntax)expression).GetIdentifier()?.Identifier.ValueText;

        protected override bool IsIdentifierWithinMemberAccess(SyntaxNode expression) =>
            expression.IsKind(SyntaxKind.IdentifierName) &&
            expression.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression);
    }
}
