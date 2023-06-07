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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.SymbolicExecution.Constraints;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks;

public abstract class ObjectsShouldNotBeDisposedMoreThanOnceBase : SymbolicRuleCheck
{
    protected const string DiagnosticId = "S3966";
    protected const string MessageFormat = "Resource '{1}' {0}";
    protected const string MessageDisposeTwice = "has already been disposed. Refactor the code to dispose it only once.";
    protected const string MessageDisposeUsing = "is ensured to be disposed by this using statement. You don't need to dispose it twice.";

    private static readonly string[] DisposeMethods = { "Dispose", "DisposeAsync"};

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        var state = context.State;
        if (context.Operation.Instance.AsInvocation() is { } invocation && DisposeMethods.Contains(invocation.TargetMethod.Name))
        {
            if (state[invocation.Instance]?.HasConstraint(DisposableConstraint.Disposed) is true)
            {
                ReportIssue(context.Operation.Instance, IsPartOfUsingStatement(invocation) ? MessageDisposeUsing : MessageDisposeTwice, invocation.Instance.Syntax.ToString());
            }
            else if (invocation.Instance.TrackedSymbol() is { } instance)
            {
                state = state.SetSymbolConstraint(instance, DisposableConstraint.Disposed);
            }
        }
        return state;

        static bool IsPartOfUsingStatement(IInvocationOperationWrapper invocation) =>
            (invocation.Instance.Syntax.Ancestors().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault() is { } localStatement
            && localStatement.UsingKeyword().IsKind(SyntaxKind.UsingKeyword))
            || invocation.Instance.Syntax.Ancestors().OfType<UsingStatementSyntax>().Any() is true;
    }
}
