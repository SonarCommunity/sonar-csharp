﻿/*
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
using Microsoft.CodeAnalysis.CSharp;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.SymbolicExecution.Sonar.Analyzers;

namespace SonarAnalyzer.Test.TestFramework.Tests.Verification
{
    [TestClass]
    public class VerifierTest
    {
        private static readonly VerifierBuilder DummyCS = new VerifierBuilder<DummyAnalyzerCS>();
        private static readonly VerifierBuilder DummyVB = new VerifierBuilder<DummyAnalyzerVB>();
        private static readonly VerifierBuilder DummyCodeFixCS = new VerifierBuilder<DummyAnalyzerCS>()
            .AddPaths("Path.cs")
            .WithCodeFix<DummyCodeFixCS>()
            .WithCodeFixedPaths("Expected.cs");

        private static readonly VerifierBuilder DummyWithLocation = new VerifierBuilder<DummyAnalyzerWithLocation>();

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Constructor_Null_Throws() =>
            ((Func<Verifier>)(() => new(null))).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("builder");

        [TestMethod]
        public void Constructor_NoAnalyzers_Throws() =>
            new VerifierBuilder()
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>()
                .WithMessage("Analyzers cannot be empty. Use VerifierBuilder<TAnalyzer> instead or add at least one analyzer using builder.AddAnalyzer().");

        [TestMethod]
        public void Constructor_NullAnalyzers_Throws() =>
            new VerifierBuilder().AddAnalyzer(() => null)
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Analyzer instance cannot be null.");

        [TestMethod]
        public void Constructor_NoPaths_Throws() =>
            DummyCS.Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Paths cannot be empty. Add at least one file using builder.AddPaths() or AddSnippet().");

        [TestMethod]
        public void Constructor_MixedLanguageAnalyzers_Throws() =>
            DummyCS.AddAnalyzer(() => new SonarAnalyzer.Rules.VisualBasic.OptionStrictOn())
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("All Analyzers must declare the same language in their DiagnosticAnalyzerAttribute.");

        [TestMethod]
        public void Constructor_MixedLanguagePaths_Throws() =>
            DummyCS.AddPaths("File.txt")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Path 'File.txt' doesn't match C# file extension '.cs'.");

        [TestMethod]
        public void Constructor_CodeFix_MissingCodeFixedPath_Throws() =>
            DummyCodeFixCS.WithCodeFixedPaths(null)
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("CodeFixedPath was not set.");

        [TestMethod]
        public void Constructor_CodeFix_WrongCodeFixedPath_Throws() =>
            DummyCodeFixCS.WithCodeFixedPaths("File.vb")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Path 'File.vb' doesn't match C# file extension '.cs'.");

        [TestMethod]
        public void Constructor_CodeFix_WrongCodeFixedPathBatch_Throws() =>
            DummyCodeFixCS.WithCodeFixedPaths("File.cs", "Batch.vb")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Path 'Batch.vb' doesn't match C# file extension '.cs'.");

        [TestMethod]
        public void Constructor_CodeFix_MultipleAnalyzers_Throws() =>
            DummyCodeFixCS.AddAnalyzer(() => new DummyAnalyzerCS())
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("When CodeFix is set, Analyzers must contain only 1 analyzer, but 2 were found.");

        [TestMethod]
        public void Constructor_CodeFix_MultiplePaths_Throws() =>
            DummyCodeFixCS.AddPaths("Second.cs", "Third.cs")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Paths must contain only 1 file, but 3 were found.");

        [TestMethod]
        public void Constructor_CodeFix_WithSnippets_Throws() =>
            DummyCodeFixCS.AddSnippet("Wrong")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("Snippets must be empty when CodeFix is set.");

        [TestMethod]
        public void Constructor_CodeFix_WrongLanguage_Throws() =>
            DummyCodeFixCS.WithCodeFix<DummyCodeFixVB>()
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("DummyAnalyzerCS language C# does not match DummyCodeFixVB language.");

        [TestMethod]
        public void Constructor_CodeFix_FixableDiagnosticsNotSupported_Throws() =>
            DummyCodeFixCS.WithCodeFix<EmptyMethodCodeFix>()
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("DummyAnalyzerCS does not support diagnostics fixable by the EmptyMethodCodeFix.");

        [TestMethod]
        public void Constructor_CodeFix_MissingAttribute_Throws() =>
            DummyCodeFixCS.WithCodeFix<DummyCodeFixNoAttribute>()
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("DummyCodeFixNoAttribute does not have ExportCodeFixProviderAttribute.");

        [TestMethod]
        public void Constructor_ProtobufPath_MultipleAnalyzers_Throws() =>
            DummyCS.AddSnippet("//Empty").WithProtobufPath("Proto.pb").AddAnalyzer(() => new DummyAnalyzerCS())
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("When ProtobufPath is set, Analyzers must contain only 1 analyzer, but 2 were found.");

        [TestMethod]
        public void Constructor_ProtobufPath_WrongAnalyzerType_Throws() =>
            DummyCS.AddSnippet("//Empty").WithProtobufPath("Proto.pb")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("DummyAnalyzerCS does not inherit from UtilityAnalyzerBase.");

        [TestMethod]
        public void Constructor_IsRazor_NoOptions_Throws() =>
            DummyCS.AddSnippet("//Empty", "File.razor").WithOptions(ImmutableArray<ParseOptions>.Empty)
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("IsRazor was set. ParseOptions must be specified.");

        [TestMethod]
        public void Constructor_IsRazor_VB_Throws() =>
            DummyVB.AddSnippet("//Empty", "File.razor")
                .Invoking(x => x.Build()).Should().Throw<ArgumentException>().WithMessage("IsRazor was set for Visual Basic analyzer. Only C# is supported.");

#if NET

        [TestMethod]
        public void Verify_RazorWithAssociatedCS() =>
            DummyCS.AddPaths(WriteFile("File.razor", """<p @bind="pValue">Dynamic content</p>"""))
                .AddPaths(WriteFile("File.razor.cs", """public partial class File { string pValue = "The value bound"; int a = 42;  }"""))
                .Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>();

        [TestMethod]
        public void Verify_RazorWithUnrelatedCS() =>
            DummyCS.AddPaths(WriteFile("File.razor", """<p @bind="pValue">Dynamic content</p>"""))
                .AddPaths(WriteFile("SomeSource.cs", """class SomeSource { int a = 42; }"""))
                .Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>();

        [TestMethod]
        public void Verify_RazorWithUnrelatedIssues() =>
            DummyCS.AddPaths(WriteFile("File.razor", """<p @bind="pValue">Dynamic content</p>"""))
                .AddPaths(WriteFile("SomeSource.cs", """class SomeSource { int a = 42; }"""))
                .AddPaths(WriteFile("Sample.cs", """
                    public class Sample
                    {
                        private int a = 42;     // Noncompliant {{Message for SDummy}}
                        private int b = 42;     // Noncompliant
                        private bool c = true;
                    }
                    """))
                .Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>();

        [DataTestMethod]
        [DataRow("Dummy.SecondaryLocation.razor")]
        [DataRow("Dummy.SecondaryLocation.cshtml")]
        public void Verify_RazorWithAdditionalLocation(string path) =>
            DummyWithLocation
                .AddPaths(path)
                .WithAdditionalFilePath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Product))
                .Verify();

        [TestMethod]
        [DataRow("Dummy.razor")]
        [DataRow("Dummy.cshtml")]
        public void Verify_Razor(string path) =>
            DummyWithLocation
                .AddPaths(path)
                .WithAdditionalFilePath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Product))
                .Verify();

        [DataTestMethod]
        [DataRow("Dummy.razor")]
        [DataRow("Dummy.cshtml")]
        public void Verify_RazorAnalysisIsDisabled_DoesNotRaise(string path) =>
            DummyWithLocation.AddPaths(path)
                .WithAdditionalFilePath(AnalysisScaffolding.CreateSonarLintXml(TestContext, analyzeRazorCode: false))
                .VerifyNoIssueReported();

        [DataTestMethod]
        [DataRow("Dummy.razor")]
        [DataRow("Dummy.cshtml")]
        public void Verify_RazorAnalysisInSLAndNugetContext_DoesNotRaise(string path) =>
            DummyWithLocation
                .AddPaths(path)
                .WithAdditionalFilePath(AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Unknown))
                .VerifyNoIssueReported();

        [Ignore("Revert after 9.19 release")]
        [DataTestMethod]
        [DataRow("net6.0")]
        [DataRow("net7.0")]
        public void Compile_Razor_WithFramework(string framework)
        {
            var compilation = DummyWithLocation.AddPaths("Dummy.razor")
                .WithFramework(framework)
                .WithLanguageVersion(LanguageVersion.CSharp12)
                .Build()
                .Compile(false)
                .Single();
            compilation.Compilation.ExternalReferences.First().Display.Should().Contain(framework);
        }

        [TestMethod]
        public void Compile_Razor_DefaultFramework()
        {
            var compilation = DummyWithLocation.AddPaths("Dummy.razor")
                .WithLanguageVersion(LanguageVersion.CSharp12)
                .Build()
                .Compile(false)
                .Single();
            compilation.Compilation.ExternalReferences.First().Display.Should().Contain("net7.0", "This version is used in EmptyProject.csproj");
        }

        [DataTestMethod]
        [DataRow("net48")]
        [DataRow("netcoreapp3.1")]
        [DataRow("netstandard2.1")]
        [DataRow("net5.0")]
        public void Verify_Razor_WithFramework_NotSupported(string framework)
        {
            var verifierBuilder = DummyWithLocation.AddPaths("Dummy.razor");
            verifierBuilder.WithFramework(framework)
                .Invoking(x => x.Verify())
                .Should()
                .Throw<InvalidOperationException>().WithMessage("Razor compilation is supported only for .NET 6 and .NET 7 frameworks.");
        }

        [TestMethod]
        public void Compile_Razor_ParseOptions_OldVersion() =>
            DummyWithLocation.AddPaths("Dummy.razor").WithOptions(ImmutableArray.Create<ParseOptions>(new CSharpParseOptions(LanguageVersion.CSharp6))).Build().Compile(false)
                .Invoking(x => x.ToArray()) // Force evaluation of the collection
                .Should()
                .Throw<NotSupportedException>();

        [TestMethod]
        public void Compile_Razor_ParseOptions_DefaultLanguageVersion()
        {
            // Version below C# 10 are not compatible with our EmptyProject scaffolding due to nullable context and global using statements.
            var expectedLanguageVersions = ParseOptionsHelper.FromCSharp10.Cast<CSharpParseOptions>().Select(x => x.LanguageVersion.ToString());
            var compilations = DummyWithLocation.AddPaths("Dummy.razor").Build().Compile(false);
            compilations.Select(c => c.Compilation.LanguageVersionString()).Should().BeEquivalentTo(expectedLanguageVersions);
        }

        [TestMethod]
        public void Compile_Razor_ParseOptions_WithSpecificVersion()
        {
            var builder = DummyWithLocation.AddPaths("Dummy.razor");
            // Version below C# 10 are not compatible with our EmptyProject scaffolding due to nullable context and global using statements.
            foreach (var options in ParseOptionsHelper.FromCSharp10)
            {
                // Update Verifier.LanguageVersionReference() in case this breaks
                builder.WithOptions(ImmutableArray.Create(options)).Build().Compile(false).Should().ContainSingle();
            }
        }

        [TestMethod]
        public void Compile_Razor_AddReferences()
        {
            var compilations = DummyWithLocation.AddPaths("Dummy.razor")
                .AddReferences(NuGetMetadataReference.MicrosoftAzureDocumentDB())
                .WithLanguageVersion(LanguageVersion.Latest)
                .Build()
                .Compile(false);
            var references = compilations.Single().Compilation.References;
            references.Should().Contain(x => x.Display.Contains("Microsoft.Azure.DocumentDB"));
        }

        [TestMethod]
        public void Compile_Razor_NoReferences()
        {
            var compilations = DummyWithLocation.AddPaths("Dummy.razor")
                .WithLanguageVersion(LanguageVersion.Latest)
                .Build()
                .Compile(false);
            var references = compilations.Single().Compilation.References;
            references.Should().NotContain(x => x.Display.Contains("Microsoft.Azure.DocumentDB"));
        }

#endif

        [TestMethod]
        public void Verify_ThrowsWithCodeFixSet()
        {
            var originalPath = WriteFile("File.cs", null);
            var fixedPath = WriteFile("File.Fixed.cs", null);
            DummyCS.AddPaths(originalPath).WithCodeFix<DummyCodeFixCS>().WithCodeFixedPaths(fixedPath).Invoking(x => x.Verify())
                .Should().Throw<InvalidOperationException>().WithMessage("Cannot use Verify with CodeFix set.");
        }

        [TestMethod]
        public void Verify_RaiseExpectedIssues_CS() =>
            WithSnippetCS("""
                public class Sample
                {
                    private int a = 42;     // Noncompliant {{Message for SDummy}}
                    private int b = 42;     // Noncompliant
                    private bool c = true;
                }
                """).Invoking(x => x.Verify()).Should().NotThrow();

        [TestMethod]
        public void Verify_RaiseExpectedIssues_VB() =>
            WithSnippetVB("""
                Public Class Sample
                    Private A As Integer = 42   ' Noncompliant {{Message for SDummy}}
                    Private B As Integer = 42   ' Noncompliant
                    Private C As Boolean = True
                End Class
                """).Invoking(x => x.Verify()).Should().NotThrow();

        [TestMethod]
        public void Verify_RaiseUnexpectedIssues_CS() =>
            WithSnippetCS("""
                public class Sample
                {
                    private int a = 42;     // FP
                    private int b = 42;     // FP
                    private bool c = true;
                }
                """).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 3: Unexpected issue 'Message for SDummy' Rule SDummy
                  Line 4: Unexpected issue 'Message for SDummy' Rule SDummy

                There are differences for CSharp7 File.cs:
                  Line 3: Unexpected issue 'Message for SDummy' Rule SDummy
                  Line 4: Unexpected issue 'Message for SDummy' Rule SDummy
                """);

        [TestMethod]
        public void Verify_RaiseUnexpectedIssues_VB() =>
            WithSnippetVB("""
                Public Class Sample
                    Private A As Integer = 42   ' FP
                    Private B As Integer = 42   ' FP
                    Private C As Boolean = True
                End Class
                """).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for VisualBasic12 File.Concurrent.vb:
                  Line 2: Unexpected issue 'Message for SDummy' Rule SDummy
                  Line 3: Unexpected issue 'Message for SDummy' Rule SDummy

                There are differences for VisualBasic12 File.vb:
                  Line 2: Unexpected issue 'Message for SDummy' Rule SDummy
                  Line 3: Unexpected issue 'Message for SDummy' Rule SDummy
                """);

        [TestMethod]
        public void Verify_MissingExpectedIssues() =>
            WithSnippetCS("""
                public class Sample
                {
                    private bool a = true;   // Noncompliant - FN
                    private bool b = true;   // Noncompliant - FN
                    private bool c = true;
                }
                """).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 3: Missing expected issue
                  Line 4: Missing expected issue

                There are differences for CSharp7 File.cs:
                  Line 3: Missing expected issue
                  Line 4: Missing expected issue

                """);

        [TestMethod]
        public void Verify_TwoAnalyzers() =>
            WithSnippetCS("""
                public class Sample
                {
                    private int a = 42;     // Noncompliant {{Message for SDummy}}
                                            // Noncompliant@-1
                    private int b = 42;     // Noncompliant
                                            // Noncompliant@-1
                    private bool c = true;
                }
                """)
            .AddAnalyzer(() => new DummyAnalyzerCS()) // Duplicate
            .Invoking(x => x.Verify()).Should().NotThrow();

        [TestMethod]
        public void Verify_TwoPaths() =>
            WithSnippetCS("""
                public class First
                {
                    private bool a = true;     // Noncompliant - FN in File.cs
                }
                """)
            .AddPaths(WriteFile("Second.cs", """
                public class Second
                {
                    private bool a = true;     // Noncompliant - FN in Second.cs
                }
                """))
            .WithConcurrentAnalysis(false)
            .Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.cs:
                  Line 3: Missing expected issue

                There are differences for CSharp7 Second.cs:
                  Line 3: Missing expected issue
                """);

        [TestMethod]
        public void Verify_AutogenerateConcurrentFiles()
        {
            var builder = WithSnippetCS("// Noncompliant - FN");
            // Concurrent analysis by-default automatically generates concurrent files - File.Concurrent.cs
            builder.Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 1: Missing expected issue

                There are differences for CSharp7 File.cs:
                  Line 1: Missing expected issue

                """);
            // When AutogenerateConcurrentFiles is turned off, only the provided snippet is analyzed
            builder.WithAutogenerateConcurrentFiles(false).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.cs:
                  Line 1: Missing expected issue

                """);
        }

        [TestMethod]
        public void Verify_TestProject()
        {
            var builder = new VerifierBuilder<DoNotWriteToStandardOutput>() // Rule with scope Main
                .AddSnippet("public class Sample { public void Main() { System.Console.WriteLine(); } }");
            builder.Invoking(x => x.Verify()).Should().Throw<AssertFailedException>();
            builder.AddTestReference().Invoking(x => x.Verify()).Should().NotThrow("Project references should be recognized as Test code.");
        }

        [TestMethod]
        public void Verify_ParseOptions()
        {
            var builder = WithSnippetCS("""
                public class Sample
                {
                    private System.Exception ex = new(); // C# 9 target-typed new
                }
                """);
            builder.WithOptions(ParseOptionsHelper.FromCSharp9).Invoking(x => x.Verify()).Should().NotThrow();
            builder.WithOptions(ParseOptionsHelper.BeforeCSharp9).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp5 File.Concurrent.cs:
                  Line 3: Unexpected error, use // Error [CS8026] Feature 'target-typed object creation' is not available in C# 5. Please use language version 9.0 or greater.

                There are differences for CSharp5 File.cs:
                  Line 3: Unexpected error, use // Error [CS8026] Feature 'target-typed object creation' is not available in C# 5. Please use language version 9.0 or greater.
                """);
        }

        [TestMethod]
        public void Verify_BasePath()
        {
            DummyCS.AddPaths("Nonexistent.cs").Invoking(x => x.Verify()).Should().Throw<FileNotFoundException>("This file should not exist in TestCases directory.");
            DummyCS.AddPaths("Verifier.BasePathAssertFails.cs").Invoking(x => x.Verify()).Should().Throw<AssertFailedException>("File should be found in TestCases directory.");
            DummyCS.WithBasePath("Verifier").AddPaths("Verifier.BasePath.cs").Invoking(x => x.Verify()).Should().NotThrow();
        }

        [TestMethod]
        public void Verify_ErrorBehavior()
        {
            var builder = WithSnippetCS("undefined");
            builder.Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>()
                .WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 1: Unexpected error, use // Error [CS0116] A namespace cannot directly contain members such as fields, methods or statements

                There are differences for CSharp7 File.cs:
                  Line 1: Unexpected error, use // Error [CS0246] The type or namespace name 'undefined' could not be found (are you missing a using directive or an assembly reference?)
                  Line 1: Unexpected error, use // Error [CS8107] Feature 'top-level statements' is not available in C# 7.0. Please use language version 9.0 or greater.
                  Line 1: Unexpected error, use // Error [CS8805] Program using top-level statements must be an executable.
                  Line 1: Unexpected error, use // Error [CS1001] Identifier expected
                  Line 1: Unexpected error, use // Error [CS1002] ; expected
                """);
            builder.WithErrorBehavior(CompilationErrorBehavior.FailTest).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 1: Unexpected error, use // Error [CS0116] A namespace cannot directly contain members such as fields, methods or statements

                There are differences for CSharp7 File.cs:
                  Line 1: Unexpected error, use // Error [CS0246] The type or namespace name 'undefined' could not be found (are you missing a using directive or an assembly reference?)
                  Line 1: Unexpected error, use // Error [CS8107] Feature 'top-level statements' is not available in C# 7.0. Please use language version 9.0 or greater.
                  Line 1: Unexpected error, use // Error [CS8805] Program using top-level statements must be an executable.
                  Line 1: Unexpected error, use // Error [CS1001] Identifier expected
                  Line 1: Unexpected error, use // Error [CS1002] ; expected
                """);
            builder.WithErrorBehavior(CompilationErrorBehavior.Ignore).Invoking(x => x.Verify()).Should().NotThrow();
        }

        [TestMethod]
        public void Verify_OnlyDiagnostics()
        {
            var builder = new VerifierBuilder<SymbolicExecutionRunner>().AddPaths(WriteFile("File.cs", """
                public class Sample
                {
                    public void Method()
                    {
                        var t = true;
                        if (t)          // S2583
                            t = true;
                        else
                            t = true;
                        if (t)          // S2589
                            t = true;
                    }
                }
                """));
            builder.Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 6: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'. Some code paths are unreachable.' Rule S2583
                  Line 10: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'.' Rule S2589
                  Line 9 Secondary location: Unexpected issue '' Rule S2583

                There are differences for CSharp7 File.cs:
                  Line 6: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'. Some code paths are unreachable.' Rule S2583
                  Line 10: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'.' Rule S2589
                  Line 9 Secondary location: Unexpected issue '' Rule S2583
                """);
            builder.WithOnlyDiagnostics(ConditionEvaluatesToConstant.S2589).Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp7 File.Concurrent.cs:
                  Line 10: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'.' Rule S2589

                There are differences for CSharp7 File.cs:
                  Line 10: Unexpected issue 'Change this condition so that it does not always evaluate to 'True'.' Rule S2589
                """);
            builder.WithOnlyDiagnostics(NullPointerDereference.S2259).Invoking(x => x.Verify()).Should().NotThrow();
        }

        [TestMethod]
        public void Verify_NonConcurrentAnalysis()
        {
            var builder = WithSnippetCS("var topLevelStatement = true;").WithOptions(ParseOptionsHelper.FromCSharp9).WithOutputKind(OutputKind.ConsoleApplication);
            builder.Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>("Default Verifier behavior duplicates the source file.").WithMessage("""
                There are differences for CSharp9 File.Concurrent.cs:
                  Line 1: Unexpected error, use // Error [CS0825] The contextual keyword 'var' may only appear within a local variable declaration or in script code
                  Line 1: Unexpected error, use // Error [CS0116] A namespace cannot directly contain members such as fields, methods or statements
                """);
            builder.WithConcurrentAnalysis(false).Invoking(x => x.Verify()).Should().NotThrow();
        }

        [TestMethod]
        public void Verify_OutputKind()
        {
            var builder = WithSnippetCS("var topLevelStatement = true;").WithOptions(ParseOptionsHelper.FromCSharp9);
            builder.WithTopLevelStatements().Invoking(x => x.Verify()).Should().NotThrow();
            builder.WithOutputKind(OutputKind.ConsoleApplication).WithConcurrentAnalysis(false).Invoking(x => x.Verify()).Should().NotThrow();
            builder.Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                There are differences for CSharp9 File.Concurrent.cs:
                  Line 1: Unexpected error, use // Error [CS0825] The contextual keyword 'var' may only appear within a local variable declaration or in script code
                  Line 1: Unexpected error, use // Error [CS0116] A namespace cannot directly contain members such as fields, methods or statements

                There are differences for CSharp9 File.cs:
                  Line 1: Unexpected error, use // Error [CS8805] Program using top-level statements must be an executable.
                """);
        }

        [TestMethod]
        public void Verify_Snippets() =>
            DummyCS.AddSnippet("public class First { } // Noncompliant [first]  - not raised")
                .AddSnippet("public class Second { } // Noncompliant [second] - not raised")
                .Invoking(x => x.Verify()).Should().Throw<DiagnosticVerifierException>().WithMessage("""
                    There are differences for CSharp7 snippet0.cs:
                      Line 1: Missing expected issue ID first

                    There are differences for CSharp7 snippet1.cs:
                      Line 1: Missing expected issue ID second
                    """);

        [TestMethod]
        public void VerifyCodeFix_FixExpected_CS()
        {
            var originalPath = WriteFile("File.cs", """
                public class Sample
                {
                    private int a = 0;     // Noncompliant
                    private int b = 0;     // Noncompliant
                    private bool c = true;
                }
                """);
            var fixedPath = WriteFile("File.Fixed.cs", """
                public class Sample
                {
                    private int a = default;     // Fixed
                    private int b = default;     // Fixed
                    private bool c = true;
                }
                """);
            DummyCS.AddPaths(originalPath).WithCodeFix<DummyCodeFixCS>().WithCodeFixedPaths(fixedPath).Invoking(x => x.VerifyCodeFix()).Should().NotThrow();
        }

        [TestMethod]
        public void VerifyCodeFix_FixExpected_VB()
        {
            var originalPath = WriteFile("File.vb", """
                Public Class Sample
                    Private A As Integer = 42   ' Noncompliant
                    Private B As Integer = 42   ' Noncompliant
                    Private C As Boolean = True
                End Class
                """);
            var fixedPath = WriteFile("File.Fixed.vb", """
                Public Class Sample
                    Private A As Integer = Nothing   ' Fixed
                    Private B As Integer = Nothing   ' Fixed
                    Private C As Boolean = True
                End Class
                """);
            DummyVB.AddPaths(originalPath).WithCodeFix<DummyCodeFixVB>().WithCodeFixedPaths(fixedPath).Invoking(x => x.VerifyCodeFix()).Should().NotThrow();
        }

        [TestMethod]
        public void VerifyCodeFix_NotFixed_CS()
        {
            var originalPath = WriteFile("File.cs", """
                public class Sample
                {
                    private int a = 0;     // Noncompliant
                    private int b = 0;     // Noncompliant
                    private bool c = true;
                }
                """);
            DummyCS.AddPaths(originalPath).WithCodeFix<DummyCodeFixCS>().WithCodeFixedPaths(originalPath).Invoking(x => x.VerifyCodeFix()).Should().Throw<AssertFailedException>().WithMessage("""
                Expected * to be*
                "public class Sample
                {
                    private int a = 0;     // Noncompliant
                    private int b = 0;     // Noncompliant
                    private bool c = true;
                }" with a length of 136 because VerifyWhileDocumentChanges updates the document until all issues are fixed, even if the fix itself creates a new issue again. Language: CSharp7, but*
                "public class Sample
                {
                    private int a = default;     // Fixed
                    private int b = default;     // Fixed
                    private bool c = true;
                }" has a length of 134, differs near "def" (index 42).
                """);
        }

        [TestMethod]
        public void VerifyCodeFix_NotFixed_VB()
        {
            var originalPath = WriteFile("File.vb", """
                Public Class Sample
                    Private A As Integer = 42   ' Noncompliant
                    Private B As Integer = 42   ' Noncompliant
                    Private C As Boolean = True
                End Class
                """);
            DummyVB.AddPaths(originalPath).WithCodeFix<DummyCodeFixVB>().WithCodeFixedPaths(originalPath).Invoking(x => x.VerifyCodeFix()).Should().Throw<AssertFailedException>().WithMessage("""
                Expected * to be*
                "Public Class Sample
                    Private A As Integer = 42   ' Noncompliant
                    Private B As Integer = 42   ' Noncompliant
                    Private C As Boolean = True
                End Class" with a length of 155 because VerifyWhileDocumentChanges updates the document until all issues are fixed, even if the fix itself creates a new issue again. Language: VisualBasic12, but*
                "Public Class Sample
                    Private A As Integer = Nothing   ' Fixed
                    Private B As Integer = Nothing   ' Fixed
                    Private C As Boolean = True
                End Class" has a length of 151, differs near "Not" (index 47).
                """);
        }

        [TestMethod]
        public void VerifyNoIssueReported_NoIssues_Succeeds() =>
            WithSnippetCS("// Noncompliant - this comment is ignored").Invoking(x => x.VerifyNoIssueReported()).Should().NotThrow();

        [TestMethod]
        public void VerifyNoIssueReported_WithIssues_Throws() =>
            WithSnippetCS("""
                public class Sample
                {
                    private int a = 42;     // This will raise an issue
                }
                """).Invoking(x => x.VerifyNoIssueReported()).Should().Throw<AssertFailedException>();

        [TestMethod]
        public void Verify_ConcurrentAnalysis_FileEndingWithComment_CS() =>
            WithSnippetCS("// Nothing to see here, file ends with a comment").Invoking(x => x.Verify()).Should().NotThrow();

        [TestMethod]
        public void Verify_ConcurrentAnalysis_FileEndingWithComment_VB() =>
            WithSnippetVB("' Nothing to see here, file ends with a comment").Invoking(x => x.Verify()).Should().NotThrow();

        [TestMethod]
        public void VerifyUtilityAnalyzerProducesEmptyProtobuf_EmptyFile()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Empty.pb");
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerCS(protobufPath, null)).AddSnippet("// Nothing to see here").WithProtobufPath(protobufPath)
                .Invoking(x => x.VerifyUtilityAnalyzerProducesEmptyProtobuf())
                .Should().NotThrow();
        }

        [TestMethod]
        public void VerifyUtilityAnalyzerProducesEmptyProtobuf_WithContent()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Empty.pb");
            var message = new LogInfo { Text = "Lorem Ipsum" };
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerCS(protobufPath, message)).AddSnippet("// Nothing to see here").WithProtobufPath(protobufPath)
                .Invoking(x => x.VerifyUtilityAnalyzerProducesEmptyProtobuf())
                .Should().Throw<AssertFailedException>().WithMessage("Expected value to be 0L because protobuf file should be empty, but found *");
        }

        [TestMethod]
        public void VerifyUtilityAnalyzer_CorrectProtobuf_CS()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Log.pb");
            var message = new LogInfo { Text = "Lorem Ipsum" };
            var wasInvoked = false;
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerCS(protobufPath, message)).AddSnippet("// Nothing to see here").WithProtobufPath(protobufPath)
                .VerifyUtilityAnalyzer<LogInfo>(x =>
                {
                    x.Should().ContainSingle().Which.Text.Should().Be("Lorem Ipsum");
                    wasInvoked = true;
                });
            wasInvoked.Should().BeTrue();
        }

        [TestMethod]
        public void VerifyUtilityAnalyzer_CorrectProtobuf_VB()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Log.pb");
            var message = new LogInfo { Text = "Lorem Ipsum" };
            var wasInvoked = false;
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerVB(protobufPath, message)).AddSnippet("' Nothing to see here").WithProtobufPath(protobufPath)
                .VerifyUtilityAnalyzer<LogInfo>(x =>
                {
                    x.Should().ContainSingle().Which.Text.Should().Be("Lorem Ipsum");
                    wasInvoked = true;
                });
            wasInvoked.Should().BeTrue();
        }

        [TestMethod]
        public void VerifyUtilityAnalyzer_VerifyProtobuf_PropagateFailedAssertion_CS()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Empty.pb");
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerCS(protobufPath, null)).AddSnippet("// Nothing to see here").WithProtobufPath(protobufPath)
                .Invoking(x => x.VerifyUtilityAnalyzer<LogInfo>(x => throw new AssertFailedException("Some failed assertion about Protobuf")))
                .Should().Throw<AssertFailedException>().WithMessage("Some failed assertion about Protobuf");
        }

        [TestMethod]
        public void VerifyUtilityAnalyzer_VerifyProtobuf_PropagateFailedAssertion_VB()
        {
            var protobufPath = TestHelper.TestPath(TestContext, "Empty.pb");
            new VerifierBuilder().AddAnalyzer(() => new DummyUtilityAnalyzerVB(protobufPath, null)).AddSnippet("' Nothing to see here").WithProtobufPath(protobufPath)
                .Invoking(x => x.VerifyUtilityAnalyzer<LogInfo>(x => throw new AssertFailedException("Some failed assertion about Protobuf")))
                .Should().Throw<AssertFailedException>().WithMessage("Some failed assertion about Protobuf");
        }

        private VerifierBuilder WithSnippetCS(string code) =>
            DummyCS.AddPaths(WriteFile("File.cs", code));

        private VerifierBuilder WithSnippetVB(string code) =>
            DummyVB.AddPaths(WriteFile("File.vb", code));

        private string WriteFile(string name, string content) =>
            TestHelper.WriteFile(TestContext, $@"TestCases\{name}", content);
    }
}
