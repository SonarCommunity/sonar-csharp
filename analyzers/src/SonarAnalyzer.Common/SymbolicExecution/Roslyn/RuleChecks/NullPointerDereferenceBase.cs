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

using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.SymbolicExecution.Constraints;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks
{
    public abstract class NullPointerDereferenceBase : SymbolicRuleCheck
    {
        internal const string DiagnosticId = "S2259";

        protected override ProgramState PreProcessSimple(SymbolicContext context)
        {
            switch (context.Operation.Instance.Kind)
            {
                case OperationKindEx.Invocation:
                    if (context.Operation.Instance.ToInvocation() is { Instance: { } instance }
                        && context.HasConstraint(ObjectConstraint.Null, instance))
                    {
                        NodeContext.ReportIssue(Diagnostic.Create(Rule, instance.Syntax.GetLocation(), instance.Syntax.ToString()));
                    }
                    break;
                case OperationKindEx.ArrayElementReference:
                    if (IArrayElementReferenceOperationWrapper.FromOperation(context.Operation.Instance) is { ArrayReference: { } arrayReference }
                        && context.HasConstraint(ObjectConstraint.Null, arrayReference))
                    {
                        NodeContext.ReportIssue(Diagnostic.Create(Rule, arrayReference.Syntax.GetLocation(), arrayReference.Syntax.ToString()));
                    }
                    break;
            }
            return context.State;
        }
    }
}
