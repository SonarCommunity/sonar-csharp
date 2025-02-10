/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

using Google.Protobuf;
using SonarAnalyzer.TestFramework.Build;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace SonarAnalyzer.TestFramework.Verification;

// ToDo: Remove this enum https://github.com/SonarSource/sonar-dotnet/issues/8588
[Obsolete("This will be removed. Use FailTest if you really have to provide this argument somewhere.")]
public enum CompilationErrorBehavior
{
    FailTest,
    Ignore,
    Default = FailTest
}

/// <summary>
/// Immutable builder that holds all parameters for rule verification.
/// </summary>
public record VerifierBuilder
{
    // All properties are (and should be) immutable.
    public ImmutableArray<Func<DiagnosticAnalyzer>> Analyzers { get; init; } = ImmutableArray<Func<DiagnosticAnalyzer>>.Empty;
    public bool AutogenerateConcurrentFiles { get; init; } = true;
    public string BasePath { get; init; }
    public Func<SonarCodeFix> CodeFix { get; init; }
    public string CodeFixedPath { get; init; }
    public string CodeFixed { get; init; }
    public string CodeFixedPathBatch { get; init; }
    public string CodeFixTitle { get; init; }
    public bool ConcurrentAnalysis { get; init; } = true;
    public CompilationErrorBehavior ErrorBehavior { get; init; } = CompilationErrorBehavior.Default;
    public ImmutableArray<DiagnosticDescriptor> OnlyDiagnostics { get; init; } = ImmutableArray<DiagnosticDescriptor>.Empty;
    public OutputKind OutputKind { get; init; } = OutputKind.DynamicallyLinkedLibrary;
    public ImmutableArray<string> Paths { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<ParseOptions> ParseOptions { get; init; } = ImmutableArray<ParseOptions>.Empty;
    public string ProtobufPath { get; init; }
    public ImmutableArray<MetadataReference> References { get; init; } = ImmutableArray<MetadataReference>.Empty;
    public ImmutableArray<Snippet> Snippets { get; init; } = ImmutableArray<Snippet>.Empty;
    public string AdditionalFilePath { get; init; }

    /// <summary>
    /// This method solves complicated scenarios. Use 'new VerifierBuilder&lt;TAnalyzer&gt;()' for single analyzer cases with no rule parameters.
    /// </summary>
    public VerifierBuilder AddAnalyzer(Func<DiagnosticAnalyzer> createConfiguredAnalyzer) =>
        this with { Analyzers = Analyzers.Append(createConfiguredAnalyzer).ToImmutableArray() };

    public VerifierBuilder AddPaths(params string[] paths) =>
        this with { Paths = Paths.Concat(paths).ToImmutableArray(), };

    public VerifierBuilder AddReferences(IEnumerable<MetadataReference> references) =>
        this with { References = References.Concat(references).ToImmutableArray() };

    public VerifierBuilder AddSnippet(string snippet, string fileName = null) =>
        this with { Snippets = Snippets.Add(new(snippet, fileName)) };

    /// <summary>
    /// Add a test reference to change the project type to Test project.
    /// </summary>
    public VerifierBuilder AddTestReference() =>
        AddReferences(NuGetMetadataReference.MSTestTestFrameworkV1);

    public VerifierBuilder WithAutogenerateConcurrentFiles(bool autogenerateConcurrentFiles) =>
        this with { AutogenerateConcurrentFiles = autogenerateConcurrentFiles };

    /// <summary>
    /// Path infix relative to TestCases directory.
    /// </summary>
    /// <remarks>If we ever need to change the root outside the .\TestCases directory, add WithRootPath(..) while WithBasePath(..) would still append another part after the root path.</remarks>
    public VerifierBuilder WithBasePath(string basePath) =>
        this with { BasePath = basePath };

    public VerifierBuilder WithCodeFix<TCodeFix>() where TCodeFix : SonarCodeFix, new() =>
        this with { CodeFix = () => new TCodeFix() };

    /// <param name="codeFixedPathBatch">Fixed file for cases when FixAllProvider produces different results than applying all code fixes on the same original document.</param>
    public VerifierBuilder WithCodeFixedPaths(string codeFixedPath, string codeFixedPathBatch = null) =>
        this with { CodeFixedPath = codeFixedPath, CodeFixedPathBatch = codeFixedPathBatch };

    public VerifierBuilder WithCodeFixedSnippet(string codeFixed) =>
        this with { CodeFixed = codeFixed };

    public VerifierBuilder WithCodeFixTitle(string codeFixTitle) =>
        this with { CodeFixTitle = codeFixTitle };

    public VerifierBuilder WithConcurrentAnalysis(bool concurrentAnalysis) =>
        this with { ConcurrentAnalysis = concurrentAnalysis };

    [Obsolete("Do not use CompilationErrorBehavior. Assert errors with // Error [CSxxxx, CSyyyy] instead")]
    public VerifierBuilder WithErrorBehavior(CompilationErrorBehavior errorBehavior) =>
        this with { ErrorBehavior = errorBehavior };

    public VerifierBuilder WithLanguageVersion(CS.LanguageVersion languageVersion) =>
        WithOptions([new CS.CSharpParseOptions(languageVersion)]);

    public VerifierBuilder WithLanguageVersion(VB.LanguageVersion languageVersion) =>
        WithOptions([new VB.VisualBasicParseOptions(languageVersion)]);

    public VerifierBuilder WithOnlyDiagnostics(params DiagnosticDescriptor[] onlyDiagnostics) =>
        this with { OnlyDiagnostics = onlyDiagnostics.ToImmutableArray() };

    public VerifierBuilder WithOptions(ImmutableArray<ParseOptions> parseOptions) =>
        this with { ParseOptions = parseOptions };

    public VerifierBuilder WithOutputKind(OutputKind outputKind) =>
        this with { OutputKind = outputKind };

    public VerifierBuilder WithProtobufPath(string protobufPath) =>
        this with { ProtobufPath = protobufPath };

    public VerifierBuilder WithAdditionalFilePath(string additionalFilePath) =>
        this with { AdditionalFilePath = additionalFilePath };

    public VerifierBuilder WithTopLevelStatements()
    {
        if (ParseOptions.OfType<VB.VisualBasicParseOptions>().Any())
        {
            throw new InvalidOperationException($"{nameof(WithTopLevelStatements)} is not supported with {nameof(VB.VisualBasicParseOptions)}.");
        }
        if (ParseOptions.Cast<CS.CSharpParseOptions>().Any(x => x.LanguageVersion < CS.LanguageVersion.CSharp9))
        {
            throw new InvalidOperationException($"{nameof(WithTopLevelStatements)} is supported from {nameof(CS.LanguageVersion.CSharp9)}.");
        }
        return (ParseOptions.IsEmpty ? WithOptions(LanguageOptions.FromCSharp9) : this)
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithConcurrentAnalysis(false);
    }

    public IEnumerable<Compilation> Compile() =>
        Build().Compile(false).Select(x => x.Compilation);

    /// <summary>
    /// Verifies that the diagnostics match the expected diagnostics and at least one diagnostic is found.
    /// </summary>
    public void Verify() =>
        Build().Verify();

    /// <summary>
    /// Verifies that no diagnostics are found.
    /// </summary>
    public void VerifyNoIssues() =>
        Build().VerifyNoIssues();

    /// <summary>
    /// Check that the compilation does not produce any AD0001 issues. Other diagnostics are ignored.
    /// </summary>
    public void VerifyNoAD0001() =>
        Build().VerifyNoAD0001();

    /// <summary>
    /// Verifies that no diagnostics, except errors, are found.
    /// </summary>
    public void VerifyNoIssuesIgnoreErrors() =>
        Build().VerifyNoIssuesIgnoreErrors();

    public void VerifyCodeFix() =>
        Build().VerifyCodeFix();

    public void VerifyUtilityAnalyzerProducesEmptyProtobuf() =>
        Build().VerifyUtilityAnalyzerProducesEmptyProtobuf();

    public void VerifyUtilityAnalyzer<TMessage>(Action<IReadOnlyList<TMessage>> verifyProtobuf)
        where TMessage : IMessage<TMessage>, new() =>
        Build().VerifyUtilityAnalyzer(verifyProtobuf);

    internal Verifier Build() =>
        new(this);
}

public record VerifierBuilder<TAnalyzer> : VerifierBuilder
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public VerifierBuilder() =>
        Analyzers = new Func<DiagnosticAnalyzer>[] { () => new TAnalyzer() }.ToImmutableArray();
}
