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

using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;
using static SonarAnalyzer.Helpers.DiagnosticDescriptorFactory;

namespace SonarAnalyzer;

/// <summary>
/// SonarC# and SonarVB specific context for initializing an analyzer. This type acts as a wrapper around Roslyn
/// <see cref="AnalysisContext"/> to allow for specialized control over the analyzer.
/// Here is the list of fine-grained changes we are doing:
/// - Avoid duplicated issues when the analyzer NuGet (SonarAnalyzer) and the VSIX (SonarLint) are installed simultaneously.
/// - Allow a specific kind of rule-set for SonarLint (enable/disable a rule).
/// - Prevent reporting an issue when it was suppressed on SonarQube.
/// </summary>
public partial class SonarAnalysisContext
{
    public delegate bool TryGetValueDelegate<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, out TValue value);     // FIXME: Remove, new system doesn't need this anymore

    private static readonly SourceTextValueProvider<bool> ShouldAnalyzeGeneratedCS = CreateAnalyzeGeneratedProvider(LanguageNames.CSharp);
    private static readonly SourceTextValueProvider<bool> ShouldAnalyzeGeneratedVB = CreateAnalyzeGeneratedProvider(LanguageNames.VisualBasic);
    private static readonly SourceTextValueProvider<ProjectConfigReader> ProjectConfigProvider = new(x => new ProjectConfigReader(x));  // FIXME: Remove, it was migrated to the SonarAnalysisContextBase

    public bool ShouldAnalyzeGenerated(Compilation c, AnalyzerOptions options) =>
        ShouldAnalyzeGenerated(analysisContext.TryGetValue, c, options);

    public bool IsScannerRun(AnalyzerOptions options) =>
        ProjectConfiguration(options).IsScannerRun;

    public static bool IsScannerRun(CompilationAnalysisContext context) =>
        ProjectConfiguration(context.TryGetValue, context.Options).IsScannerRun;

    public bool IsTestProject(Compilation c, AnalyzerOptions options) =>
        IsTestProject(analysisContext.TryGetValue, c, options);

    public static bool IsTestProject(CompilationAnalysisContext analysisContext) =>
        IsTestProject(analysisContext.TryGetValue, analysisContext.Compilation, analysisContext.Options);

    internal static bool IsRegisteredActionEnabled(IEnumerable<DiagnosticDescriptor> diagnostics, SyntaxTree tree) =>
        ShouldExecuteRegisteredAction == null || tree == null || ShouldExecuteRegisteredAction(diagnostics, tree);

    /// <summary>
    /// Reads configuration from SonarProjectConfig.xml file and caches the result for scope of this analysis.
    /// </summary>
    internal ProjectConfigReader ProjectConfiguration(AnalyzerOptions options) =>   // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
        ProjectConfiguration(analysisContext.TryGetValue, options);

    internal static ProjectConfigReader ProjectConfiguration(TryGetValueDelegate<ProjectConfigReader> tryGetValue, AnalyzerOptions options) // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
    {
        if (options.SonarProjectConfig() is { } sonarProjectConfigXml)
        {
            return sonarProjectConfigXml.GetText() is { } sourceText
                // TryGetValue catches all exceptions from SourceTextValueProvider and returns false when thrown
                && tryGetValue(sourceText, ProjectConfigProvider, out var cachedProjectConfigReader)
                ? cachedProjectConfigReader
                : throw new InvalidOperationException($"File {Path.GetFileName(sonarProjectConfigXml.Path)} has been added as an AdditionalFile but could not be read and parsed.");
        }
        else
        {
            return ProjectConfigReader.Empty;
        }
    }

    internal static bool IsAnalysisScopeMatching(Compilation compilation, bool isTestProject, bool isScannerRun, IEnumerable<DiagnosticDescriptor> diagnostics)
    {
        // We don't know the project type without the compilation so let's run the rule
        return compilation == null || diagnostics.Any(IsMatching);

        bool IsMatching(DiagnosticDescriptor descriptor)
        {
            // MMF-2297: Test Code as 1st Class Citizen is not ready on server side yet.
            // ScannerRun: Only utility rules and rules with TEST-ONLY scope are executed for test projects for now.
            // SonarLint & Standalone Nuget: Respect the scope as before.
            return isTestProject
                ? ContainsTag(TestSourceScopeTag) && !(isScannerRun && ContainsTag(MainSourceScopeTag) && !ContainsTag(UtilityTag))
                : ContainsTag(MainSourceScopeTag);

            bool ContainsTag(string tag) =>
                descriptor.CustomTags.Contains(tag);
        }
    }

    private static bool IsTestProject(TryGetValueDelegate<ProjectConfigReader> tryGetValue, Compilation compilation, AnalyzerOptions options)   // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
    {
        var projectType = ProjectConfiguration(tryGetValue, options).ProjectType;
        return projectType == ProjectType.Unknown
            ? compilation.IsTest()              // SonarLint, NuGet or Scanner <= 5.0
            : projectType == ProjectType.Test;  // Scanner >= 5.1 does authoritative decision that we follow
    }

    private static SourceTextValueProvider<bool> CreateAnalyzeGeneratedProvider(string language) => //FIXME: Remove, it was migrated to the SonarAnalysisContextBase
        new(x => PropertiesHelper.ReadAnalyzeGeneratedCodeProperty(ParseXmlSettings(x), language));

    private static IEnumerable<XElement> ParseXmlSettings(SourceText sourceText)    // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
    {
        try
        {
            return XDocument.Parse(sourceText.ToString()).Descendants("Setting");
        }
        catch
        {
            // cannot log the exception, so ignore it
            return Enumerable.Empty<XElement>();
        }
    }

    private static bool ShouldAnalyzeGenerated(TryGetValueDelegate<bool> tryGetValue, Compilation c, AnalyzerOptions options) =>    // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
        options.SonarLintXml() is { } sonarLintXml
        && tryGetValue(sonarLintXml.GetText(), ShouldAnalyzeGeneratedProvider(c.Language), out var shouldAnalyzeGenerated)
        && shouldAnalyzeGenerated;

    private static SourceTextValueProvider<bool> ShouldAnalyzeGeneratedProvider(string language) => // FIXME: Remove, it was migrated to the SonarAnalysisContextBase
        language == LanguageNames.CSharp ? ShouldAnalyzeGeneratedCS : ShouldAnalyzeGeneratedVB;
}
