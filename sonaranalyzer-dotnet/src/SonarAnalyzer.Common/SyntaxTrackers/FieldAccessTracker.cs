﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;

namespace SonarAnalyzer.Helpers
{
    public delegate bool FieldAccessCondition(FieldAccessContext invocationContext);

    public abstract class FieldAccessTracker<TSyntaxKind> : SyntaxTrackerBase<TSyntaxKind>
        where TSyntaxKind : struct
    {
        protected FieldAccessTracker(IAnalyzerConfiguration analysisConfiguration, DiagnosticDescriptor rule)
            : base(analysisConfiguration, rule)
        {
        }

        protected abstract bool IsIdentifierWithinMemberAccess(SyntaxNode expression);

        protected abstract SyntaxNode GetIdentifier(SyntaxNode expression);

        public void Track(SonarAnalysisContext context, params FieldAccessCondition[] conditions)
        {
            context.RegisterCompilationStartAction(
                c =>
                {
                    if (IsEnabled(c.Options))
                    {
                        c.RegisterSyntaxNodeActionInNonGenerated(
                            GeneratedCodeRecognizer,
                            TrackMemberAccess,
                            TrackedSyntaxKinds);
                    }
                });

            void TrackMemberAccess(SyntaxNodeAnalysisContext c)
            {
                if (IsTrackedField(c.Node, c.SemanticModel))
                {
                    c.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, c.Node.GetLocation()));
                }
            }

            bool IsTrackedField(SyntaxNode expression, SemanticModel semanticModel)
            {
                // We register for both MemberAccess and IdentifierName and we want to
                // avoid raising two times for the same identifier.
                if (IsIdentifierWithinMemberAccess(expression))
                {
                    return false;
                }

                var identifier = GetIdentifier(expression);
                if (identifier == null)
                {
                    return false;
                }

                var conditionContext = new FieldAccessContext(expression, identifier, semanticModel);
                return conditions.All(c => c(conditionContext));
            }
        }

        #region Syntax-level standard conditions

        public abstract FieldAccessCondition MatchSimpleNames(params MethodSignature[] methods);

        public abstract FieldAccessCondition MatchGet();

        public abstract FieldAccessCondition MatchSet();

        public abstract FieldAccessCondition AssignedValueIsConstant();

        #endregion

        #region Symbol-level standard conditions

        // Add any common symbol-level checks here...

        #endregion
    }
}
