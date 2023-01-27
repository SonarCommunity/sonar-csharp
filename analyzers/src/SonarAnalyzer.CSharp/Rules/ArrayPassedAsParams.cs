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

namespace SonarAnalyzer.Rules.CSharp;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArrayPassedAsParams : ArrayPassedAsParamsBase<SyntaxKind, ArgumentSyntax>
{
    protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

    protected override SyntaxKind[] ParamsInvocationKinds { get; } =
        {
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.InvocationExpression
        };

    protected override ArgumentSyntax GetLastArgumentIfArrayCreation(SyntaxNode expression) =>
        expression switch
        {
            ObjectCreationExpressionSyntax { } creation => CheckLastArgument(creation.ArgumentList),
            InvocationExpressionSyntax { } invocation => CheckLastArgument(invocation.ArgumentList),
            _ => null
        };

    private static ArgumentSyntax CheckLastArgument(ArgumentListSyntax argumentList) =>
        argumentList is not null
        && argumentList.Arguments.Any()
        && argumentList.Arguments.Last().Expression is ArrayCreationExpressionSyntax invocationArray
        && invocationArray.Initializer is InitializerExpressionSyntax { Expressions.Count: > 0 }
        ? argumentList.Arguments.Last()
        : null;
}
