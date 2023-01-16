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
using Moq;
using SonarAnalyzer.AnalysisContext;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.Helpers;
using SonarAnalyzer.UnitTest.Rules;
using RoslynAnalysisContext = Microsoft.CodeAnalysis.Diagnostics.AnalysisContext;

namespace SonarAnalyzer.UnitTest.AnalysisContext;

[TestClass]
public partial class SonarAnalysisContextTest
{
    public TestContext TestContext { get; set; }

    private sealed class TestSetup
    {
        public string Path { get; }
        public DiagnosticAnalyzer Analyzer { get; }
        public VerifierBuilder Builder { get; }

        public TestSetup(string testCase, SonarDiagnosticAnalyzer analyzer) : this(testCase, analyzer, Enumerable.Empty<MetadataReference>()) { }

        public TestSetup(string testCase, SonarDiagnosticAnalyzer analyzer, IEnumerable<MetadataReference> additionalReferences)
        {
            Path = testCase;
            Analyzer = analyzer;
            additionalReferences = additionalReferences
                .Concat(MetadataReferenceFacade.SystemComponentModelPrimitives)
                .Concat(NetStandardMetadataReference.Netstandard)
                .Concat(MetadataReferenceFacade.SystemData);
            Builder = new VerifierBuilder().AddAnalyzer(() => analyzer).AddPaths(Path).AddReferences(additionalReferences);
        }
    }

    // Various classes that invoke all the `ReportIssue` methods in AnalysisContextExtensions
    // We mention in comments the type of Context that is used to invoke (directly or indirectly) the `ReportIssue` method
    private readonly List<TestSetup> testCases = new(
        new[]
        {
            // SyntaxNodeAnalysisContext
            // S3244 - MAIN and TEST
            new TestSetup("AnonymousDelegateEventUnsubscribe.cs", new AnonymousDelegateEventUnsubscribe()),
            // S2699 - TEST only
            new TestSetup(
                "TestMethodShouldContainAssertion.NUnit.cs",
                new TestMethodShouldContainAssertion(),
                TestMethodShouldContainAssertionTest.WithTestReferences(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion)).References),    // ToDo: Reuse the entire builder in TestSetup

            // SyntaxTreeAnalysisContext
            // S3244 - MAIN and TEST
            new TestSetup("AsyncAwaitIdentifier.cs", new AsyncAwaitIdentifier()),

            // CompilationAnalysisContext
            // S3244 - MAIN and TEST
            new TestSetup(
                @"Hotspots\RequestsWithExcessiveLength.cs",
                new RequestsWithExcessiveLength(AnalyzerConfiguration.AlwaysEnabled),
                RequestsWithExcessiveLengthTest.GetAdditionalReferences()),

            // CodeBlockAnalysisContext
            // S5693 - MAIN and TEST
            new TestSetup("GetHashCodeEqualsOverride.cs", new GetHashCodeEqualsOverride()),

            // SymbolAnalysisContext
            // S2953 - MAIN only
            new TestSetup("DisposeNotImplementingDispose.cs", new DisposeNotImplementingDispose()),
            // S1694 - MAIN only
            new TestSetup("ClassShouldNotBeAbstract.cs", new ClassShouldNotBeAbstract()),
        });

    [TestMethod]
    public void Constructor_Null()
    {
        var supportedDiag = Enumerable.Empty<DiagnosticDescriptor>();
        var roslynContext = Mock.Of<RoslynAnalysisContext>();

        ((Func<SonarAnalysisContext>)(() => new(null, supportedDiag))).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("analysisContext");
        ((Func<SonarAnalysisContext>)(() => new(roslynContext, null))).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("supportedDiagnostics");
    }

    [TestMethod]
    public void WhenShouldAnalysisBeDisabledReturnsTrue_NoIssueReported()
    {
        SonarAnalysisContext.ShouldExecuteRegisteredAction = (_, _) => false;
        try
        {
            foreach (var testCase in testCases)
            {
                // ToDo: We should find a way to ack the fact the action was not run
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .VerifyNoIssueReported();
            }
        }
        finally
        {
            SonarAnalysisContext.ShouldExecuteRegisteredAction = null;
        }
    }

    [TestMethod]
    public void ByDefault_ExecuteRule()
    {
        foreach (var testCase in testCases)
        {
            // ToDo: We test that a rule is enabled only by checking the issues are reported
            testCase.Builder
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();
        }
    }

    [TestMethod]
    public void WhenProjectType_IsTest_RunRulesWithTestScope_SonarLint()
    {
        var sonarProjectConfig = AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Test, false);
        foreach (var testCase in testCases)
        {
            var hasTestScope = testCase.Analyzer.SupportedDiagnostics.Any(d => d.CustomTags.Contains(DiagnosticDescriptorFactory.TestSourceScopeTag));
            if (hasTestScope)
            {
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .Verify();
            }
            else
            {
                // MAIN-only
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .VerifyNoIssueReported();
            }
        }
    }

    [TestMethod]
    public void WhenProjectType_IsTest_RunRulesWithTestScope_Scanner()
    {
        var sonarProjectConfig = AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Test);
        foreach (var testCase in testCases)
        {
            var hasProductScope = testCase.Analyzer.SupportedDiagnostics.Any(d => d.CustomTags.Contains(DiagnosticDescriptorFactory.MainSourceScopeTag));
            if (hasProductScope)
            {
                // MAIN-only and MAIN & TEST rules
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .VerifyNoIssueReported();
            }
            else
            {
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .Verify();
            }
        }
    }

    [TestMethod]
    public void WhenProjectType_IsTest_RunRulesWithMainScope()
    {
        var sonarProjectConfig = AnalysisScaffolding.CreateSonarProjectConfig(TestContext, ProjectType.Product);
        foreach (var testCase in testCases)
        {
            var hasProductScope = testCase.Analyzer.SupportedDiagnostics.Any(d => d.CustomTags.Contains(DiagnosticDescriptorFactory.MainSourceScopeTag));
            if (hasProductScope)
            {
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .Verify();
            }
            else
            {
                // TEST-only rule
                testCase.Builder
                    .WithOptions(ParseOptionsHelper.FromCSharp8)
                    .WithSonarProjectConfigPath(sonarProjectConfig)
                    .VerifyNoIssueReported();
            }
        }
    }

    [TestMethod]
    public void WhenAnalysisDisabledBaseOnSyntaxTree_ReportIssuesForEnabledRules()
    {
        testCases.Should().HaveCountGreaterThan(2);

        try
        {
            var testCase = testCases[0];
            var testCase2 = testCases[2];
            SonarAnalysisContext.ShouldExecuteRegisteredAction = (diags, tree) => tree.FilePath.EndsWith(new FileInfo(testCase.Path).Name, StringComparison.OrdinalIgnoreCase);
            testCase.Builder.WithConcurrentAnalysis(false).Verify();
            testCase2.Builder.VerifyNoIssueReported();
        }
        finally
        {
            SonarAnalysisContext.ShouldExecuteRegisteredAction = null;
        }
    }

    [TestMethod]
    public void WhenReportDiagnosticActionNotNull_AllowToControlWhetherOrNotToReport()
    {
        try
        {
            SonarAnalysisContext.ReportDiagnostic = context =>
            {
                // special logic for rules with SyntaxNodeAnalysisContext
                if (context.Diagnostic.Id != AnonymousDelegateEventUnsubscribe.DiagnosticId && context.Diagnostic.Id != TestMethodShouldContainAssertion.DiagnosticId)
                {
                    // Verifier expects all diagnostics to increase the counter in order to check that all rules call the
                    // extension method and not the direct `ReportDiagnostic`.
                    DiagnosticVerifier.SuppressionHandler.IncrementReportCount(context.Diagnostic.Id);
                    context.ReportDiagnostic(context.Diagnostic);
                }
            };

            // Because the Verifier sets the SonarAnalysisContext.ShouldDiagnosticBeReported delegate we end up in a case
            // where the Debug.Assert of the AnalysisContextExtensions.ReportDiagnostic() method will raise.
            using (new AssertIgnoreScope())
            {
                foreach (var testCase in testCases)
                {
                    // special logic for rules with SyntaxNodeAnalysisContext
                    if (testCase.Analyzer is AnonymousDelegateEventUnsubscribe || testCase.Analyzer is TestMethodShouldContainAssertion)
                    {
                        testCase.Builder
                            .WithOptions(ParseOptionsHelper.FromCSharp8)
                            .VerifyNoIssueReported();
                    }
                    else
                    {
                        testCase.Builder
                            .WithOptions(ParseOptionsHelper.FromCSharp8)
                            .Verify();
                    }
                }
            }
        }
        finally
        {
            SonarAnalysisContext.ReportDiagnostic = null;
        }
    }

    [DataTestMethod]
    [DataRow(ProjectType.Product, false)]
    [DataRow(ProjectType.Test, true)]
    public void IsTestProject_Standalone(ProjectType projectType, bool expectedResult)
    {
        var compilation = new SnippetCompiler("// Nothing to see here", TestHelper.ProjectTypeReference(projectType)).SemanticModel.Compilation;
        var context = new CompilationAnalysisContext(compilation, AnalysisScaffolding.CreateOptions(), null, null, default);
        var sut = new SonarCompilationReportingContext(AnalysisScaffolding.CreateSonarAnalysisContext(), context);

        sut.IsTestProject().Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow(ProjectType.Product, false)]
    [DataRow(ProjectType.Test, true)]
    public void IsTestProject_WithConfigFile(ProjectType projectType, bool expectedResult)
    {
        var configPath = AnalysisScaffolding.CreateSonarProjectConfig(TestContext, projectType);
        var context = new CompilationAnalysisContext(null, AnalysisScaffolding.CreateOptions(configPath), null, null, default);
        var sut = new SonarCompilationReportingContext(AnalysisScaffolding.CreateSonarAnalysisContext(), context);

        sut.IsTestProject().Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow(SnippetFileName, false)]
    [DataRow(AnotherFileName, true)]
    public void ReportDiagnosticIfNonGenerated_UnchangedFiles_CompilationAnalysisContext(string unchangedFileName, bool expected)
    {
        var context = new DummyAnalysisContext(TestContext, unchangedFileName);
        var wasReported = false;
        var location = context.Tree.GetRoot().GetLocation();
        var symbol = Mock.Of<ISymbol>(x => x.Locations == ImmutableArray.Create(location));
        var symbolContext = new SymbolAnalysisContext(symbol, context.Model.Compilation, context.Options, _ => wasReported = true, _ => true, default);
        var sut = new SonarSymbolReportingContext(new SonarAnalysisContext(context, DummyMainDescriptor), symbolContext);
        sut.ReportIssue(CSharpGeneratedCodeRecognizer.Instance, Mock.Of<Diagnostic>(x => x.Id == "Sxxx" && x.Location == location && x.Descriptor == DummyMainDescriptor[0]));

        wasReported.Should().Be(expected);
    }
}
