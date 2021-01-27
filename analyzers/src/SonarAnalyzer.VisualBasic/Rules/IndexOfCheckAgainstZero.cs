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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    [Rule(DiagnosticId)]
    public sealed class IndexOfCheckAgainstZero : IndexOfCheckAgainstZeroBase<SyntaxKind, BinaryExpressionSyntax>
    {
        protected override ILanguageFacade LanguageFacade { get; } = VisualBasicFacade.Instance;
        protected override SyntaxKind LessThanExpression => SyntaxKind.LessThanExpression;
        protected override SyntaxKind GreaterThanExpression => SyntaxKind.GreaterThanExpression;

        public IndexOfCheckAgainstZero() : base(RspecStrings.ResourceManager) { }

        protected override SyntaxNode Left(BinaryExpressionSyntax binaryExpression) =>
            binaryExpression.Left;

        protected override SyntaxToken OperatorToken(BinaryExpressionSyntax binaryExpression) =>
            binaryExpression.OperatorToken;

        protected override SyntaxNode Right(BinaryExpressionSyntax binaryExpression) =>
            binaryExpression.Right;
    }
}
