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

using SonarAnalyzer.SymbolicExecution.Constraints;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks;

public abstract class NullPointerDereferenceBase : SymbolicRuleCheck
{
    internal const string DiagnosticId = "S2259";

    protected virtual bool IsSupressed(SyntaxNode node) => false;

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        if (ReferenceOrDefault(context.Operation.Instance) is { Syntax: var syntax} reference
            && context.HasConstraint(reference, ObjectConstraint.Null)
            && !IsSupressed(syntax)
            && SemanticModel.GetTypeInfo(syntax).Nullability().FlowState != NullableFlowState.NotNull)
        {
            ReportIssue(reference, syntax.ToString());
        }
        return context.State;
    }

    private static IOperation ReferenceOrDefault(IOperation operation) =>
        operation.Kind switch
        {
            OperationKindEx.Invocation => InvocationOrDefault(operation),
            OperationKindEx.PropertyReference => PropertyReferenceOrDefault(operation),
            OperationKindEx.Await => operation.ToAwait().Operation,
            OperationKindEx.ArrayElementReference => operation.ToArrayElementReference().ArrayReference,
            _ => null,
        };

    private static IOperation InvocationOrDefault(IOperation operation) =>
        operation.ToInvocation() is { Instance: var instance, TargetMethod: var method }
        && (!method.ContainingType.Is(KnownType.System_Nullable_T) || method.Name == nameof(Nullable<int>.GetType)) // All methods on Nullable but .GetType() are safe to call
            ? instance
            : null;

    private static IOperation PropertyReferenceOrDefault(IOperation operation) =>
        operation.ToPropertyReference() is { Instance: var instance, Property: var property }
        && !property.IsInType(KnownType.System_Nullable_T)  // HasValue doesn't throw; Value is covered by S3655
            ? instance
            : null;
}
