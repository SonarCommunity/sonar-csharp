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

namespace SonarAnalyzer.VisualBasic.Rules
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class PartCreationPolicyShouldBeUsedWithExportAttribute
        : PartCreationPolicyShouldBeUsedWithExportAttributeBase<AttributeSyntax, ClassStatementSyntax>
    {
        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override ClassStatementSyntax GetTypeDeclaration(AttributeSyntax attribute) =>
            attribute.FirstAncestorOrSelf<ClassStatementSyntax>();

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(AnalyzeNode, SyntaxKind.Attribute);
    }
}
