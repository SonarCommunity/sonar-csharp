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

namespace SonarAnalyzer.Rules.CSharp;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ToStringShouldNotReturnNull : ToStringShouldNotReturnNullBase<SyntaxKind>
{
    protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

    protected override SyntaxKind MethodKind => SyntaxKind.MethodDeclaration;

    protected override void Initialize(SonarAnalysisContext context)
    {
        base.Initialize(context);

        context.RegisterNodeAction(
            c => ToStringReturnsNull(c, ((MethodDeclarationSyntax)c.Node).ExpressionBody),
            SyntaxKind.MethodDeclaration);
    }

    protected override IEnumerable<SyntaxNode> Conditionals(SyntaxNode expression) =>
        expression is ConditionalExpressionSyntax conditional
        ? new SyntaxNode[] { conditional.WhenTrue, conditional.WhenFalse }
        : Array.Empty<SyntaxNode>();

    protected override bool IsLocalOrLambda(SyntaxNode node) =>
        node.IsAnyKind(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKindEx.LocalFunctionStatement);
}
