﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenTypeAnalyzer : TokenTypeAnalyzerBase<SyntaxKind>
    {
        protected override ILanguageFacade<SyntaxKind> Language { get; } = CSharpFacade.Instance;

        protected override TokenClassifierBase GetTokenClassifier(SyntaxToken token, SemanticModel semanticModel, bool skipIdentifierTokens) =>
            new TokenClassifier(token, semanticModel, skipIdentifierTokens);

        private sealed class TokenClassifier : TokenClassifierBase
        {
            public TokenClassifier(SyntaxToken token, SemanticModel semanticModel, bool skipIdentifiers) : base(token, semanticModel, skipIdentifiers) { }

            protected override SyntaxNode GetBindableParent(SyntaxToken token) =>
                token.GetBindableParent();

            protected override bool IsIdentifier(SyntaxToken token) =>
                token.IsKind(SyntaxKind.IdentifierToken);

            protected override bool IsKeyword(SyntaxToken token) =>
                SyntaxFacts.IsKeywordKind(token.Kind());

            protected override bool IsRegularComment(SyntaxTrivia trivia) =>
                trivia.IsAnyKind(SyntaxKind.SingleLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia);

            protected override bool IsNumericLiteral(SyntaxToken token) =>
                token.IsKind(SyntaxKind.NumericLiteralToken);

            protected override bool IsStringLiteral(SyntaxToken token) =>
                token.IsAnyKind(
                    SyntaxKind.StringLiteralToken,
                    SyntaxKind.CharacterLiteralToken,
                    SyntaxKindEx.SingleLineRawStringLiteralToken,
                    SyntaxKindEx.MultiLineRawStringLiteralToken,
                    SyntaxKindEx.Utf8StringLiteralToken,
                    SyntaxKindEx.Utf8SingleLineRawStringLiteralToken,
                    SyntaxKindEx.Utf8MultiLineRawStringLiteralToken,
                    SyntaxKind.InterpolatedStringStartToken,
                    SyntaxKind.InterpolatedVerbatimStringStartToken,
                    SyntaxKindEx.InterpolatedSingleLineRawStringStartToken,
                    SyntaxKindEx.InterpolatedMultiLineRawStringStartToken,
                    SyntaxKind.InterpolatedStringTextToken,
                    SyntaxKind.InterpolatedStringEndToken,
                    SyntaxKindEx.InterpolatedRawStringEndToken);

            protected override bool IsDocComment(SyntaxTrivia trivia) =>
                trivia.IsAnyKind(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.MultiLineDocumentationCommentTrivia);
        }
    }
}
