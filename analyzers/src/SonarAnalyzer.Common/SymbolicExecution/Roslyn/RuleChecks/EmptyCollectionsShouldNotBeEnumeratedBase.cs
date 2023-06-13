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

using System.Collections;
using System.Collections.ObjectModel;
using SonarAnalyzer.SymbolicExecution.Sonar.Constraints;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks;

public abstract class EmptyCollectionsShouldNotBeEnumeratedBase : SymbolicRuleCheck
{
    protected const string DiagnosticId = "S4158";
    protected const string MessageFormat = "Remove this call, the collection is known to be empty here.";

    private static readonly KnownType[] TrackedCollectionTypes = new[]
    {
        KnownType.System_Collections_Generic_List_T,
        KnownType.System_Collections_Generic_HashSet_T,
        KnownType.System_Collections_Generic_Queue_T,
        KnownType.System_Collections_Generic_Stack_T,
        KnownType.System_Collections_ObjectModel_ObservableCollection_T,
        KnownType.System_Array,
        KnownType.System_Collections_Generic_Dictionary_TKey_TValue
    };

    private static readonly HashSet<string> RaisingMethods = new()
    {
        nameof(IEnumerable<int>.GetEnumerator),
        nameof(ICollection.CopyTo),
        nameof(ICollection<int>.Clear),
        nameof(ICollection<int>.Contains),
        nameof(ICollection<int>.Remove),
        nameof(List<int>.BinarySearch),
        nameof(List<int>.ConvertAll),
        nameof(List<int>.Exists),
        nameof(List<int>.Find),
        nameof(List<int>.FindAll),
        nameof(List<int>.FindIndex),
        nameof(List<int>.FindLast),
        nameof(List<int>.FindLastIndex),
        nameof(List<int>.ForEach),
        nameof(List<int>.GetRange),
        nameof(List<int>.IndexOf),
        nameof(List<int>.LastIndexOf),
        nameof(List<int>.RemoveAll),
        nameof(List<int>.RemoveAt),
        nameof(List<int>.RemoveRange),
        nameof(List<int>.Reverse),
        nameof(List<int>.Sort),
        nameof(List<int>.TrueForAll),
        nameof(HashSet<int>.ExceptWith),
        nameof(HashSet<int>.IntersectWith),
        nameof(HashSet<int>.IsProperSubsetOf),
        nameof(HashSet<int>.IsProperSupersetOf),
        nameof(HashSet<int>.IsSubsetOf),
        nameof(HashSet<int>.IsSupersetOf),
        nameof(HashSet<int>.Overlaps),
        nameof(HashSet<int>.RemoveWhere),
        nameof(HashSet<int>.SymmetricExceptWith),
        nameof(HashSet<int>.UnionWith),
        nameof(Queue<int>.Dequeue),
        nameof(Queue<int>.Peek),
        "TryDequeue",
        "TryPeek",
        nameof(Stack<int>.Pop),
        "TryPop",
        nameof(ObservableCollection<int>.Move),
        nameof(Array.Clone),
        nameof(Array.GetLength),
        nameof(Array.GetLongLength),
        nameof(Array.GetLowerBound),
        nameof(Array.GetUpperBound),
        nameof(Array.GetValue),
        nameof(Array.Initialize),
        nameof(Array.SetValue),
        nameof(Dictionary<int, int>.ContainsKey),
        nameof(Dictionary<int, int>.ContainsValue),
        nameof(Dictionary<int, int>.TryGetValue)
    };

    private static readonly HashSet<string> AddMethods = new()
    {
        nameof(ICollection<int>.Add),
        nameof(List<int>.AddRange),
        nameof(List<int>.Insert),
        nameof(List<int>.InsertRange),
        nameof(HashSet<int>.SymmetricExceptWith),
        nameof(HashSet<int>.UnionWith),
        nameof(Queue<int>.Enqueue),
        nameof(Stack<int>.Push),
        nameof(Collection<int>.Insert),
        "TryAdd"
    };

    private readonly HashSet<IOperation> emptyAccess = new();
    private readonly HashSet<IOperation> nonEmptyAccess = new();

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        var operation = context.Operation.Instance;
        if (operation.AsObjectCreation()?.Type.IsAny(TrackedCollectionTypes) ?? false)
        {
            return context.State.SetOperationConstraint(operation, CollectionConstraint.Empty);
        }
        else if (operation.AsInvocation() is { Instance: not null } invocation)
        {
            if (RaisingMethods.Contains(invocation.TargetMethod.Name))
            {
                if (context.State[invocation.Instance]?.HasConstraint(CollectionConstraint.Empty) is true)
                {
                    emptyAccess.Add(operation);
                }
                else
                {
                    nonEmptyAccess.Add(operation);
                }
            }
            if (AddMethods.Contains(invocation.TargetMethod.Name) && invocation.Instance.TrackedSymbol() is { } symbol)
            {
                return context.State.SetSymbolConstraint(symbol, CollectionConstraint.NotEmpty);
            }
        }
        return context.State;
    }

    public override void ExecutionCompleted()
    {
        foreach (var operation in emptyAccess.Except(nonEmptyAccess))
        {
            ReportIssue(operation, operation.Syntax.ToString());
        }
    }
}
