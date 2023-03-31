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

public abstract class EmptyNullableValueAccessBase : SymbolicRuleCheck
{
    internal const string DiagnosticId = "S3655";

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        var operationInstance = context.Operation.Instance;
        if (operationInstance.Kind == OperationKindEx.PropertyReference
            && operationInstance.ToPropertyReference() is var reference
            && reference.Property.Name == nameof(Nullable<int>.Value)
            && reference.Instance is { } instance
            && instance.Type.IsNullableValueType()
            && context.HasConstraint(instance, ObjectConstraint.Null))
        {
            ReportIssue(instance, instance.Syntax.ToString());
        }
        else if (operationInstance.Kind == OperationKindEx.Conversion
            && operationInstance.ToConversion() is var conversion
            && conversion.Operand.Type.IsNullableValueType()
            && conversion.Type.IsNonNullableValueType()
            && context.HasConstraint(conversion.Operand, ObjectConstraint.Null))
        {
            ReportIssue(conversion.Operand, conversion.Operand.Syntax.ToString());
        }

        return context.State;
    }
}
