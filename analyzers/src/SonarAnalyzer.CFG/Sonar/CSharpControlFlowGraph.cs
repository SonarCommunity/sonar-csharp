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

namespace SonarAnalyzer.CFG.Sonar
{
    public static class CSharpControlFlowGraph
    {
        public static bool TryGet(SyntaxNode node, SemanticModel semanticModel, out IControlFlowGraph cfg)
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
                // historically, these have been considered as expected
                // but we should be aware of what syntax we do not yet support
                // https://github.com/SonarSource/sonar-dotnet/issues/2541
            }
            catch (Exception exc) when (exc is NotImplementedException)
            {
                Debug.Fail(exc.ToString());
            }

            return cfg != null;
        }

        internal /* for testing */ static IControlFlowGraph Create(SyntaxNode node, SemanticModel semanticModel) =>
            new CSharpControlFlowGraphBuilder(node, semanticModel).Build();
    }
}
