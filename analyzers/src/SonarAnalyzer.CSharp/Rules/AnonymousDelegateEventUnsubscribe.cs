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
    public sealed class AnonymousDelegateEventUnsubscribe : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3244";
        private const string MessageFormat = "Unsubscribe with the same delegate that was used for the subscription.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var assignment = (AssignmentExpressionSyntax)c.Node;


                    if (c.Model.GetSymbolInfo(assignment.Left).Symbol is IEventSymbol @event &&
                        assignment.Right is AnonymousFunctionExpressionSyntax)
                    {
                        c.ReportIssue(rule, assignment.OperatorToken.CreateLocation(assignment));
                    }
                },
                SyntaxKind.SubtractAssignmentExpression);
        }
    }
}
