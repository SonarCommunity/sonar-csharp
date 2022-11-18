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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SonarAnalyzer.CFG.Roslyn
{
    public abstract class ControlFlowGraphCacheBase
    {
        // We need to cache per compilation to avoid reusing CFGs when compilation object is altered by VS configuration changes
        private readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SyntaxNode, Wrapper>> compilationCache = new();

        protected abstract bool HasNestedCfg(SyntaxNode node);
        protected abstract bool IsLocalFunction(SyntaxNode node);

        public ControlFlowGraph FindOrCreate(SyntaxNode declaration, SemanticModel model, CancellationToken cancel)
        {
            var rootSyntax = model.GetOperation(declaration).RootOperation().Syntax;
            var nodeCache = compilationCache.GetValue(model.Compilation, x => new());
            if (!nodeCache.TryGetValue(rootSyntax, out var wrapper))
            {
                wrapper = new(ControlFlowGraph.Create(rootSyntax, model, cancel));
                nodeCache[rootSyntax] = wrapper;
            }
            if (HasNestedCfg(declaration))
            {
                // We need to go up and track all possible enclosing lambdas, local functions and other FlowAnonymousFunctionOperations
                foreach (var node in declaration.AncestorsAndSelf().TakeWhile(x => x != rootSyntax).Reverse())
                {
                    if (IsLocalFunction(node))
                    {
                        wrapper = new(wrapper.Cfg.GetLocalFunctionControlFlowGraph(node, cancel));
                    }
                    else if (wrapper.FlowOperation(node) is { WrappedOperation: not null } flowOperation)
                    {
                        wrapper = new(wrapper.Cfg.GetAnonymousFunctionControlFlowGraph(flowOperation, cancel));
                    }
                    else if (node == declaration)
                    {
                        return null;    // Lambda syntax is not always recognized as a FlowOperation for invalid syntaxes
                    }
                }
            }
            return wrapper.Cfg;
        }

        private sealed class Wrapper
        {
            private IFlowAnonymousFunctionOperationWrapper[] flowOperations;

            public ControlFlowGraph Cfg { get; }

            public Wrapper(ControlFlowGraph cfg) =>
                Cfg = cfg;

            public IFlowAnonymousFunctionOperationWrapper FlowOperation(SyntaxNode node)
            {
                flowOperations ??= Cfg.FlowAnonymousFunctionOperations().ToArray();  // Avoid recomputing, it's expensive and called many times for a single CFG
                return flowOperations.SingleOrDefault(x => x.WrappedOperation.Syntax == node);
            }
        }
    }
}
