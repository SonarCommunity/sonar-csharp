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

namespace SonarAnalyzer.SymbolicExecution.Roslyn.OperationProcessors;

internal sealed class Argument : SimpleProcessor<IArgumentOperationWrapper>
{
    protected override IArgumentOperationWrapper Convert(IOperation operation) =>
        IArgumentOperationWrapper.FromOperation(operation);

    protected override ProgramState Process(SymbolicContext context, IArgumentOperationWrapper argument) =>
        ProcessArgument(context.State, argument) ?? context.State;

    private static ProgramState ProcessArgument(ProgramState state, IArgumentOperationWrapper argument)
    {
        if (argument.Parameter is null)
        {
            return null; // __arglist is not assigned to a parameter
        }
        if (argument is { Parameter.RefKind: RefKind.Out or RefKind.Ref } && argument.Value.TrackedSymbol() is { } symbol)
        {
            state = state.SetSymbolValue(symbol, null); // Forget state for "out" or "ref" arguments
        }
        if (argument.Parameter.GetAttributes() is { Length: > 0 } attributes)
        {
            state = ProcessArgumentAttributes(state, argument, attributes);
        }
        return state;
    }

    private static ProgramState ProcessArgumentAttributes(ProgramState state, IArgumentOperationWrapper argument, ImmutableArray<AttributeData> attributes) =>
        attributes.Any(IsNotNullAttribute) && argument.Value.TrackedSymbol() is { } symbol
            ? state.SetSymbolConstraint(symbol, ObjectConstraint.NotNull)
            : state;

    // https://docs.microsoft.com/dotnet/api/microsoft.validatednotnullattribute
    // https://docs.microsoft.com/dotnet/csharp/language-reference/attributes/nullable-analysis#postconditions-maybenull-and-notnull
    // https://www.jetbrains.com/help/resharper/Reference__Code_Annotation_Attributes.html#NotNullAttribute
    private static bool IsNotNullAttribute(AttributeData attribute) =>
        attribute.HasAnyName("ValidatedNotNullAttribute", "NotNullAttribute");
}
