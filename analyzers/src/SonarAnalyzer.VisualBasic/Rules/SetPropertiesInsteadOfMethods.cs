﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
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

namespace SonarAnalyzer.VisualBasic.Rules;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public sealed class SetPropertiesInsteadOfMethods : SetPropertiesInsteadOfMethodsBase<SyntaxKind, InvocationExpressionSyntax>
{
    protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

    protected override bool HasCorrectArgumentCount(InvocationExpressionSyntax invocation) =>
        invocation.HasExactlyNArguments(1);

    // In VB you need to be explicit: Enumerable.Min(set), because set.Min() resolves to the property.
    protected override bool TryGetOperands(InvocationExpressionSyntax invocation, out SyntaxNode typeNode, out SyntaxNode methodNode)
    {
        typeNode = invocation.ArgumentList.Arguments[0].GetExpression();
        methodNode = invocation;
        return true;
    }
}
