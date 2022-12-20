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

namespace SonarAnalyzer.Helpers
{
    public static class AnalysisContextExtensions
    {
        public static SyntaxTree GetSyntaxTree(this SyntaxNodeAnalysisContext context) =>
            context.Node.SyntaxTree;

        public static SyntaxTree GetSyntaxTree(this SyntaxTreeAnalysisContext context) =>
            context.Tree;

        public static SyntaxTree GetFirstSyntaxTree(this CompilationAnalysisContext context) =>
            context.Compilation.SyntaxTrees.FirstOrDefault();

#pragma warning disable RS1012 // Start action has no registered actions.
        public static SyntaxTree GetFirstSyntaxTree(this CompilationStartAnalysisContext context) =>
#pragma warning restore RS1012 // Start action has no registered actions.
            context.Compilation.SyntaxTrees.FirstOrDefault();

        public static SyntaxTree GetFirstSyntaxTree(this SymbolAnalysisContext context) =>
            context.Symbol.Locations.FirstOrDefault(l => l.SourceTree != null)?.SourceTree;

        public static SyntaxTree GetSyntaxTree(this CodeBlockAnalysisContext context) =>
            context.CodeBlock.SyntaxTree;

#pragma warning disable RS1012 // Start action has no registered actions.
        public static SyntaxTree GetSyntaxTree<TLanguageKindEnum>(this CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
#pragma warning restore RS1012 // Start action has no registered actions.
            where TLanguageKindEnum : struct =>
            context.CodeBlock.SyntaxTree;

        /// <param name="verifyScopeContext">Provide value for this argument only if the class has more than one SupportedDiagnostics.</param>
        public static void ReportIssue(this SyntaxNodeAnalysisContext context, Diagnostic diagnostic, SonarAnalysisContext verifyScopeContext = null) =>
            ReportIssue(new ReportingContext(context, diagnostic), verifyScopeContext?.IsTestProject(context.Compilation, context.Options), verifyScopeContext?.IsScannerRun(context.Options));

        // SyntaxTreeAnalysisContext doesn't have a Compilation => verifyScopeContext is never needed, because IsAnalysisScopeMatching returns always true.
        public static void ReportIssue(this SyntaxTreeAnalysisContext context, Diagnostic diagnostic) =>
            ReportIssue(new ReportingContext(context, diagnostic), null, null);

        public static void ReportIssue(this CompilationAnalysisContext context, Diagnostic diagnostic) =>
            ReportIssue(new ReportingContext(context, diagnostic), SonarAnalysisContext.IsTestProject(context), SonarAnalysisContext.IsScannerRun(context));

        /// <param name="verifyScopeContext">Provide value for this argument only if the class has more than one SupportedDiagnostics.</param>
        public static void ReportIssue(this SymbolAnalysisContext context, Diagnostic diagnostic, SonarAnalysisContext verifyScopeContext = null) =>
            ReportIssue(new ReportingContext(context, diagnostic), verifyScopeContext?.IsTestProject(context.Compilation, context.Options), verifyScopeContext?.IsScannerRun(context.Options));

        /// <param name="verifyScopeContext">Provide value for this argument only if the class has more than one SupportedDiagnostics.</param>
        public static void ReportIssue(this CodeBlockAnalysisContext context, Diagnostic diagnostic, SonarAnalysisContext verifyScopeContext = null) =>
            ReportIssue(new ReportingContext(context, diagnostic), verifyScopeContext?.IsTestProject(context.SemanticModel.Compilation, context.Options),
                verifyScopeContext?.IsScannerRun(context.Options));

        private static void ReportIssue(ReportingContext reportingContext, bool? isTestProject, bool? isScannerRun) // FIXME: REMOVE
        {
            if (isTestProject.HasValue
                && !SonarAnalysisContext.IsAnalysisScopeMatching(reportingContext.Compilation, isTestProject.Value, isScannerRun ?? true, new[] { reportingContext.Diagnostic.Descriptor }))
            {
                return;
            }

            if (reportingContext is { Compilation: { } compilation, Diagnostic.Location: { Kind: LocationKind.SourceFile, SourceTree: { } syntaxTree } }
                && !compilation.ContainsSyntaxTree(syntaxTree))
            {
                Debug.Fail("Primary location should be part of the compilation. An AD0001 is raised if this is not the case.");
                return;
            }

            // This is the current way SonarLint will handle how and what to report.
            if (SonarAnalysisContext.ReportDiagnostic != null)
            {
                Debug.Assert(SonarAnalysisContext.ShouldDiagnosticBeReported == null, "Not expecting SonarLint to set both the old and the new delegates.");
                SonarAnalysisContext.ReportDiagnostic(reportingContext);
                return;
            }

            // Standalone NuGet, Scanner run and SonarLint < 4.0 used with latest NuGet
            if (!VbcHelper.IsTriggeringVbcError(reportingContext.Diagnostic)
                && (SonarAnalysisContext.ShouldDiagnosticBeReported?.Invoke(reportingContext.SyntaxTree, reportingContext.Diagnostic) ?? true))
            {
                reportingContext.ReportDiagnostic(reportingContext.Diagnostic);
            }
        }
    }
}
