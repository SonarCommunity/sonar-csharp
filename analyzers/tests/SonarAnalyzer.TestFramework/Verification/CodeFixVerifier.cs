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

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using SonarAnalyzer.TestFramework.Verification.IssueValidation;

namespace SonarAnalyzer.TestFramework.Verification;

internal class CodeFixVerifier
{
    private const string FixedMessage = "Fixed";

    private readonly DiagnosticAnalyzer analyzer;
    private readonly CodeFixProvider codeFix;
    private readonly Document originalDocument;
    private readonly string codeFixTitle;

    public CodeFixVerifier(DiagnosticAnalyzer analyzer, CodeFixProvider codeFix, Document originalDocument, string codeFixTitle)
    {
        this.analyzer = analyzer;
        this.codeFix = codeFix;
        this.originalDocument = originalDocument;
        this.codeFixTitle = codeFixTitle;
    }

    public void VerifyWhileDocumentChanges(ParseOptions parseOptions, FileInfo pathToExpected) =>
        VerifyWhileDocumentChanges(parseOptions, File.ReadAllText(pathToExpected.FullName));

    public void VerifyWhileDocumentChanges(ParseOptions parseOptions, string expectedCode)
    {
        var state = new State(analyzer, originalDocument, parseOptions);
        var codeFixExecuted = false;
        string codeBeforeFix;
        state.Diagnostics.Should().NotBeEmpty();
        do
        {
            codeBeforeFix = state.ActualCode;
            if (state.Diagnostics
                    .Where(x => codeFix.FixableDiagnosticIds.Contains(x.Id))    // Analyzer can also raise other Diagnostics that we can't fix
                    .SelectMany(x => ActionToApply(codeFix, state.Document, x))
                    .FirstOrDefault() is { } actionToApply)
            {
                state = new State(analyzer, ApplyCodeFix(state.Document, actionToApply), parseOptions);
                codeFixExecuted = true;
            }
        }
        while (codeBeforeFix != state.ActualCode);

        codeFixExecuted.Should().BeTrue();
        state.AssertExpected(expectedCode, nameof(VerifyWhileDocumentChanges) + " updates the document until all issues are fixed, even if the fix itself creates a new issue again");
    }

    public void VerifyFixAllProvider(FixAllProvider fixAllProvider, ParseOptions parseOptions, FileInfo pathToExpected) =>
        VerifyFixAllProvider(fixAllProvider, parseOptions, File.ReadAllText(pathToExpected.FullName));

    public void VerifyFixAllProvider(FixAllProvider fixAllProvider, ParseOptions parseOptions, string expectedCode)
    {
        var state = new State(analyzer, originalDocument, parseOptions);
        state.Diagnostics.Should().NotBeEmpty();

        var fixAllDiagnosticProvider = new FixAllDiagnosticProvider(state.Diagnostics);
        var codeActionEquivalenceKey = codeFixTitle ?? CodeFixTitle(codeFix, state);    // We need to find the title of the single action to use
        var fixAllContext = new FixAllContext(state.Document, codeFix, FixAllScope.Document, codeActionEquivalenceKey, codeFix.FixableDiagnosticIds, fixAllDiagnosticProvider, default);
        var codeActionToExecute = fixAllProvider.GetFixAsync(fixAllContext).Result;
        codeActionToExecute.Should().NotBeNull();

        new State(analyzer, ApplyCodeFix(state.Document, codeActionToExecute), parseOptions)
            .AssertExpected(expectedCode, $"{nameof(VerifyFixAllProvider)} runs {fixAllProvider.GetType().Name} once");
    }

    private string CodeFixTitle(CodeFixProvider codeFix, State state) =>
        state.Diagnostics.SelectMany(x => ActionToApply(codeFix, state.Document, x)).First().Title;

    private static Document ApplyCodeFix(Document document, CodeAction codeAction)
    {
        var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return solution.GetDocument(document.Id);
    }

    private IEnumerable<CodeAction> ActionToApply(CodeFixProvider codeFix, Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), default);
        codeFix.RegisterCodeFixesAsync(context).Wait();
        return actions.Where(x => codeFixTitle is null || x.Title == codeFixTitle);
    }

    private sealed class State
    {
        public readonly Document Document;
        public readonly ImmutableArray<Diagnostic> Diagnostics;
        public readonly string ActualCode;
        private readonly Compilation compilation;

        public State(DiagnosticAnalyzer analyzer, Document document, ParseOptions parseOptions)
        {
            var project = document.Project.WithParseOptions(parseOptions);
            document = project.GetDocument(document.Id);    // There's a new instance with the same ID
            compilation = project.GetCompilationAsync().Result;
            Document = document;
            Diagnostics = DiagnosticVerifier.AnalyzerDiagnostics(compilation, analyzer, CompilationErrorBehavior.Ignore).Where(x => x.Severity != DiagnosticSeverity.Error).ToImmutableArray();
            ActualCode = document.GetSyntaxRootAsync().Result.GetText().ToString();
        }

        public void AssertExpected(string expectedCode, string becauseMessage) =>
            ActualCodeWithReplacedComments().ToUnixLineEndings().Should().Be(expectedCode, $"{becauseMessage}. Language: {compilation.LanguageVersionString()}");

        private string ActualCodeWithReplacedComments() =>
            ActualCode.ToUnixLineEndings()
                .Split(new[] { TestConstants.UnixLineEnding }, StringSplitOptions.None)
                .Where(x => !IssueLocationCollector.RxPreciseLocation.IsMatch(x))
                .Select(ReplaceNonCompliantComment)
                .JoinStr(TestConstants.UnixLineEnding);

        private static string ReplaceNonCompliantComment(string line)
        {
            var match = IssueLocationCollector.RxIssue.Match(line);
            if (!match.Success || match.Groups["IssueType"].Value == "Error")
            {
                return line;
            }

            if (match.Groups["IssueType"].Value == "Noncompliant")
            {
                var startIndex = line.IndexOf(match.Groups["IssueType"].Value);
                return string.Concat(line.Remove(startIndex), FixedMessage);
            }

            return line.Replace(match.Value, string.Empty).TrimEnd();
        }
    }
}
