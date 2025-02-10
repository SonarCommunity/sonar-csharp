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

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.AnalysisContext;
using SonarAnalyzer.Analyzers;
using SonarAnalyzer.TestFramework.Verification;

namespace SonarAnalyzer.Test.TestFramework.Tests.Verification;

[TestClass]
public class CodeFixProviderSnippetTest
{
    [TestMethod]
    public void VerifyCodeFix_Snippet()
    {
        var verifier = new VerifierBuilder
        {
            Analyzers = [() => new TestRule([SyntaxKind.ClassDeclaration])],
            CodeFix = () => new TestRuleCodeFix<ClassDeclarationSyntax>((root, node) =>
                root.ReplaceNode(node, node.WithIdentifier(SyntaxFactory.Identifier("Changed").WithTriviaFrom(node.Identifier)))),
        }
        .AddSnippet("""
        class C { }
        """)
        .WithCodeFixedSnippet("""
        class Changed { }
        """);
        Action a = () => verifier.VerifyCodeFix();
        a.Should().NotThrow();
    }

    [TestMethod]
    public void VerifyCodeFix_Snippet_Fail()
    {
        var verifier = new VerifierBuilder
        {
            Analyzers = [() => new TestRule([SyntaxKind.ClassDeclaration])],
            CodeFix = () => new TestRuleCodeFix<ClassDeclarationSyntax>((root, node) =>
                root.ReplaceNode(node, node.WithIdentifier(SyntaxFactory.Identifier("ActualChange").WithTriviaFrom(node.Identifier)))),
        }
        .AddSnippet("""
        class C { }
        """)
        .WithCodeFixedSnippet("""
        class ExpectedChange { }
        """);
        Action a = () => verifier.VerifyCodeFix();
        a.Should().Throw<AssertFailedException>().WithMessage(
            """Expected ActualCodeWithReplacedComments().ToUnixLineEndings() to be "class ExpectedChange { }" with a length of 24 """ +
            """because VerifyWhileDocumentChanges updates the document until all issues are fixed, even if the fix itself creates a new issue again. Language: CSharp7, """ +
            """but "class ActualChange { }" has a length of 22, differs near "Act" (index 6).""");
    }

    [TestMethod]
    public void VerifyCodeFix_Snippet_Fail_WithNoncompliantComment()
    {
        var verifier = new VerifierBuilder
        {
            Analyzers = [() => new TestRule([SyntaxKind.ClassDeclaration])],
            CodeFix = () => new TestRuleCodeFix<ClassDeclarationSyntax>((root, node) =>
                root.ReplaceNode(node, node.WithIdentifier(SyntaxFactory.Identifier("ActualChange").WithTriviaFrom(node.Identifier)))),
        }
        .AddSnippet("""
        class C { } // Noncompliant
        """)
        .WithCodeFixedSnippet("""
        class ExpectedChange { } // Fixed
        """);
        Action a = () => verifier.VerifyCodeFix();
        a.Should().Throw<AssertFailedException>().WithMessage(
            """Expected ActualCodeWithReplacedComments().ToUnixLineEndings() to be "class ExpectedChange { } // Fixed" with a length of 33 """ +
            """because VerifyWhileDocumentChanges updates the document until all issues are fixed, even if the fix itself creates a new issue again. Language: CSharp7, """ +
            """but "class ActualChange { } // Fixed" has a length of 31, differs near "Act" (index 6).""");
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void VerifyCodeFix_Snippet_MultipleIssues(bool withFixAllProvider)
    {
        var verifier = new VerifierBuilder
        {
            Analyzers = [() => new TestRule([SyntaxKind.ClassDeclaration], x => x is ClassDeclarationSyntax c && c.Identifier.ValueText.Length == 1)],
            CodeFix = () => new TestRuleCodeFix<ClassDeclarationSyntax>(withFixAllProvider, (root, node) =>
                root.ReplaceNode(node, node.WithIdentifier(SyntaxFactory.Identifier($"{node.Identifier.ValueText}Changed").WithTriviaFrom(node.Identifier)))),
        }
        .AddSnippet("""
        class A { } // Noncompliant
        class B { } // Noncompliant
        class C { } // Noncompliant
        """)
        .WithCodeFixedSnippet("""
        class AChanged { } // Fixed
        class BChanged { } // Fixed
        class CChanged { } // Fixed
        """);
        Action a = () => verifier.VerifyCodeFix();
        a.Should().NotThrow();
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestRule : SonarDiagnosticAnalyzer
    {
        public const string DiagnosticId = "TEST001";

        private readonly DiagnosticDescriptor rule = AnalysisScaffolding.CreateDescriptorMain(DiagnosticId);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

        public SyntaxKind[] RegisterSyntaxKinds { get; }
        public Func<SyntaxNode, bool> Filter { get; }

        public TestRule(SyntaxKind[] registerSyntaxKinds, Func<SyntaxNode, bool> filter = null)
        {
            RegisterSyntaxKinds = registerSyntaxKinds;
            Filter = filter ?? (_ => true);
        }

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(TestGeneratedCodeRecognizer.Instance, c =>
            {
                if (Filter(c.Node))
                {
                    c.ReportIssue(rule, c.Context.Node);
                }
            }, RegisterSyntaxKinds);
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class TestRuleCodeFix<TSyntax>(bool withFixAllProvider, Func<SyntaxNode, TSyntax, SyntaxNode> createChangedRoot) : SonarCodeFix where TSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [TestRule.DiagnosticId];

        public TestRuleCodeFix(Func<SyntaxNode, TSyntax, SyntaxNode> createChangedRoot) : this(true, createChangedRoot) { }

        public override FixAllProvider GetFixAllProvider() => withFixAllProvider
            ? base.GetFixAllProvider()
            : null;

        protected override Task RegisterCodeFixesAsync(SyntaxNode root, SonarCodeFixContext context)
        {
            context.RegisterCodeFix("TestTitle", async x =>
            {
                var root = await context.Document.GetSyntaxRootAsync(x);
                var node = (TSyntax)root.FindNode(context.Span);
                var newRoot = createChangedRoot(root, node);
                return context.Document.WithSyntaxRoot(newRoot);
            }, context.Diagnostics);
            return Task.CompletedTask;
        }
    }
}
