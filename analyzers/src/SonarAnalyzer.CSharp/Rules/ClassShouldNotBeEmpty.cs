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
public sealed class ClassShouldNotBeEmpty : ClassShouldNotBeEmptyBase<SyntaxKind>
{
    protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

    protected override bool IsEmptyAndNotPartial(SyntaxNode node) =>
        node is TypeDeclarationSyntax { Members.Count: 0 } typeDeclaration
        && !typeDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword))
        && (node is ClassDeclarationSyntax || IsParameterlessRecord(node));

    protected override bool IsClassWithDeclaredBaseClass(SyntaxNode node) => node is ClassDeclarationSyntax { BaseList: not null };

    protected override string DeclarationTypeKeyword(SyntaxNode node) => ((TypeDeclarationSyntax)node).Keyword.ValueText;

    private bool IsParameterlessRecord(SyntaxNode node) =>
        RecordDeclarationSyntaxWrapper.IsInstance(node)
        && (RecordDeclarationSyntaxWrapper)node is { ParameterList.Parameters.Count: 0 };
}
