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
public sealed class MutableFieldsShouldNotBePublicReadonly : MutableFieldsShouldNotBe
{
    private const string DiagnosticId = "S3887";
    private const string MessageFormat = "Use an immutable collection or reduce the accessibility of the non-private readonly field{0} {1}.";

    protected override ISet<SyntaxKind> InvalidModifiers { get; } = new HashSet<SyntaxKind>
    {
        SyntaxKind.PublicKeyword,
        SyntaxKind.ReadOnlyKeyword
    };

    public MutableFieldsShouldNotBePublicReadonly() : base(DiagnosticId, MessageFormat) { }
}
