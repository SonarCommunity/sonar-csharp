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

using System.Reflection.Metadata;
using SonarAnalyzer.SymbolicExecution.Constraints;
using static Microsoft.CodeAnalysis.Accessibility;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks;

public abstract class PublicMethodArgumentsShouldBeCheckedForNullBase : SymbolicRuleCheck
{
    protected const string DiagnosticId = "S3900";
    protected const string MessageFormat = "{0}";

    protected abstract bool IsInConstructorInitializer(SyntaxNode node);
    protected abstract string NullName { get; }

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        if (NullDereferenceCandidate(context.Operation.Instance) is { } candidate
            && candidate.Kind == OperationKindEx.ParameterReference
            && candidate.ToParameterReference() is var reference
            && !reference.Parameter.Type.IsValueType
            && MissesObjectConstraint(context.State[reference.Parameter])
            && !context.CapturedVariables.Contains(reference.Parameter) // Workaround to avoid FPs. Can be removed once captures are properly handled by lva.LiveOut()
            && !reference.Parameter.HasAttribute(KnownType.Microsoft_AspNetCore_Mvc_FromServicesAttribute))
        {
            var message = IsInConstructorInitializer(context.Operation.Instance.Syntax)
                ? "Refactor this constructor to avoid using members of parameter '{0}' because it could be {1}."
                : "Refactor this method to add validation of parameter '{0}' before using it.";
            ReportIssue(reference.WrappedOperation, string.Format(message, reference.WrappedOperation.Syntax, NullName), context);
        }

        return context.State;

        // Checks whether the null state of the parameter symbol is determined or if was assigned a new value after it was passed to the method.
        // In either of those cases the rule will not raise an issue for the parameter.
        static bool MissesObjectConstraint(SymbolicValue symbolState) =>
            symbolState is null
            || (!symbolState.HasConstraint<ObjectConstraint>() && !symbolState.HasConstraint<ParameterReassignedConstraint>());
    }

    protected override ProgramState PostProcessSimple(SymbolicContext context)
    {
        if (AssignmentTarget(context.Operation.Instance) is { Kind: OperationKindEx.ParameterReference } assignedParameter)
        {
            return context.SetSymbolConstraint(assignedParameter.ToParameterReference().Parameter, ParameterReassignedConstraint.Instance);
        }

        return context.State;
    }

    protected bool IsAccessibleFromOtherAssemblies() =>
        SemanticModel.GetDeclaredSymbol(Node).GetEffectiveAccessibility() is Public or Protected or ProtectedOrInternal;

    private static IOperation NullDereferenceCandidate(IOperation operation)
    {
        var candidate = operation.Kind switch
        {
            // C# extensions have Instance=Null, while VB extensions have it set.
            OperationKindEx.Invocation when operation.ToInvocation() is var invocation && !invocation.TargetMethod.IsExtensionMethod => invocation.Instance,
            OperationKindEx.FieldReference => operation.ToFieldReference().Instance,
            OperationKindEx.PropertyReference => operation.ToPropertyReference().Instance,
            OperationKindEx.EventReference => operation.ToEventReference().Instance,
            OperationKindEx.Await => operation.ToAwait().Operation,
            OperationKindEx.ArrayElementReference => operation.ToArrayElementReference().ArrayReference,
            OperationKindEx.MethodReference => operation.ToMethodReference().Instance,
            _ => null,
        };
        return candidate?.UnwrapConversion();
    }

    private static IOperation AssignmentTarget(IOperation operation) =>
        // Missing operation types: DeconstructionAssignment, CoalesceAssignment, CompoundAssignment, ThrowExpression
        operation.Kind == OperationKindEx.SimpleAssignment
            ? operation.ToAssignment().Target
            : null;
}
