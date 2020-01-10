/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Helpers.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class UnnecessaryUsings : SonarDiagnosticAnalyzer
    {

        internal const string DiagnosticId = "S1128";
        private const string MessageFormat = "Remove this unnecessary 'using'.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var compilationUnit = (CompilationUnitSyntax)c.Node;
                    var simpleNamespaces = compilationUnit.Usings.Where(usingDirective => usingDirective.Alias == null);
                    var globalUsingDirectives = simpleNamespaces.Select(x => new EquivalentNameSyntax(x.Name)).ToImmutableHashSet();

                    var visitor = new CSharpRemovableUsingWalker(c, globalUsingDirectives, null);
                    VisitContent(visitor, compilationUnit.Members, c.Node.DescendantTrivia());
                    foreach (var attribute in compilationUnit.AttributeLists)
                    {
                        visitor.SafeVisit(attribute);
                    }

                    CheckUnnecessaryUsings(c, simpleNamespaces, visitor.necessaryNamespaces);
                },
                SyntaxKind.CompilationUnit);

        }

        private static void VisitContent(CSharpRemovableUsingWalker visitor, SyntaxList<MemberDeclarationSyntax> members, IEnumerable<SyntaxTrivia> trivias)
        {
            var comments = trivias.Where(trivia => trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia);

            foreach (var member in members)
            {
                visitor.SafeVisit(member);
            }
            foreach (var comment in comments)
            {
                if (comment.HasStructure)
                {
                    visitor.SafeVisit(comment.GetStructure());
                }
            }
        }

        private static void CheckUnnecessaryUsings(SyntaxNodeAnalysisContext context, IEnumerable<UsingDirectiveSyntax> usingDirectives, HashSet<INamespaceSymbol> necessaryNamespaces)
        {
            foreach (var usingDirective in usingDirectives)
            {
                if (context.SemanticModel.GetSymbolInfo(usingDirective.Name).Symbol is INamespaceSymbol namespaceSymbol
                && !necessaryNamespaces.Any(usedNamespace => usedNamespace.IsSameNamespace(namespaceSymbol)))
                {
                    context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, usingDirective.GetLocation()));
                }
            }
        }

        private class CSharpRemovableUsingWalker : CSharpSyntaxWalker
        {
            private readonly SyntaxNodeAnalysisContext context;
            private readonly IImmutableSet<EquivalentNameSyntax> usingDirectivesFromParent;
            private readonly INamespaceSymbol currentNamespace;
            public readonly HashSet<INamespaceSymbol> necessaryNamespaces;
            private bool linqQueryVisited = false;

            public CSharpRemovableUsingWalker(SyntaxNodeAnalysisContext context, IImmutableSet<EquivalentNameSyntax> usingDirectives, INamespaceSymbol currentNamespace)
            {
                this.context = context;
                this.usingDirectivesFromParent = usingDirectives;
                this.necessaryNamespaces = new HashSet<INamespaceSymbol>();
                this.currentNamespace = currentNamespace;
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                var simpleNamespaces = node.Usings.Where(usingDirective => usingDirective.Alias == null);
                var newUsingDirectives = new HashSet<EquivalentNameSyntax>();
                newUsingDirectives.UnionWith(usingDirectivesFromParent);
                newUsingDirectives.UnionWith(simpleNamespaces.Select(x => new EquivalentNameSyntax(x.Name)));

                // We visit the namespace declaration with the updated set of parent 'usings', this is needed in case of nested namespaces
                var visitingNamespace = context.SemanticModel.GetSymbolInfo(node.Name).Symbol as INamespaceSymbol;
                var visitor = new CSharpRemovableUsingWalker(context, newUsingDirectives.ToImmutableHashSet(), visitingNamespace);

                VisitContent(visitor, node.Members, node.DescendantTrivia());

                CheckUnnecessaryUsings(context, simpleNamespaces, visitor.necessaryNamespaces);

                necessaryNamespaces.UnionWith(visitor.necessaryNamespaces);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                VisitNameNode(node);
            }

            public override void VisitGenericName(GenericNameSyntax node)
            {
                VisitNameNode(node);
                base.VisitGenericName(node);
            }

            public override void VisitAwaitExpression(AwaitExpressionSyntax node)
            {
                VisitSymbol(context.SemanticModel.GetAwaitExpressionInfo(node).GetAwaiterMethod);
                base.VisitAwaitExpression(node);
            }

            /// <summary>
            /// LINQ Query Syntax do not use symbols from the 'System.Linq' namespace directly, but the using directive is
            /// still necessary to use the Query Syntax form.
            /// </summary>
            public override void VisitQueryExpression(QueryExpressionSyntax node)
            {
                if (!this.linqQueryVisited && TryGetSystemLinkNamespace(out var systemLinqNamespaceSymbol))
                {
                    this.necessaryNamespaces.Add(systemLinqNamespaceSymbol);
                }
                this.linqQueryVisited = true;
                base.VisitQueryExpression(node);
            }

            private bool TryGetSystemLinkNamespace(out INamespaceSymbol systemLinqNamespace)
            {
                foreach (var usingDirective in this.usingDirectivesFromParent)
                {
                    if (this.context.SemanticModel.GetSymbolInfo(usingDirective.Name).Symbol is INamespaceSymbol namespaceSymbol &&
                        namespaceSymbol.ToDisplayString() == "System.Linq")
                    {
                        systemLinqNamespace = namespaceSymbol;
                        return true;
                    }
                }
                systemLinqNamespace = null;
                return false;
            }

            /// <summary>
            /// We check the symbol of each name node found in the code. If the containing namespace of the symbol is
            /// neither the current namespace or one of its parent, it is then added to the necessary namespace set, as
            /// importing that namespace is indeed necessary.
            /// </summary>
            private void VisitNameNode(SimpleNameSyntax node)
            {
                VisitSymbol(context.SemanticModel.GetSymbolInfo(node).Symbol);
            }

            private void VisitSymbol(ISymbol symbol)
            {
                if (symbol != null &&
                    symbol.ContainingNamespace is INamespaceSymbol namespaceSymbol &&
                    (currentNamespace == null || !namespaceSymbol.IsSameOrAncestorOf(currentNamespace)))
                {
                    necessaryNamespaces.Add(namespaceSymbol);
                }
            }
        }
    }

    internal sealed class EquivalentNameSyntax : IEquatable<EquivalentNameSyntax>
    {
        public NameSyntax Name { get; private set; }

        public EquivalentNameSyntax(NameSyntax name)
        {
            Name = name;
        }

        public override int GetHashCode()
        {
            return Name.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is EquivalentNameSyntax equivalentName
                && Equals(equivalentName);
        }

        public bool Equals(EquivalentNameSyntax other)
        {
            return other != null && CSharpEquivalenceChecker.AreEquivalent(Name, other.Name);
        }
    }
}
