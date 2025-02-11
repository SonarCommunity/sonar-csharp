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

namespace SonarAnalyzer.CSharp.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedundantParenthesesObjectsCreation : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3235";
        private const string MessageFormat = "Remove these redundant parentheses.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var argumentList = (AttributeArgumentListSyntax)c.Node;
                    if (!argumentList.Arguments.Any())
                    {
                        c.ReportIssue(rule, argumentList);
                    }
                },
                SyntaxKind.AttributeArgumentList);

            context.RegisterNodeAction(
                c =>
                {
                    var objectCreation = (ObjectCreationExpressionSyntax)c.Node;
                    var argumentList = objectCreation.ArgumentList;
                    if (argumentList != null &&
                        objectCreation.Initializer != null &&
                        !argumentList.Arguments.Any())
                    {
                        c.ReportIssue(rule, argumentList);
                    }
                },
                SyntaxKind.ObjectCreationExpression);
        }
    }
}
