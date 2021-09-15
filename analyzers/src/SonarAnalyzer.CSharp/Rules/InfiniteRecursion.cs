﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public partial class InfiniteRecursion : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2190";
        private const string MessageFormat = "Add a way to break out of this {0}.";

        private readonly IChecker checker;

        private static DiagnosticDescriptor Rule => DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public InfiniteRecursion() : this(AnalyzerConfiguration.AlwaysEnabled) { }

        internal /* for testing */ InfiniteRecursion(IAnalyzerConfiguration configuration) =>
            checker = (CFG.Roslyn.ControlFlowGraph.IsAvailable && !configuration.ForceSonarCfg)
                ? (IChecker)new RoslynChecker()
                : new SonarChecker();

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var method = (MethodDeclarationSyntax)c.Node;
                    CheckForNoExitMethod(c, (CSharpSyntaxNode)method.Body ?? method.ExpressionBody, method.Identifier);
                },
                SyntaxKind.MethodDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var function = (LocalFunctionStatementSyntaxWrapper)c.Node;
                    CheckForNoExitMethod(c, (CSharpSyntaxNode)function.Body ?? function.ExpressionBody, function.Identifier);
                },
                SyntaxKindEx.LocalFunctionStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var property = (PropertyDeclarationSyntax)c.Node;
                    if (c.SemanticModel.GetDeclaredSymbol(property) is { } propertySymbol)
                    {
                        checker.CheckForNoExitProperty(c, property, propertySymbol);
                    }
                },
                SyntaxKind.PropertyDeclaration);
        }

        private void CheckForNoExitMethod(SyntaxNodeAnalysisContext c, CSharpSyntaxNode body, SyntaxToken identifier)
        {
            if (body != null && c.SemanticModel.GetDeclaredSymbol(c.Node) is { } symbol)
            {
                checker.CheckForNoExitMethod(c, body, identifier, symbol);
            }
        }

        private static bool IsInstructionOnThisAndMatchesDeclaringSymbol(SyntaxNode node, ISymbol declaringSymbol, SemanticModel semanticModel)
        {
            var name = node is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.IsKind(SyntaxKind.ThisExpression)
                ? memberAccess.Name
                : node as NameSyntax;

            return name != null
                   && semanticModel.GetSymbolInfo(name).Symbol is { } assignedSymbol
                   && declaringSymbol.Equals(assignedSymbol);
        }

        private class RecursionContext<TControlFlowGraph>
        {
            private readonly string messageArg;
            public TControlFlowGraph ControlFlowGraph { get; }
            public ISymbol AnalyzedSymbol { get; }
            public Location IssueLocation { get; }
            public SemanticModel SemanticModel => AnalysisContext.SemanticModel;
            private SyntaxNodeAnalysisContext AnalysisContext { get; }

            public RecursionContext(TControlFlowGraph controlFlowGraph,
                                    ISymbol analyzedSymbol,
                                    Location issueLocation,
                                    SyntaxNodeAnalysisContext analysisContext,
                                    string messageArg)
            {
                this.messageArg = messageArg;

                ControlFlowGraph = controlFlowGraph;
                AnalyzedSymbol = analyzedSymbol;
                IssueLocation = issueLocation;
                AnalysisContext = analysisContext;
            }

            public void ReportIssue() =>
                ReportIssue(IssueLocation);

            public void ReportIssue(Location location) =>
                AnalysisContext.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, location, messageArg));
        }

        private interface IChecker
        {
            void CheckForNoExitProperty(SyntaxNodeAnalysisContext c, PropertyDeclarationSyntax property, IPropertySymbol propertySymbol);
            void CheckForNoExitMethod(SyntaxNodeAnalysisContext c, CSharpSyntaxNode body, SyntaxToken identifier, ISymbol symbol);
        }
    }
}
