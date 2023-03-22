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

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks.CSharp;

public class PublicMethodArgumentsShouldBeCheckedForNull : SymbolicRuleCheck
{
    private const string DiagnosticId = "S3900";
    private const string MessageFormat = "{0}";

    internal static readonly DiagnosticDescriptor S3900 = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    protected override DiagnosticDescriptor Rule => S3900;

    public override bool ShouldExecute() => true;

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        if (context.Operation.Instance.Kind == OperationKindEx.ParameterReference
            && context.Operation.Instance.ToParameterReference() is { WrappedOperation: var reference, WrappedOperation.Syntax: var syntax, Parameter: var parameterSymbol }
            && !parameterSymbol.Type.IsValueType
            && IsParameterDereferenced(context.Operation)
            && !IgnoreBecauseOfParameterAttribute(parameterSymbol)
            && NullableStateIsNotKnownForParameter(parameterSymbol)
            && SemanticModel.GetDeclaredSymbol(Node) is { } methodSymbol)
        {
            var message = methodSymbol.IsConstructor()
                ? "Refactor this constructor to avoid using members of parameter '{0}' because it could be null."
                : "Refactor this method to add validation of parameter '{0}' before using it.";
            ReportIssue(reference, string.Format(message, syntax.ToString()), context);
        }

        return context.State;

        static bool IsParameterDereferenced(IOperationWrapperSonar operation) =>
            operation.Parent != null
            && operation.Parent.IsAnyKind(
                OperationKindEx.Invocation,
                OperationKindEx.FieldReference,
                OperationKindEx.PropertyReference,
                OperationKindEx.EventReference,
                OperationKindEx.Await,
                OperationKindEx.ArrayElementReference);

        bool IgnoreBecauseOfParameterAttribute(IParameterSymbol symbol) =>
            symbol.HasAttribute(KnownType.Microsoft_AspNetCore_Mvc_FromServicesAttribute);

        bool NullableStateIsNotKnownForParameter(IParameterSymbol symbol) =>
            !context.HasConstraint(symbol, ObjectConstraint.Null)
            && !context.HasConstraint(symbol, ObjectConstraint.NotNull)
            && SemanticModel.GetTypeInfo(context.Operation.Instance.Syntax).Nullability().FlowState != NullableFlowState.NotNull;
    }
}
