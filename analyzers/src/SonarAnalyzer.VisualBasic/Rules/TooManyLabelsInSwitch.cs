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
    public sealed class TooManyLabelsInSwitch : TooManyLabelsInSwitchBase<SyntaxKind, SelectStatementSyntax>
    {
        protected override DiagnosticDescriptor Rule { get; } =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager,
                isEnabledByDefault: false);

        private const string MessageFormat = "Consider reworking this 'Select Case' to reduce the number of 'Case's" +
            " from {1} to at most {0}.";

        protected override SyntaxKind[] SyntaxKinds { get; } =
            new[] { SyntaxKind.SelectStatement };

        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer =>
            Helpers.VisualBasic.VisualBasicGeneratedCodeRecognizer.Instance;

        protected override SyntaxNode GetExpression(SelectStatementSyntax statement) =>
            statement.Expression;

        protected override int GetSectionsCount(SelectStatementSyntax statement) =>
            ((SelectBlockSyntax)statement.Parent).CaseBlocks.Count;

        protected override Location GetKeywordLocation(SelectStatementSyntax statement) =>
            statement.SelectKeyword.GetLocation();
    }
}
