﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Extensions
{
    public static class IOperationExtensions
    {
        public static bool IsOutArgumentReference(this IOperation operation) =>
            new IOperationWrapperSonar(operation) is var wrapped
            && IArgumentOperationWrapper.IsInstance(wrapped.Parent)
            && IArgumentOperationWrapper.FromOperation(wrapped.Parent).Parameter.RefKind == RefKind.Out;

        public static bool IsAssignmentTarget(this IOperationWrapper operation) =>
            new IOperationWrapperSonar(operation.WrappedOperation).Parent is { } parent
            && ISimpleAssignmentOperationWrapper.IsInstance(parent)
            && ISimpleAssignmentOperationWrapper.FromOperation(parent).Target == operation.WrappedOperation;

        public static bool IsCompoundAssignmentTarget(this IOperationWrapper operation) =>
            new IOperationWrapperSonar(operation.WrappedOperation).Parent is { } parent
            && ICompoundAssignmentOperationWrapper.IsInstance(parent)
            && ICompoundAssignmentOperationWrapper.FromOperation(parent).Target == operation.WrappedOperation;

        public static bool IsOutArgument(this IOperationWrapper operation) =>
            new IOperationWrapperSonar(operation.WrappedOperation).Parent is { } parent
            && IArgumentOperationWrapper.IsInstance(parent)
            && IArgumentOperationWrapper.FromOperation(parent).Parameter.RefKind == RefKind.Out;

        public static bool IsAnyKind(this IOperation operation, params OperationKind[] kinds) =>
            kinds.Contains(operation.Kind);

        public static IOperation RootOperation(this IOperation operation)
        {
            var wrapper = new IOperationWrapperSonar(operation);
            while (wrapper.Parent != null)
            {
                wrapper = new IOperationWrapperSonar(wrapper.Parent);
            }
            return wrapper.Instance;
        }

        public static OperationExecutionOrder ToExecutionOrder(this IEnumerable<IOperation> operations) =>
            new OperationExecutionOrder(operations, false);

        public static OperationExecutionOrder ToReversedExecutionOrder(this IEnumerable<IOperation> operations) =>
            new OperationExecutionOrder(operations, true);

        public static string Serialize(this IOperation operation) =>
            $"{OperationPrefix(operation)}{OperationSuffix(operation)} / {operation.Syntax.GetType().Name}: {operation.Syntax}";

        // This method is taken from Roslyn implementation
        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation) =>
            Descendants(operation, true);

        // This method is taken from Roslyn implementation
        private static IEnumerable<IOperation> Descendants(IOperation operation, bool includeSelf)
        {
            if (operation == null)
            {
                yield break;
            }
            if (includeSelf)
            {
                yield return operation;
            }
            var stack = new Stack<IEnumerator<IOperation>>();
            stack.Push(new IOperationWrapperSonar(operation).Children.GetEnumerator());
            while (stack.Any())
            {
                var iterator = stack.Pop();
                if (!iterator.MoveNext())
                {
                    continue;
                }

                stack.Push(iterator);
                if (iterator.Current is { } current)
                {
                    yield return current;
                    stack.Push(new IOperationWrapperSonar(current).Children.GetEnumerator());
                }
            }
        }

        private static string OperationPrefix(IOperation op) =>
            op.Kind == OperationKindEx.Invalid ? "INVALID" : op.GetType().Name;

        private static string OperationSuffix(IOperation op) =>
            op switch
            {
                var _ when IInvocationOperationWrapper.IsInstance(op) => ": " + IInvocationOperationWrapper.FromOperation(op).TargetMethod.Name,
                var _ when IFlowCaptureOperationWrapper.IsInstance(op) => ": #" + IFlowCaptureOperationWrapper.FromOperation(op).Id.GetHashCode(),
                var _ when IFlowCaptureReferenceOperationWrapper.IsInstance(op) => ": #" + IFlowCaptureReferenceOperationWrapper.FromOperation(op).Id.GetHashCode(),
                _ => null
            };
    }
}
