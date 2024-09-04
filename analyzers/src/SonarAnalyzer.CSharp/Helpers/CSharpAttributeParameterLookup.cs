﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

namespace SonarAnalyzer.CSharp.Core.Syntax.Utilities;

internal class CSharpAttributeParameterLookup(AttributeSyntax attribute, IMethodSymbol methodSymbol) :
    MethodParameterLookupBase<AttributeArgumentSyntax>(attribute.ArgumentList?.Arguments ?? default, methodSymbol)
{
    protected override SyntaxNode Expression(AttributeArgumentSyntax argument) =>
        argument.Expression;

    protected override SyntaxToken? GetNameColonIdentifier(AttributeArgumentSyntax argument) =>
        argument.NameColon?.Name.Identifier;

    protected override SyntaxToken? GetNameEqualsIdentifier(AttributeArgumentSyntax argument) =>
        argument.NameEquals?.Name.Identifier;
}
