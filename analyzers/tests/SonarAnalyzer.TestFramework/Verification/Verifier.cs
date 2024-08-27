/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using SonarAnalyzer.Rules;
using SonarAnalyzer.TestFramework.Build;

namespace SonarAnalyzer.TestFramework.Verification;

internal class Verifier
{
    private const string TestCases = "TestCases";

    private static readonly Regex ImportsRegexVB = new(@"^\s*Imports\s+.+$", RegexOptions.Multiline | RegexOptions.RightToLeft, RegexConstants.DefaultTimeout);
    private readonly VerifierBuilder builder;
    private readonly DiagnosticAnalyzer[] analyzers;
    private readonly SonarCodeFix codeFix;
    private readonly AnalyzerLanguage language;
    private readonly string[] onlyDiagnosticIds;
    private readonly string[] razorFilePaths;

    public Verifier(VerifierBuilder builder)
    {
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        onlyDiagnosticIds = builder.OnlyDiagnostics.Select(x => x.Id).ToArray();
        analyzers = builder.Analyzers.Select(x => x()).ToArray();
        if (!analyzers.Any())
        {
            throw new ArgumentException($"{nameof(builder.Analyzers)} cannot be empty. Use {nameof(VerifierBuilder)}<TAnalyzer> instead or add at least one analyzer using {nameof(builder)}.{nameof(builder.AddAnalyzer)}().");
        }
        if (Array.Exists(analyzers, x => x == null))
        {
            throw new ArgumentException("Analyzer instance cannot be null.");
        }
        var allLanguages = analyzers.SelectMany(x => x.GetType().GetCustomAttributes<DiagnosticAnalyzerAttribute>()).SelectMany(x => x.Languages).Distinct().ToArray();
        if (allLanguages.Length > 1)
        {
            throw new ArgumentException($"All {nameof(builder.Analyzers)} must declare the same language in their DiagnosticAnalyzerAttribute.");
        }
        language = AnalyzerLanguage.FromName(allLanguages.Single());
        if (!builder.Paths.Any() && !builder.Snippets.Any())
        {
            throw new ArgumentException($"{nameof(builder.Paths)} cannot be empty. Add at least one file using {nameof(builder)}.{nameof(builder.AddPaths)}() or {nameof(builder.AddSnippet)}().");
        }
        foreach (var path in builder.Paths)
        {
            ValidateExtension(path);
        }
        if (builder.ProtobufPath is not null)
        {
            ValidateSingleAnalyzer(nameof(builder.ProtobufPath));
            if (analyzers.Single() is not UtilityAnalyzerBase)
            {
                throw new ArgumentException($"{analyzers.Single().GetType().Name} does not inherit from {nameof(UtilityAnalyzerBase)}.");
            }
        }
        if (builder.CodeFix is not null)
        {
            codeFix = builder.CodeFix();
            ValidateCodeFix();
        }
        razorFilePaths = builder.Paths
            .Select(TestCasePath)
            .Where(IsRazorOrCshtml)
            .Concat(builder.Snippets.Where(x => IsRazorOrCshtml(x.FileName)).Select(x =>
            {
                var tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases", x.FileName);
                // Source snippets need to be on disk for DiagnosticVerifier.Verify to work.
                // If this becomes unnecessary, adding source snippets should be reworked to align it with adding content snippets.
                File.WriteAllText(tempFilePath, x.Content);
                return tempFilePath;
            }))
            .ToArray();
    }

    public void Verify()    // This should never have any arguments
    {
        if (codeFix is not null)
        {
            throw new InvalidOperationException($"Cannot use {nameof(Verify)} with {nameof(builder.CodeFix)} set.");
        }
        var numberOfIssues = Compile(builder.ConcurrentAnalysis)
            .Sum(x => DiagnosticVerifier.Verify(
                x.Compilation,
                analyzers,
                builder.ErrorBehavior,
                builder.AdditionalFilePath,
                onlyDiagnosticIds,
                razorFilePaths.Concat(x.AdditionalSourceFiles ?? []).ToArray()));
        numberOfIssues.Should().BeGreaterThan(0, $"otherwise you should use '{nameof(VerifyNoIssues)}' instead");
    }

    public void VerifyNoIssues()    // This should never have any arguments
    {
        foreach (var compilation in Compile(builder.ConcurrentAnalysis))
        {
            foreach (var analyzer in analyzers)
            {
                DiagnosticVerifier.VerifyNoIssues(compilation.Compilation, analyzer, builder.ErrorBehavior, builder.AdditionalFilePath, onlyDiagnosticIds);
            }
        }
    }

    public void VerifyNoIssuesIgnoreErrors()    // This should never have any arguments
    {
        foreach (var compilation in Compile(builder.ConcurrentAnalysis))
        {
            foreach (var analyzer in analyzers)
            {
                DiagnosticVerifier.VerifyNoIssuesIgnoreErrors(compilation.Compilation, analyzer, builder.ErrorBehavior, builder.AdditionalFilePath, onlyDiagnosticIds);
            }
        }
    }

    public void VerifyCodeFix()     // This should never have any arguments
    {
        _ = codeFix ?? throw new InvalidOperationException($"{nameof(builder.CodeFix)} was not set.");
        var document = CreateProject(false).FindDocument(Path.Combine(builder.BasePath ?? string.Empty, Path.GetFileName(builder.Paths.Single())));
        var codeFixVerifier = new CodeFixVerifier(analyzers.Single(), codeFix, document, builder.CodeFixTitle);
        var fixAllProvider = codeFix.GetFixAllProvider();
        foreach (var parseOptions in builder.ParseOptions.OrDefault(language.LanguageName))
        {
            codeFixVerifier.VerifyWhileDocumentChanges(parseOptions, TestCasePath(builder.CodeFixedPath));
            if (fixAllProvider is not null)
            {
                codeFixVerifier.VerifyFixAllProvider(fixAllProvider, parseOptions, TestCasePath(builder.CodeFixedPathBatch ?? builder.CodeFixedPath));
            }
        }
    }

    public void VerifyUtilityAnalyzerProducesEmptyProtobuf()     // This should never have any arguments
    {
        foreach (var compilation in Compile(false))
        {
            DiagnosticVerifier.Verify(compilation.Compilation, analyzers.Single(), builder.AdditionalFilePath);
            new FileInfo(builder.ProtobufPath).Length.Should().Be(0, "protobuf file should be empty");
        }
    }

    public void VerifyUtilityAnalyzer<TMessage>(Action<IReadOnlyList<TMessage>> verifyProtobuf)
        where TMessage : IMessage<TMessage>, new()
    {
        foreach (var compilation in Compile(false))
        {
            DiagnosticVerifier.Verify(compilation.Compilation, analyzers.Single(), builder.AdditionalFilePath);
            verifyProtobuf(ReadProtobuf().ToList());
        }

        IEnumerable<TMessage> ReadProtobuf()
        {
            using var input = File.OpenRead(builder.ProtobufPath);
            var parser = new MessageParser<TMessage>(() => new TMessage());
            while (input.Position < input.Length)
            {
                yield return parser.ParseDelimitedFrom(input);
            }
        }
    }

    public IEnumerable<CompilationData> Compile(bool concurrentAnalysis)
    {
        using var scope = new EnvironmentVariableScope { EnableConcurrentAnalysis = concurrentAnalysis };
        return CreateProject(concurrentAnalysis).Solution.Compile(builder.ParseOptions.ToArray()).Select(x => new CompilationData(x, null));
    }

    private ProjectBuilder CreateProject(bool concurrentAnalysis)
    {
        var paths = builder.Paths.Select(TestCasePath).ToList();
        var sourceFilePaths = paths.Except(razorFilePaths).ToArray();
        var sourceSnippets = builder.Snippets.Where(x => !IsRazorOrCshtml(x.FileName)).ToArray();
        var editorConfigGenerator = new EditorConfigGenerator(Directory.GetCurrentDirectory());
        var hasRazorFiles = razorFilePaths.Length > 0;
        concurrentAnalysis = !hasRazorFiles && concurrentAnalysis; // Concurrent analysis is not supported for Razor or cshtml files due to namespace issues
        var concurrentSourceFiles = concurrentAnalysis && builder.AutogenerateConcurrentFiles ? CreateConcurrencyTest(sourceFilePaths) : [];
        var projectBuilder = SolutionBuilder.Create()
            .AddProject(language, builder.OutputKind)
            .AddSnippets(sourceSnippets)
            .AddDocuments(sourceFilePaths)
            .AddDocuments(concurrentSourceFiles)
            .AddReferences(builder.References);
        if (hasRazorFiles)
        {
            projectBuilder = projectBuilder
                .AddAdditionalDocuments(razorFilePaths)
                .AddReferences(NuGetMetadataReference.MicrosoftAspNetCoreAppRef("7.0.17"))
                .AddReferences(NuGetMetadataReference.SystemTextEncodingsWeb("7.0.0"))
                .AddAnalyzerReferences(SourceGeneratorProvider.SourceGenerators)
                .AddAnalyzerConfigDocument(
                    Path.Combine(Directory.GetCurrentDirectory(), ".editorconfig"),
                    editorConfigGenerator.Generate(razorFilePaths));
        }
        return projectBuilder;
    }

    private IEnumerable<string> CreateConcurrencyTest(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var newPath = Path.ChangeExtension(path, ".Concurrent" + Path.GetExtension(path));
            var content = File.ReadAllText(path, Encoding.UTF8);
            File.WriteAllText(newPath, InsertConcurrentNamespace(content));
            yield return newPath;
        }
    }

    private string InsertConcurrentNamespace(string content)
    {
        return language.LanguageName switch
        {
            LanguageNames.CSharp => EncloseInNamespace(content),
            LanguageNames.VisualBasic => content.Insert(ImportsIndexVB(), "Namespace AppendedNamespaceForConcurrencyTest : ") + Environment.NewLine + " : End Namespace",
            _ => throw new UnexpectedLanguageException(language)
        };

        int ImportsIndexVB() =>
            ImportsRegexVB.Match(content) is { Success: true } match ? match.Index + match.Length + 1 : 0;
    }

    private static string EncloseInNamespace(string content)
    {
        var tree = CSharpSyntaxTree.ParseText(content);
        if (tree.TryGetRoot(out var root) && root is CompilationUnitSyntax { Members: { } members } compilationUnit)
        {
            if (members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault() is { } fileScoped)
            {
                root = root.ReplaceNode(fileScoped, fileScoped.WithName(SyntaxFactory.ParseName($"ConcurrencyTest.{fileScoped.Name}")));
            }
            else
            {
                var newNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(" AppendedNamespaceForConcurrencyTest"))
                    .WithMembers(compilationUnit.Members)
                    .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Whitespace("\n")));
                if (newNamespace.Members.Any() && newNamespace.Members[0] is NamespaceDeclarationSyntax)
                {
                    // Move the leading trivia of the first member to newNamespace
                    newNamespace = newNamespace.WithLeadingTrivia(newNamespace.Members[0].GetLeadingTrivia());
                    newNamespace = newNamespace.WithMembers(newNamespace.Members.Replace(newNamespace.Members[0], newNamespace.Members[0].WithoutLeadingTrivia()));
                }
                root = compilationUnit.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[] { newNamespace }));
            }

            return root.ToFullString();
        }
        else
        {
            return $"namespace AppendedNamespaceForConcurrencyTest {{ {content} {Environment.NewLine}}}";
        }
    }

    private string TestCaseDirectory() =>
        Path.GetFullPath(builder.BasePath == null ? TestCases : Path.Combine(TestCases, builder.BasePath));

    private string TestCasePath(string fileName) =>
        Path.Combine(TestCaseDirectory(), fileName);

    private void ValidateSingleAnalyzer(string propertyName)
    {
        if (builder.Analyzers.Length != 1)
        {
            throw new ArgumentException($"When {propertyName} is set, {nameof(builder.Analyzers)} must contain only 1 analyzer, but {analyzers.Length} were found.");
        }
    }

    private void ValidateExtension(string path)
    {
        if (!Path.GetExtension(path).Equals(language.FileExtension, StringComparison.OrdinalIgnoreCase) && !IsRazorOrCshtml(path))
        {
            throw new ArgumentException($"Path '{path}' doesn't match {language.LanguageName} file extension '{language.FileExtension}'.");
        }
    }

    private void ValidateCodeFix()
    {
        _ = builder.CodeFixedPath ?? throw new ArgumentException($"{nameof(builder.CodeFixedPath)} was not set.");
        ValidateSingleAnalyzer(nameof(builder.CodeFix));
        if (builder.Paths.Length != 1)
        {
            throw new ArgumentException($"{nameof(builder.Paths)} must contain only 1 file, but {builder.Paths.Length} were found.");
        }
        if (builder.Snippets.Any())
        {
            throw new ArgumentException($"{nameof(builder.Snippets)} must be empty when {nameof(builder.CodeFix)} is set.");
        }
        ValidateExtension(builder.CodeFixedPath);
        if (builder.CodeFixedPathBatch is not null)
        {
            ValidateExtension(builder.CodeFixedPathBatch);
        }
        if (codeFix.GetType().GetCustomAttribute<ExportCodeFixProviderAttribute>() is { } codeFixAttribute)
        {
            if (codeFixAttribute.Languages.Single() != language.LanguageName)
            {
                throw new ArgumentException($"{analyzers.Single().GetType().Name} language {language.LanguageName} does not match {codeFix.GetType().Name} language.");
            }
        }
        else
        {
            throw new ArgumentException($"{codeFix.GetType().Name} does not have {nameof(ExportCodeFixProviderAttribute)}.");
        }
        if (!analyzers.Single().SupportedDiagnostics.Select(x => x.Id).Intersect(codeFix.FixableDiagnosticIds).Any())
        {
            throw new ArgumentException($"{analyzers.Single().GetType().Name} does not support diagnostics fixable by the {codeFix.GetType().Name}.");
        }
    }

    private static bool IsRazorOrCshtml(string path) =>
        Path.GetExtension(path) is { } extension
        && (extension.Equals(".razor", StringComparison.OrdinalIgnoreCase) || extension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase));

    public sealed record CompilationData(Compilation Compilation, string[] AdditionalSourceFiles);
}
