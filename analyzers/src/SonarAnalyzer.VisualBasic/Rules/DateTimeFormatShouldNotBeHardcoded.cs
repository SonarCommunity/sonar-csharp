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

namespace SonarAnalyzer.Rules.VisualBasic;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public sealed class DateTimeFormatShouldNotBeHardcoded : DateTimeFormatShouldNotBeHardcodedBase<SyntaxKind, InvocationExpressionSyntax>
{
    protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

    protected override Location HardCodedArgumentLocation(InvocationExpressionSyntax invocation)
    {
        var simpleArgument = (SimpleArgumentSyntax)invocation.ArgumentList.Arguments[0];
        return simpleArgument.Expression.GetLocation();
    }

    protected override bool HasInvalidFirstArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
        invocation.ArgumentList is { }
        && invocation.ArgumentList.Arguments.Any()
        && invocation.ArgumentList.Arguments[0] is SimpleArgumentSyntax simpleArgument
        && simpleArgument.Expression.FindConstantValue(semanticModel) is string { Length: > 1 };
}
