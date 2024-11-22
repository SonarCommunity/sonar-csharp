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

namespace SonarAnalyzer.Rules;

public abstract class UseWhereBeforeOrderByBase<TSyntaxKind, TInvocation> : SonarDiagnosticAnalyzer<TSyntaxKind>
    where TSyntaxKind : struct
    where TInvocation : SyntaxNode
{
    private const string DiagnosticId = "S6607";
    protected override string MessageFormat => "\"Where\" should be used before \"{0}\"";

    protected UseWhereBeforeOrderByBase() : base(DiagnosticId) { }

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(Language.GeneratedCodeRecognizer, c =>
        {
            var invocation = (TInvocation)c.Node;

            if (Language.GetName(invocation).Equals("Where", Language.NameComparison)
                && Language.Syntax.TryGetOperands(invocation, out var left, out var right)
                && LeftHasCorrectName(left, out var orderByMethodDescription)
                && MethodIsLinqExtension(left, c.SemanticModel)
                && MethodIsLinqExtension(right, c.SemanticModel)
                && Language.Syntax.NodeIdentifier(right) is { } rightIdentifier
                && Language.Syntax.NodeIdentifier(left) is { } leftIdentifier)
            {
                c.ReportIssue(Rule, rightIdentifier.GetLocation(), [leftIdentifier.ToSecondaryLocation()], orderByMethodDescription);
            }
        },
        Language.SyntaxKind.InvocationExpression);

    private bool LeftHasCorrectName(SyntaxNode left, out string methodName)
    {
        var leftName = Language.GetName(left);
        if (leftName.Equals("OrderBy", Language.NameComparison)
            || leftName.Equals("OrderByDescending", Language.NameComparison))
        {
            methodName = leftName;
            return true;
        }
        methodName = null;
        return false;
    }

    private static bool MethodIsLinqExtension(SyntaxNode node, SemanticModel model) =>
        model.GetSymbolInfo(node).Symbol is IMethodSymbol method
        && method.IsExtensionOn(KnownType.System_Collections_Generic_IEnumerable_T);
}
