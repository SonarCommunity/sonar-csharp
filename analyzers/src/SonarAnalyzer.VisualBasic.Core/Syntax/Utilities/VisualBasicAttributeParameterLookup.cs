﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

namespace SonarAnalyzer.VisualBasic.Core.Syntax.Utilities;

internal class VisualBasicAttributeParameterLookup(SeparatedSyntaxList<ArgumentSyntax> argumentList, IMethodSymbol methodSymbol) : MethodParameterLookupBase<ArgumentSyntax>(argumentList, methodSymbol)
{
    protected override SyntaxNode Expression(ArgumentSyntax argument) =>
        argument.GetExpression();

    protected override SyntaxToken? GetNameColonIdentifier(ArgumentSyntax argument) =>
        null;

    protected override SyntaxToken? GetNameEqualsIdentifier(ArgumentSyntax argument) =>
        argument is SimpleArgumentSyntax { NameColonEquals.Name.Identifier: var identifier }
            ? identifier
            : null;
}
