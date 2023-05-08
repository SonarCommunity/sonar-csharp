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

namespace SonarAnalyzer.Rules.VisualBasic;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public sealed class ExistsInsteadOfAny : ExistsInsteadOfAnyBase<SyntaxKind, InvocationExpressionSyntax>
{
    protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

    // TO BE DELETED
    protected override bool TryGetOperands(InvocationExpressionSyntax invocation, out SyntaxNode left, out SyntaxNode right)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax access)
        {
            left = access.Expression ?? GetLeft(invocation);
            right = access.Name;
            return true;
        }
        left = right = null;
        return false;

        static SyntaxNode GetLeft(SyntaxNode current, int iteration = 0)
        {
            const int recursionThreshold = 42;
            if (iteration > recursionThreshold || current.Parent is CompilationUnitSyntax)
            {
                return null;
            }
            if (current.Parent is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull == current)
            {
                return conditional.Expression;
            }
            return GetLeft(current.Parent, iteration + 1);
        }
    }

    protected override bool HasAnyArguments(InvocationExpressionSyntax node) =>
        node.ArgumentList.Arguments is { Count: > 0 };

    protected override SyntaxToken? GetIdentifier(InvocationExpressionSyntax invocation) =>
        invocation.GetIdentifier();
}
