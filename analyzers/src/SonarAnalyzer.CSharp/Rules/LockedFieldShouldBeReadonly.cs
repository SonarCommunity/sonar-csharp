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
public sealed class LockedFieldShouldBeReadonly : SonarDiagnosticAnalyzer
{
    private const string DiagnosticId = "S2445";

    private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, "Do not lock on {0}, use a readonly field instead.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(CheckLockStatement, SyntaxKind.LockStatement);

    private static void CheckLockStatement(SonarSyntaxNodeReportingContext context)
    {
        var expression = ((LockStatementSyntax)context.Node).Expression?.RemoveParentheses();
        if (IsCreation(expression))
        {
            ReportIssue("a new instance because is a no-op");
        }
        else
        {
            var lazySymbol = new Lazy<ISymbol>(() => context.SemanticModel.GetSymbolInfo(expression).Symbol);
            if (IsOfTypeString(expression, lazySymbol))
            {
                ReportIssue("strings as they can be interned");
            }
            else if (expression is IdentifierNameSyntax && lazySymbol.Value is ILocalSymbol localSymbol)
            {
                ReportIssue($"local variable '{localSymbol.Name}'");
            }
            else if (FieldWritable(expression, lazySymbol) is { } field)
            {
                ReportIssue($"writable field '{field.Name}'");
            }
        }

        void ReportIssue(string message) =>
            context.ReportIssue(Diagnostic.Create(Rule, expression.GetLocation(), message));
    }

    private static bool IsCreation(ExpressionSyntax expression) =>
        expression.IsAnyKind(
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.AnonymousObjectCreationExpression,
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.ImplicitArrayCreationExpression,
            SyntaxKind.QueryExpression);

    private static bool IsOfTypeString(ExpressionSyntax expression, Lazy<ISymbol> lazySymbol) =>
        expression.IsAnyKind(SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringExpression)
        || lazySymbol.Value.GetSymbolType().Is(KnownType.System_String);

    private static IFieldSymbol FieldWritable(ExpressionSyntax expression, Lazy<ISymbol> lazySymbol) =>
        expression.IsAnyKind(SyntaxKind.IdentifierName, SyntaxKind.SimpleMemberAccessExpression) && lazySymbol.Value is IFieldSymbol lockedField && !lockedField.IsReadOnly
            ? lockedField
            : null;
}
