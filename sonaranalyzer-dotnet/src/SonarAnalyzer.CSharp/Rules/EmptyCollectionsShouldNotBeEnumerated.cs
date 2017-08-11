/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Helpers.FlowAnalysis.Common;
using SonarAnalyzer.Helpers.FlowAnalysis.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class EmptyCollectionsShouldNotBeEnumerated : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4158";
        private const string MessageFormat = "Remove this call, the collection is known to be empty here.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        private static readonly ISet<KnownType> TrackedCollectionTypes = new HashSet<KnownType>
        {
            KnownType.System_Collections_Generic_Dictionary_TKey_TValue,
            KnownType.System_Collections_Generic_List_T,
            KnownType.System_Collections_Generic_Queue_T,
            KnownType.System_Collections_Generic_Stack_T,
            KnownType.System_Collections_Generic_HashSet_T,
            KnownType.System_Collections_ObjectModel_ObservableCollection_T,
            KnownType.System_Array,
        };

        private static readonly HashSet<string> AddMethods = new HashSet<string>
        {
            nameof(List<object>.Add),
            nameof(List<object>.AddRange),
            nameof(List<object>.Insert),
            nameof(List<object>.InsertRange),
            nameof(Queue<object>.Enqueue),
            nameof(Stack<object>.Push),
            nameof(HashSet<object>.Add),
            nameof(HashSet<object>.UnionWith),
        };

        private static readonly HashSet<string> IgnoredMethods = new HashSet<string>
        {
            nameof(List<object>.GetHashCode),
            nameof(List<object>.Equals),
            nameof(List<object>.GetType),
            nameof(List<object>.ToString),
            nameof(List<object>.ToArray),
            nameof(Array.GetLength),
            nameof(Array.GetLongLength),
            nameof(Array.GetLowerBound),
            nameof(Array.GetUpperBound),
            nameof(Dictionary<object, object>.ContainsKey),
            nameof(Dictionary<object, object>.ContainsValue),
            nameof(Dictionary<object, object>.GetObjectData),
            nameof(Dictionary<object, object>.OnDeserialization),
            nameof(Dictionary<object, object>.TryGetValue),
        };

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterExplodedGraphBasedAnalysis(CheckForEmptyCollectionAccess);
        }

        private void CheckForEmptyCollectionAccess(ExplodedGraph explodedGraph, SyntaxNodeAnalysisContext context)
        {
            var check = new EmptyCollectionAccessedCheck(explodedGraph);
            explodedGraph.AddExplodedGraphCheck(check);

            var emptyCollections = new HashSet<SyntaxNode>();
            var nonEmptyCollections = new HashSet<SyntaxNode>();

            EventHandler<CollectionAccessedEventArgs> collectionAccessedHandler = (sender, args) =>
                (args.IsEmpty ? emptyCollections : nonEmptyCollections).Add(args.Node);

            check.CollectionAccessed += collectionAccessedHandler;
            try
            {
                explodedGraph.Walk();
            }
            finally
            {
                check.CollectionAccessed -= collectionAccessedHandler;
            }

            foreach (var node in emptyCollections.Except(nonEmptyCollections))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, node.GetLocation()));
            }
        }

        internal sealed class EmptyCollectionAccessedCheck : ExplodedGraphCheck
        {
            public event EventHandler<CollectionAccessedEventArgs> CollectionAccessed;

            public EmptyCollectionAccessedCheck(ExplodedGraph explodedGraph)
                : base(explodedGraph)
            {
            }

            private void OnCollectionAccessed(SyntaxNode node, bool empty)
            {
                CollectionAccessed?.Invoke(this, new CollectionAccessedEventArgs(node, empty));
            }

            public override ProgramState PreProcessInstruction(ProgramPoint programPoint, ProgramState programState)
            {
                var instruction = programPoint.Block.Instructions[programPoint.Offset];

                switch (instruction.Kind())
                {
                    case SyntaxKind.InvocationExpression:
                        return ProcessInvocation(programState, (InvocationExpressionSyntax)instruction);
                    case SyntaxKind.ElementAccessExpression:
                        return ProcessElementAccess(programState, (ElementAccessExpressionSyntax)instruction);
                    default:
                        return programState;
                }
            }

            private ProgramState ProcessInvocation(ProgramState programState, InvocationExpressionSyntax invocation)
            {
                var newProgramState = RemoveCollectionConstraintsFromArguments(invocation, programState);

                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess == null)
                {
                    return newProgramState;
                }

                var collectionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                var collectionType = GetCollectionType(collectionSymbol);

                // When invoking a collection method ...
                if (collectionType.IsAny(TrackedCollectionTypes))
                {
                    var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (IsIgnoredMethod(methodSymbol))
                    {
                        // ... ignore some methods that are irrelevant
                        return newProgramState;
                    }

                    if (AddMethods.Contains(methodSymbol.Name))
                    {
                        // ... set constraint if we are adding items
                        newProgramState = collectionSymbol.SetConstraint(CollectionCapacityConstraint.NotEmpty,
                            newProgramState);
                    }
                    else
                    {
                        // ... notify we are accessing the collection
                        OnCollectionAccessed(invocation,
                            collectionSymbol.HasConstraint(CollectionCapacityConstraint.Empty, newProgramState));
                    }
                }

                return newProgramState;
            }

            private ProgramState ProcessElementAccess(ProgramState programState, ElementAccessExpressionSyntax elementAccess)
            {
                var collectionSymbol = semanticModel.GetSymbolInfo(elementAccess.Expression).Symbol;
                var collectionType = GetCollectionType(collectionSymbol);

                // When accessing elements from a collection ...
                if (collectionType?.ConstructedFrom != null &&
                    collectionType.ConstructedFrom.IsAny(TrackedCollectionTypes))
                {
                    if (collectionType.ConstructedFrom.Is(KnownType.System_Collections_Generic_Dictionary_TKey_TValue) &&
                        IsDictionarySetItem(elementAccess))
                    {
                        // ... set constraint if we are calling the Dictionary set accessor
                        return collectionSymbol.SetConstraint(CollectionCapacityConstraint.NotEmpty, programState);
                    }
                    else
                    {
                        // ... notify we are accessing the collection
                        OnCollectionAccessed(elementAccess,
                            collectionSymbol.HasConstraint(CollectionCapacityConstraint.Empty, programState));
                    }
                }

                return programState;
            }

            public override ProgramState ObjectCreated(ProgramState programState, SymbolicValue symbolicValue,
                SyntaxNode instruction)
            {
                CollectionCapacityConstraint constraint = null;

                if (instruction.IsKind(SyntaxKind.ObjectCreationExpression))
                {
                    // When a collection is being created ...
                    var objectCreationSyntax = (ObjectCreationExpressionSyntax)instruction;

                    var constructor = semanticModel.GetSymbolInfo(objectCreationSyntax).Symbol as IMethodSymbol;
                    if (IsCollectionConstructor(constructor))
                    {
                        // ... try to devise what constraint could be applied by the constructor or the initializer
                        constraint =
                            GetInitializerConstraint(objectCreationSyntax.Initializer) ??
                            GetCollectionConstraint(constructor);
                    }
                }
                else if (instruction.IsKind(SyntaxKind.ArrayCreationExpression))
                {
                    // When an array is being created ...
                    var arrayCreationSyntax = (ArrayCreationExpressionSyntax)instruction;

                    // ... try to devise what constraint could be applied by the array size or the initializer
                    constraint =
                        GetInitializerConstraint(arrayCreationSyntax.Initializer) ??
                        GetArrayConstraint(arrayCreationSyntax);
                }

                return constraint != null
                    ? symbolicValue.SetConstraint(constraint, programState)
                    : programState;
            }

            private static bool IsIgnoredMethod(IMethodSymbol methodSymbol)
            {
                return methodSymbol == null
                    || methodSymbol.IsExtensionMethod
                    || IgnoredMethods.Contains(methodSymbol.Name);
            }

            private CollectionCapacityConstraint GetArrayConstraint(ArrayCreationExpressionSyntax arrayCreation)
            {
                // Only one-dimensional arrays can be empty, others are indeterminate, because this code becomes ugly
                if (arrayCreation.Type.RankSpecifiers.Count != 1 ||
                    arrayCreation.Type.RankSpecifiers[0].Sizes.Count != 1)
                {
                    return null;
                }

                var size = arrayCreation.Type.RankSpecifiers[0].Sizes[0] as LiteralExpressionSyntax;

                return size?.Token.ValueText == "0"
                    ? CollectionCapacityConstraint.Empty
                    : null;
            }

            private CollectionCapacityConstraint GetCollectionConstraint(IMethodSymbol constructor)
            {
                // Default constructor, or constructor that specifies capacity means empty collection,
                // otherwise do not apply constraint because we cannot be sure what has been passed
                // as arguments.
                var defaultCtorOrCapacityCtor = !constructor.Parameters.Any()
                    || constructor.Parameters.Count(p => p.IsType(KnownType.System_Int32)) == 1;

                return defaultCtorOrCapacityCtor ? CollectionCapacityConstraint.Empty : null;
            }

            private CollectionCapacityConstraint GetInitializerConstraint(InitializerExpressionSyntax initializer)
            {
                if (initializer == null)
                {
                    return null;
                }

                return initializer.Expressions.Count == 0
                    ? CollectionCapacityConstraint.Empty // No items added through the initializer
                    : CollectionCapacityConstraint.NotEmpty;
            }

            private static ProgramState RemoveCollectionConstraintsFromArguments(InvocationExpressionSyntax invocation,
                ProgramState programState)
            {
                // Remove the collection constraints from all arguments of the invocation expression

                var values = invocation.ArgumentList.Arguments.Select((node, index) =>
                {
                    SymbolicValue value;
                    programState.ExpressionStack.Pop(out value);
                    return value;
                }).ToList();

                foreach (var value in values)
                {
                    programState = value.RemoveConstraint(CollectionCapacityConstraint.Empty, programState);
                }

                return programState;
            }

            private static bool IsDictionarySetItem(ElementAccessExpressionSyntax elementAccess) =>
                (elementAccess.GetSelfOrTopParenthesizedExpression().Parent as AssignmentExpressionSyntax)
                    ?.Left.RemoveParentheses() == elementAccess;

            private static bool IsCollectionConstructor(IMethodSymbol constructorSymbol) =>
                constructorSymbol?.ContainingType?.ConstructedFrom != null &&
                constructorSymbol.ContainingType.ConstructedFrom.IsAny(TrackedCollectionTypes);

            private static INamedTypeSymbol GetCollectionType(ISymbol collectionSymbol) =>
                (collectionSymbol.GetSymbolType() as INamedTypeSymbol)?.ConstructedFrom ?? // collections
                collectionSymbol.GetSymbolType()?.BaseType; // arrays
        }

        internal sealed class CollectionAccessedEventArgs : EventArgs
        {
            public SyntaxNode Node { get; }
            public bool IsEmpty { get; }

            public CollectionAccessedEventArgs(SyntaxNode node, bool isEmpty)
            {
                Node = node;
                IsEmpty = isEmpty;
            }
        }
    }
}
