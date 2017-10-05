﻿/*
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
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SonarAnalyzer.SymbolicExecution.ControlFlowGraph
{
    public static class CSharpControlFlowGraph
    {
        public static IControlFlowGraph Create(CSharpSyntaxNode node, SemanticModel semanticModel)
        {
            return new CSharpControlFlowGraphBuilder(node, semanticModel).Build();
        }

        public static bool TryGet(CSharpSyntaxNode node, SemanticModel semanticModel, out IControlFlowGraph cfg)
        {
            cfg = null;
            try
            {
                if (node != null)
                {
                    cfg = Create(node, semanticModel);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exc) when (exc is InvalidOperationException ||
                                        exc is ArgumentException ||
                                        exc is NotSupportedException)
            {
                // These are expected
            }
            catch (Exception exc) when (exc is NotImplementedException)
            {
                Debug.Fail(exc.ToString());
            }

            return cfg != null;
        }
    }
}
