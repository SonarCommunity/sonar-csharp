﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using Microsoft.CodeAnalysis;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.Extensions
{
    internal static class IOperationExtensions
    {
        internal static ISymbol TrackedSymbol(this IOperation operation) =>
            operation switch
            {
                _ when IParameterReferenceOperationWrapper.IsInstance(operation) => IParameterReferenceOperationWrapper.FromOperation(operation).Parameter,
                _ when ILocalReferenceOperationWrapper.IsInstance(operation) => ILocalReferenceOperationWrapper.FromOperation(operation).Local,
                _ when IInvocationOperationWrapper.IsInstance(operation) => IInvocationOperationWrapper.FromOperation(operation).TargetMethod,
                _ when IConversionOperationWrapper.IsInstance(operation) => IConversionOperationWrapper.FromOperation(operation).Operand.TrackedSymbol(),
                _ when IFieldReferenceOperationWrapper.IsInstance(operation) => IFieldReferenceOperationWrapper.FromOperation(operation).Field,
                _ when IPropertyReferenceOperationWrapper.IsInstance(operation) => IPropertyReferenceOperationWrapper.FromOperation(operation).Property,
                _ when IArrayElementReferenceOperationWrapper.IsInstance(operation) => IArrayElementReferenceOperationWrapper.FromOperation(operation).ArrayReference.TrackedSymbol(),
                // ToDo: Implement the cases below and fix the SimpleAssignment tests.
                // _ => throw new NotSupportedException($"Unsupported operation type: {operation.Kind}")
                _ => null
            };
    }
}
