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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TestMethodShouldContainAssertion : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2699";
        private const string MessageFormat = "Add at least one assertion to this test case.";
        private const string CustomAssertionAttributeName = "AssertionMethodAttribute";
        private const int MaxInvocationDepth = 2; // Consider BFS instead of DFS if this gets increased

        private static readonly Dictionary<string, KnownType[]> KnownAssertions = new Dictionary<string, KnownType[]>
        {
            {"DidNotReceive", new[] {KnownType.NSubstitute_SubstituteExtensions}},
            {"DidNotReceiveWithAnyArgs", new[] {KnownType.NSubstitute_SubstituteExtensions}},
            {"Received", new[] {KnownType.NSubstitute_SubstituteExtensions, KnownType.NSubstitute_ReceivedExtensions_ReceivedExtensions}},
            {"ReceivedWithAnyArgs", new[] {KnownType.NSubstitute_SubstituteExtensions, KnownType.NSubstitute_ReceivedExtensions_ReceivedExtensions}},
            {"InOrder", new[] {KnownType.NSubstitute_Received}}
        };

        /// The assertions in the Shouldly library are supported by <see cref="UnitTestHelper.KnownAssertionMethodParts"/> (they all contain "Should")
        private static readonly ImmutableArray<KnownType> KnownAssertionTypes = ImmutableArray.Create(
            KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_Assert,
            KnownType.NFluent_Check,
            KnownType.NUnit_Framework_Assert,
            KnownType.Xunit_Assert);

        private static readonly ImmutableArray<KnownType> KnownAsertionExceptionTypes = ImmutableArray.Create(
            KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_AssertFailedException,
            KnownType.NFluent_FluentCheckException,
            KnownType.NFluent_Kernel_FluentCheckException,
            KnownType.NUnit_Framework_AssertionException,
            KnownType.Xunit_Sdk_AssertException,
            KnownType.Xunit_Sdk_XunitException);

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(
                c =>
                {
                    var methodDeclaration = MethodDeclarationFactory.Create(c.Node);
                    if (!methodDeclaration.Identifier.IsMissing
                        && methodDeclaration.HasImplementation
                        && c.SemanticModel.GetDeclaredSymbol(c.Node) is IMethodSymbol method
                        && method.IsTestMethod()
                        && !method.HasExpectedExceptionAttribute()
                        && !method.HasAssertionInAttribute()
                        && !method.IsIgnoredTestMethod()
                        && !ContainsAssertion(c.Node, c.SemanticModel, new HashSet<IMethodSymbol>(), 0))
                    {
                        c.ReportIssue(Rule, methodDeclaration.Identifier);
                    }
                },
                SyntaxKind.MethodDeclaration,
                SyntaxKindEx.LocalFunctionStatement);

        private static bool ContainsAssertion(SyntaxNode methodDeclaration, SemanticModel previousSemanticModel, ISet<IMethodSymbol> visitedSymbols, int level)
        {
            var currentSemanticModel = methodDeclaration.EnsureCorrectSemanticModelOrDefault(previousSemanticModel);
            if (currentSemanticModel == null)
            {
                return false;
            }

            var descendantNodes = methodDeclaration.DescendantNodes();
            var invocations = descendantNodes.OfType<InvocationExpressionSyntax>().ToArray();
            if (invocations.Any(x => IsAssertion(x))
                || descendantNodes.OfType<ThrowStatementSyntax>().Any(x => x.Expression != null && currentSemanticModel.GetTypeInfo(x.Expression).Type.DerivesFromAny(KnownAsertionExceptionTypes)))
            {
                return true;
            }

            var invokedSymbols = invocations.Select(expression => currentSemanticModel.GetSymbolInfo(expression).Symbol).OfType<IMethodSymbol>();
            if (invokedSymbols.Any(symbol => IsKnownAssertion(symbol) || IsCustomAssertion(symbol)))
            {
                return true;
            }

            if (level == MaxInvocationDepth)
            {
                return false;
            }

            foreach (var symbol in invokedSymbols.Where(x => !visitedSymbols.Contains(x)))
            {
                visitedSymbols.Add(symbol);
                foreach (var invokedDeclaration in symbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).OfType<MethodDeclarationSyntax>())
                {
                    if (ContainsAssertion(invokedDeclaration, currentSemanticModel, visitedSymbols, level + 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsAssertion(InvocationExpressionSyntax invocation) =>
            invocation.Expression
                .ToString()
                .SplitCamelCaseToWords()
                .Intersect(UnitTestHelper.KnownAssertionMethodParts)
                .Any();

        private static bool IsKnownAssertion(ISymbol methodSymbol) =>
            (KnownAssertions.GetValueOrDefault(methodSymbol.Name) is { } types && types.Any(x => methodSymbol.ContainingType.ConstructedFrom.Is(x)))
            || methodSymbol.ContainingType.DerivesFromAny(KnownAssertionTypes);

        private static bool IsCustomAssertion(ISymbol methodSymbol) =>
            methodSymbol.GetAttributesWithInherited().Any(x => x.AttributeClass.Name == CustomAssertionAttributeName);
    }
}
