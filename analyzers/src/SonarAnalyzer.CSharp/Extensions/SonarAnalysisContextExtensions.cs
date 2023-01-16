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

namespace SonarAnalyzer.Extensions
{
    internal static class SonarAnalysisContextExtensions
    {
        public static void RegisterNodeAction<TSyntaxKind>(this SonarAnalysisContext context,
                                                                               Action<SonarSyntaxNodeAnalysisContext> action,
                                                                               params TSyntaxKind[] syntaxKinds) where TSyntaxKind : struct =>
            context.RegisterNodeAction(CSharpGeneratedCodeRecognizer.Instance, action, syntaxKinds);

        public static void RegisterNodeAction<TSyntaxKind>(this SonarParametrizedAnalysisContext context,
                                                                               Action<SonarSyntaxNodeAnalysisContext> action,
                                                                               params TSyntaxKind[] syntaxKinds) where TSyntaxKind : struct =>
            context.RegisterNodeAction(CSharpGeneratedCodeRecognizer.Instance, action, syntaxKinds);

        public static void RegisterNodeAction<TSyntaxKind>(this SonarCompilationStartAnalysisContext context,
                                                                               Action<SonarSyntaxNodeAnalysisContext> action,
                                                                               params TSyntaxKind[] syntaxKinds) where TSyntaxKind : struct =>
            context.RegisterNodeAction(CSharpGeneratedCodeRecognizer.Instance, action, syntaxKinds);

        public static void RegisterTreeAction(this SonarAnalysisContext context, Action<SonarSyntaxTreeAnalysisContext> action) =>
            context.RegisterTreeAction(CSharpGeneratedCodeRecognizer.Instance, action);

        public static void RegisterTreeAction(this SonarParametrizedAnalysisContext context, Action<SonarSyntaxTreeAnalysisContext> action) =>
            context.RegisterTreeAction(CSharpGeneratedCodeRecognizer.Instance, action);

        public static void RegisterCodeBlockStartAction<TSyntaxKind>(this SonarAnalysisContext context, Action<SonarCodeBlockStartAnalysisContext<TSyntaxKind>> action)
            where TSyntaxKind : struct =>
            context.RegisterCodeBlockStartAction(CSharpGeneratedCodeRecognizer.Instance, action);

        public static void ReportIssue(this SonarCompilationAnalysisContext context, Diagnostic diagnostic) =>
            context.ReportIssue(CSharpGeneratedCodeRecognizer.Instance, diagnostic);

        public static void ReportIssue(this SonarSymbolAnalysisContext context, Diagnostic diagnostic) =>
            context.ReportIssue(CSharpGeneratedCodeRecognizer.Instance, diagnostic);
    }
}
