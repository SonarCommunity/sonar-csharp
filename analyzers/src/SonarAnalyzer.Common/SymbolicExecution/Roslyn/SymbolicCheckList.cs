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

namespace SonarAnalyzer.SymbolicExecution.Roslyn
{
    public class SymbolicCheckList
    {
        private readonly SymbolicCheck[] checks;

        public SymbolicCheckList(SymbolicCheck[] checks) =>
            this.checks = checks ?? throw new ArgumentNullException(nameof(checks));

        public ProgramState ConditionEvaluated(SymbolicContext context)
        {
            foreach (var check in checks)
            {
                if (check.ConditionEvaluated(context) is { } newState)
                {
                    context = context.WithState(newState);
                }
                else
                {
                    return null;
                }
            }
            return context.State;
        }

        public void ExitReached(SymbolicContext context)
        {
            foreach (var check in checks)
            {
                check.ExitReached(context);
            }
        }

        public void ExecutionCompleted()
        {
            foreach (var check in checks)
            {
                check.ExecutionCompleted();
            }
        }

        public SymbolicContext[] PreProcess(SymbolicContext context) =>
            InvokeChecks(context, (check, context) => check.PreProcess(context));

        public SymbolicContext[] PostProcess(SymbolicContext context) =>
            InvokeChecks(context, (check, context) => check.PostProcess(context));

        private SymbolicContext[] InvokeChecks(SymbolicContext context, Func<SymbolicCheck, SymbolicContext, ProgramState[]> process)
        {
            var contexts = new List<SymbolicContext>();
            foreach (var check in checks)
            {
                var newStates = process(check, context);
                foreach (var newState in newStates)
                {
                    context = context.WithState(newState);
                    contexts.Add(context);
                }
            }
            return contexts.ToArray();
        }
    }
}
