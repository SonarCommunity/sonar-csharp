﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SonarAnalyzer.Helpers
{
    internal static class InvocationExpressionSyntaxExtensions
    {
        internal static bool IsMemberAccessOnKnownType(this InvocationExpressionSyntax invocation, string identifierName, KnownType knownType, SemanticModel semanticModel) =>
            invocation.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax &&
            memberAccessExpressionSyntax.NameIs(identifierName) &&
            semanticModel.GetTypeInfo(invocation).Type.Is(knownType);

        internal static IEnumerable<ISymbol> GetArgumentSymbolsOfKnownType(this InvocationExpressionSyntax invocation, KnownType knownType, SemanticModel semanticModel) =>
            invocation.ArgumentList.Arguments.GetSymbolsOfKnownType(knownType, semanticModel);
    }
}
