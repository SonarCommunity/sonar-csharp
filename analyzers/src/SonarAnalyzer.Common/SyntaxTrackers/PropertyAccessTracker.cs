﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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
using SonarAnalyzer.Common;
using PropertyAccessCondition = SonarAnalyzer.Helpers.TrackingCondition<SonarAnalyzer.Helpers.PropertyAccessContext>;

namespace SonarAnalyzer.Helpers
{
    public abstract class PropertyAccessTracker<TSyntaxKind> : SyntaxTrackerBase<TSyntaxKind, PropertyAccessContext>
        where TSyntaxKind : struct
    {
        public abstract object AssignedValue(PropertyAccessContext context);
        public abstract PropertyAccessCondition MatchGetter();
        public abstract PropertyAccessCondition MatchSetter();
        public abstract PropertyAccessCondition AssignedValueIsConstant();
        protected abstract bool IsIdentifierWithinMemberAccess(SyntaxNode expression);
        protected abstract string GetPropertyName(SyntaxNode expression);

        private bool CaseInsensitiveComparison { get; }

        protected PropertyAccessTracker(IAnalyzerConfiguration analyzerConfiguration, DiagnosticDescriptor rule, bool caseInsensitiveComparison = false) : base(analyzerConfiguration, rule) =>
           CaseInsensitiveComparison = caseInsensitiveComparison;

        public PropertyAccessCondition MatchProperty(params MemberDescriptor[] properties) =>
            context => MemberDescriptor.MatchesAny(context.PropertyName, context.PropertySymbol, false, CaseInsensitiveComparison, properties);

        protected override SyntaxBaseContext CreateContext(SyntaxNode expression, SemanticModel semanticModel)
        {
            // We register for both MemberAccess and IdentifierName and we want to
            // avoid raising two times for the same identifier.
            if (IsIdentifierWithinMemberAccess(expression))
            {
                return null;
            }

            return GetPropertyName(expression) is string propertyName ? (SyntaxBaseContext)new PropertyAccessContext(expression, propertyName, semanticModel) : null;
        }
    }
}
