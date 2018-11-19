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

using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class UsingRegularExpressionsBase<TSyntaxKind> : SonarDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected const string DiagnosticId = "S4784";
        protected const string MessageFormat = "Make sure that using a regular expression is safe here.";

        protected InvocationTracker<TSyntaxKind> InvocationTracker { get; set; }

        protected ObjectCreationTracker<TSyntaxKind> ObjectCreationTracker { get; set; }

        protected override void Initialize(SonarAnalysisContext context)
        {
            InvocationTracker.Track(context,
                InvocationTracker.MatchSimpleNames(
                    new MethodSignature(KnownType.System_Text_RegularExpressions_Regex, "IsMatch"),
                    new MethodSignature(KnownType.System_Text_RegularExpressions_Regex, "Match"),
                    new MethodSignature(KnownType.System_Text_RegularExpressions_Regex, "Matches"),
                    new MethodSignature(KnownType.System_Text_RegularExpressions_Regex, "Replace"),
                    new MethodSignature(KnownType.System_Text_RegularExpressions_Regex, "Split")),
                InvocationTracker.IsStatic());

            ObjectCreationTracker.Track(context,
                ObjectCreationTracker.MatchConstructors(
                    KnownType.System_Text_RegularExpressions_Regex));
        }
    }
}
