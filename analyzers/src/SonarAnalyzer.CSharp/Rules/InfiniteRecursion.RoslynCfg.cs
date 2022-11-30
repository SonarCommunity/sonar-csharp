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

using SonarAnalyzer.CFG.Roslyn;
using CfgAllPathValidator = SonarAnalyzer.CFG.Roslyn.CfgAllPathValidator;

namespace SonarAnalyzer.Rules.CSharp
{
    public partial class InfiniteRecursion
    {
        private sealed class RoslynChecker : IChecker
        {
            public void CheckForNoExitProperty(SyntaxNodeAnalysisContext c, PropertyDeclarationSyntax property, IPropertySymbol propertySymbol)
            {
                if (property.ExpressionBody?.Expression != null)
                {
                    var cfg = ControlFlowGraph.Create(property.ExpressionBody, c.SemanticModel, c.CancellationToken);
                    var walker = new RecursionSearcher(new RecursionContext<ControlFlowGraph>(cfg, propertySymbol, property.Identifier.GetLocation(), c, "property's recursion"));
                    walker.CheckPaths();
                }
                else if (property.AccessorList != null)
                {
                    foreach (var accessor in property.AccessorList.Accessors.Where(a => a.HasBodyOrExpressionBody()))
                    {
                        var cfg = ControlFlowGraph.Create(accessor, c.SemanticModel, c.CancellationToken);
                        var context = new RecursionContext<ControlFlowGraph>(cfg, propertySymbol, accessor.Keyword.GetLocation(), c, "property accessor's recursion");
                        var walker = new RecursionSearcher(context, !accessor.Keyword.IsKind(SyntaxKind.SetKeyword));
                        walker.CheckPaths();
                    }
                }
            }

            public void CheckForNoExitMethod(SyntaxNodeAnalysisContext c, CSharpSyntaxNode body, SyntaxToken identifier, IMethodSymbol symbol)
            {
                if (body.CreateCfg(c.SemanticModel, c.CancellationToken) is { } cfg)
                {
                    var context = new RecursionContext<ControlFlowGraph>(cfg, symbol, identifier.GetLocation(), c, "method's recursion");
                    var walker = new RecursionSearcher(context);
                    walker.CheckPaths();
                }
            }

            private sealed class RecursionSearcher : CfgAllPathValidator
            {
                private readonly RecursionContext<ControlFlowGraph> context;
                private readonly bool isGetAccesor;

                public RecursionSearcher(RecursionContext<ControlFlowGraph> context, bool isGetAccesor = true)
                    : base(context.ControlFlowGraph)
                {
                    this.context = context;
                    this.isGetAccesor = isGetAccesor;
                }

                public void CheckPaths()
                {
                    if (!CfgCanExit() || CheckAllPaths())
                    {
                        context.ReportIssue();
                    }
                }

                protected override bool IsValid(BasicBlock block)
                {
                    if (block.OperationsAndBranchValue.ToReversedExecutionOrder().FirstOrDefault(x => context.AnalyzedSymbol.Equals(MemberSymbol(x.Instance))) is { } operation)
                    {
                        var isWrite = operation.Parent is { Kind: OperationKindEx.SimpleAssignment } parent
                                      && ISimpleAssignmentOperationWrapper.FromOperation(parent).Target == operation.Instance;
                        return isGetAccesor ^ isWrite;
                    }

                    return false;

                    static ISymbol MemberSymbol(IOperation operation) =>
                        operation.Kind switch
                        {
                            OperationKindEx.PropertyReference
                                when IPropertyReferenceOperationWrapper.FromOperation(operation) is var propertyReference && InstanceReferencesThis(propertyReference.Instance) =>
                                propertyReference.Property,
                            OperationKindEx.Invocation
                                when IInvocationOperationWrapper.FromOperation(operation) is var invocation && (!invocation.IsVirtual || InstanceReferencesThis(invocation.Instance)) =>
                                invocation.TargetMethod,
                            OperationKindEx.Binary
                                when IBinaryOperationWrapper.FromOperation(operation) is var binaryOperation =>
                                binaryOperation.OperatorMethod,
                            OperationKindEx.Decrement
                                when IIncrementOrDecrementOperationWrapper.FromOperation(operation) is var decrementOperation =>
                                decrementOperation.OperatorMethod,
                            OperationKindEx.Increment
                                when IIncrementOrDecrementOperationWrapper.FromOperation(operation) is var incrementOperation =>
                                incrementOperation.OperatorMethod,
                            OperationKindEx.Unary
                                when IUnaryOperationWrapper.FromOperation(operation) is var unaryOperation =>
                                unaryOperation.OperatorMethod,
                            _ => null
                        };

                    static bool InstanceReferencesThis(IOperation instance) =>
                        instance == null || instance.IsAnyKind(OperationKindEx.FlowCaptureReference, OperationKindEx.InstanceReference);
                }

                protected override bool IsInvalid(BasicBlock block) => false;

                private bool CfgCanExit() =>
                    context.ControlFlowGraph.ExitBlock.IsReachable
                    || context.ControlFlowGraph.Blocks.Any(x => x.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Throw && x.IsReachable);
            }
        }
    }
}
