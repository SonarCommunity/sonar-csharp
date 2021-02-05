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

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class CommandPathBase<TSyntaxKind> : SonarDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected const string DiagnosticId = "S4036";
        private const string MessageFormat = "Make sure the \"PATH\" used to find this command includes only what you intend.";

        private readonly Regex validPath = new Regex(@"^(\.{0,2}[\\/]|\w+:)");

        private readonly IAnalyzerConfiguration configuration;
        private readonly DiagnosticDescriptor rule;

        protected abstract ILanguageFacade<TSyntaxKind> Language { get; }
        protected abstract string FirstArgument(InvocationContext context);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected CommandPathBase(IAnalyzerConfiguration configuration, System.Resources.ResourceManager rspecResources)
        {
            this.configuration = configuration;
            rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, rspecResources).WithNotConfigurable();
        }

        protected override void Initialize(SonarAnalysisContext context)
        {
            var input = new TrackerInput(context, configuration, rule);

            var inv = Language.Tracker.Invocation;
            inv.Track(input,
                inv.MatchMethod(new MemberDescriptor(KnownType.System_Diagnostics_Process, "Start")),
                c => IsInvalid(FirstArgument(c)));

            var pa = Language.Tracker.PropertyAccess;
            pa.Track(input,
                pa.MatchProperty(new MemberDescriptor(KnownType.System_Diagnostics_ProcessStartInfo, "FileName")),
                pa.MatchSetter(),
                c => IsInvalid((string)pa.AssignedValue(c)));

            var oc = Language.Tracker.ObjectCreation;
            oc.Track(input,
                oc.MatchConstructor(KnownType.System_Diagnostics_ProcessStartInfo),
                c => oc.ConstArgumentForParameter(c, "fileName") is string value && IsInvalid(value));
        }

        private bool IsInvalid(string path) =>
            !string.IsNullOrEmpty(path) && !validPath.IsMatch(path);
    }
}
