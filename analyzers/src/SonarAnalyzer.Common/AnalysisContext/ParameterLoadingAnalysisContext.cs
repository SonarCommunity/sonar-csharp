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

using Microsoft.CodeAnalysis.Text;

namespace SonarAnalyzer.Helpers;

public sealed class ParameterLoadingAnalysisContext : SonarAnalysisContextBase // FIXME: Refactor: Use Sonar* prefix, change contract not to expose the collection, rename everything to "Postponed" to make it clear, add docs
{
    private readonly List<Action<SonarCompilationStartAnalysisContext>> postponedActions = new();

    public SonarAnalysisContext Context { get; }

    internal ParameterLoadingAnalysisContext(SonarAnalysisContext context) =>
        Context = context;

    /// <summary>
    /// Register CompilationStart action that will be executed once rule parameters are set.
    /// </summary>
    public void RegisterPostponedAction(Action<SonarCompilationStartAnalysisContext> action) =>
        postponedActions.Add(action);

    /// <summary>
    /// Execution of postponed registration actions. This should be called once all rule parameters are set.
    /// </summary>
    public void ExecutePostponedActions(SonarCompilationStartAnalysisContext context)
    {
        foreach (var action in postponedActions)
        {
            action(context);
        }
    }

    public override bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, out TValue value) =>
        Context.TryGetValue(text, valueProvider, out value);
}
