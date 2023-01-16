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

namespace SonarAnalyzer.AnalysisContext;

public sealed class SonarParametrizedAnalysisContext
{
    private readonly List<Action<SonarCompilationStartAnalysisContext>> postponedActions = new();

    public SonarAnalysisContext Context { get; }

    internal SonarParametrizedAnalysisContext(SonarAnalysisContext context) =>
        Context = context;

    public void RegisterNodeAction<TSyntaxKind>(GeneratedCodeRecognizer generatedCodeRecognizer, Action<SonarSyntaxNodeAnalysisContext> action, params TSyntaxKind[] syntaxKinds)
        where TSyntaxKind : struct =>
        Context.RegisterNodeAction(generatedCodeRecognizer, action, syntaxKinds);

    public void RegisterTreeAction(GeneratedCodeRecognizer generatedCodeRecognizer, Action<SonarSyntaxTreeAnalysisContext> action)
    {
        // This is tricky. SyntaxTree actions do not have compilation. So we register them in CompilationStart.
        // ParametrizedAnalyzer postpones CompilationStartActions to enforce that parameters are already set when the postponed action is executed.
        var wrappedAction = Context.WrapTreeAction(action, generatedCodeRecognizer);
        RegisterPostponedAction(startContext => wrappedAction(startContext.Context));
    }

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
}
