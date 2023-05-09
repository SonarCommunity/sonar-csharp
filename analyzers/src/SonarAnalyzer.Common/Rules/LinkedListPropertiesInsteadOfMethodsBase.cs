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

namespace SonarAnalyzer.Rules;

public abstract class LinkedListPropertiesInsteadOfMethodsBase<TSyntaxKind, TInvocationExpression> : SonarDiagnosticAnalyzer<TSyntaxKind>
    where TSyntaxKind : struct
    where TInvocationExpression : SyntaxNode
{
    private const string DiagnosticId = "S6613";

    protected override string MessageFormat => "'{0}' property of 'LinkedList' should be used instead of the '{0}()' extension method.";

    protected abstract SyntaxToken? GetIdentifier(TInvocationExpression invocation);
    protected abstract bool IsCorrectCallAndType(TInvocationExpression invocation, SemanticModel model);

    protected LinkedListPropertiesInsteadOfMethodsBase() : base(DiagnosticId) { }

    protected sealed override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(Language.GeneratedCodeRecognizer, c =>
        {
            var invocation = c.Node as TInvocationExpression;
            var methodName = Language.GetName(invocation);

            if (IsFirstOrLast(methodName) && IsCorrectCallAndType(invocation, c.SemanticModel))
            {
                c.ReportIssue(Diagnostic.Create(Rule, GetIdentifier(invocation)?.GetLocation(), methodName));
            }
        }, Language.SyntaxKind.InvocationExpression);

    internal static bool IsCorrectCall(SyntaxNode right, SemanticModel model) =>
        model.GetSymbolInfo(right).Symbol is IMethodSymbol method
        && method.IsExtensionOn(KnownType.System_Collections_Generic_IEnumerable_T);

    private bool IsFirstOrLast(string methodName) =>
        methodName.Equals("First", Language.NameComparison)
        || methodName.Equals("Last", Language.NameComparison);
}
