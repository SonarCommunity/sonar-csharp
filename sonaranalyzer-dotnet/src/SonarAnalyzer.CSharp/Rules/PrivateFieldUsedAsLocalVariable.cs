﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class PrivateFieldUsedAsLocalVariable : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1450";
        private const string MessageFormat = "Remove the '{0}' field and declare it as a local variable in the relevant methods.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var classNode = c.Node as TypeDeclarationSyntax;
                    if (classNode != null && classNode.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        // Not supported
                        return;
                    }

                    var privateFields = GetPrivateFields(classNode, c.SemanticModel);
                    var referencesByEnclosingSymbol = GetReferencesByEnclosingSymbol(classNode, privateFields, c.SemanticModel);

                    var classSymbol = c.SemanticModel.GetDeclaredSymbol(classNode);
                    if (classSymbol == null)
                    {
                        return;
                    }

                    var classMethods = classSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Method).ToHashSet();

                    ExcludePrivateFieldsBasedOnReferences(privateFields, referencesByEnclosingSymbol, classMethods);
                    ExcludePrivateFieldsBasedOnCompilerErrors(privateFields, referencesByEnclosingSymbol, classMethods,
                        classNode, c.SemanticModel.Compilation);

                    foreach (var privateField in privateFields.Values.Where(f => !f.Excluded))
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(rule, privateField.Syntax.GetLocation(),
                            privateField.Symbol.Name));
                    }
                },
                SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static IImmutableDictionary<ISymbol, PrivateField> GetPrivateFields(TypeDeclarationSyntax classNode,
            SemanticModel semanticModel)
        {
            return classNode.Members
                        .Where(m => m.IsKind(SyntaxKind.FieldDeclaration))
                        .Cast<FieldDeclarationSyntax>()
                        .Where(f => !f.AttributeLists.Any())
                        .SelectMany(f => f.Declaration.Variables.Select(
                            v => new PrivateField(v, semanticModel.GetDeclaredSymbol(v))))
                        .Where(f => f.Symbol != null && f.Symbol.DeclaredAccessibility == Accessibility.Private)
                        .ToImmutableDictionary(
                            f => f.Symbol,
                            f => f);
        }

        private static IDictionary<ISymbol, IDictionary<SyntaxNode, ISymbol>> GetReferencesByEnclosingSymbol(
            SyntaxNode node,
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            SemanticModel semanticModel)
        {
            var privateFieldNames = privateFields.Keys.Select(s => s.Name).ToHashSet();

            var potentialReferences = node.DescendantNodes()
                .Where(n => n.IsKind(SyntaxKind.IdentifierName))
                .Cast<IdentifierNameSyntax>()
                .Where(id => privateFieldNames.Contains(id.Identifier.ValueText));

            var builder = new Dictionary<ISymbol, IDictionary<SyntaxNode, ISymbol>>();
            foreach (var potentialReference in potentialReferences)
            {
                var referencedSymbol = semanticModel.GetSymbolInfo(potentialReference).Symbol;
                if (referencedSymbol == null || !privateFields.ContainsKey(referencedSymbol))
                {
                    continue;
                }

                SyntaxNode referenceSyntax = potentialReference;
                while (referenceSyntax.Parent != null &&
                       referencedSymbol.Equals(semanticModel.GetSymbolInfo(referenceSyntax.Parent).Symbol))
                {
                    referenceSyntax = referenceSyntax.Parent;
                }

                if (referenceSyntax.Parent != null &&
                    referenceSyntax.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
                {
                    referenceSyntax = referenceSyntax.Parent;
                }


                var enclosingSymbol = semanticModel.GetEnclosingSymbol(potentialReference.SpanStart);
                if (!builder.ContainsKey(enclosingSymbol))
                {
                    builder.Add(enclosingSymbol, new Dictionary<SyntaxNode, ISymbol>());
                }

                builder[enclosingSymbol].Add(referenceSyntax, referencedSymbol);
            }

            return builder;
        }

        private static void ExcludePrivateFieldsBasedOnReferences(
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            IDictionary<ISymbol, IDictionary<SyntaxNode, ISymbol>> referencesByEnclosingSymbol,
            ISet<ISymbol> classMethods)
        {
            var referencedAtLeastOnceFromClassMethod = new HashSet<ISymbol>();

            foreach (var references in referencesByEnclosingSymbol)
            {
                if (!classMethods.Contains(references.Key))
                {
                    foreach (var reference in references.Value)
                    {
                        privateFields[reference.Value].Excluded = true;
                    }

                    continue;
                }

                foreach (var reference in references.Value)
                {
                    referencedAtLeastOnceFromClassMethod.Add(reference.Value);

                    if (!IsReferenceToSingleFieldValue(reference))
                    {
                        privateFields[reference.Value].Excluded = true;
                    }
                }
            }

            foreach (var privateField in privateFields.Values)
            {
                if (!referencedAtLeastOnceFromClassMethod.Contains(privateField.Symbol))
                {
                    privateField.Excluded = true;
                }
            }
        }

        private static bool IsReferenceToSingleFieldValue(KeyValuePair<SyntaxNode, ISymbol> reference)
        {
            if (reference.Key.IsKind(SyntaxKind.IdentifierName) || reference.Value.IsStatic)
            {
                return true;
            }

            if (reference.Key is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                return true;
            }

            if (reference.Key is ConditionalAccessExpressionSyntax conditionalAccess && conditionalAccess.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                return true;
            }

            return false;
        }

        private static void ExcludePrivateFieldsBasedOnCompilerErrors(
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            IDictionary<ISymbol, IDictionary<SyntaxNode, ISymbol>> referencesByEnclosingSymbol,
            IEnumerable<ISymbol> classMethods,
            TypeDeclarationSyntax classNode,
            Compilation compilation)
        {
            var replacements = new Dictionary<SyntaxNode, BlockSyntax>();
            foreach (var classMethod in classMethods.Where(m => referencesByEnclosingSymbol.ContainsKey(m)))
            {
                var references = referencesByEnclosingSymbol[classMethod].Where(r => !privateFields[r.Value].Excluded)
                    .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
                if (TryRewriteMethodBody(privateFields, references, classMethod, out var body, out var newBody))
                {
                    replacements.Add(body, newBody);
                }
            }

            if (!replacements.Any())
            {
                return;
            }

            var newSyntaxRoot = classNode.SyntaxTree.GetRoot().ReplaceSyntax(
                replacements.Keys,
                (original, partiallyRewritten) => replacements[original],
                null,
                null,
                null,
                null);
            var newSyntaxTree = classNode.SyntaxTree.WithRootAndOptions(newSyntaxRoot,
                classNode.SyntaxTree.Options);
            var newCompilation = CSharpCompilation.Create(nameof(PrivateFieldUsedAsLocalVariable),
                new[] { newSyntaxTree }, compilation.References, compilation.Options as CSharpCompilationOptions);

            var diagnostics = newCompilation.GetDiagnostics();
            foreach (var privateField in privateFields.Values.Where(f => !f.Excluded))
            {
                if (diagnostics.Any(d => d.Id == WellKnownDiagnosticIds.ERR_UseDefViolation
                                         && d.GetMessage().Contains(privateField.UniqueName)))
                {
                    privateField.Excluded = true;
                }
            }
        }

        private static bool TryRewriteMethodBody(
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            IImmutableDictionary<SyntaxNode, ISymbol> references,
            ISymbol classMethod,
            out BlockSyntax body,
            out BlockSyntax newBody)
        {
            if (!references.Any())
            {
                body = null;
                newBody = null;

                return false;
            }

            if (!TryGetMemberBody(classMethod, out body))
            {
                // We don't know how the field is being used within this method
                foreach (var reference in references)
                {
                    privateFields[reference.Value].Excluded = true;
                }

                newBody = null;

                return false;
            }

            newBody = RewriteBody(privateFields, body, references, out var rewrittenNodes);

            return !ExcludePrivateFieldsBasedOnRewrittenNodes(privateFields, references, rewrittenNodes);
        }

        private static bool TryGetMemberBody(ISymbol memberSymbol, out BlockSyntax body)
        {
            body = null;

            var syntax = memberSymbol.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax();
            if (syntax != null)
            {
                if (syntax is BaseMethodDeclarationSyntax methodSyntax)
                {
                    body = methodSyntax.Body;
                }

                if (syntax is AccessorDeclarationSyntax accessorSyntax)
                {
                    body = accessorSyntax.Body;
                }
            }

            return body != null;
        }

        private static BlockSyntax RewriteBody(
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            BlockSyntax body,
            IImmutableDictionary<SyntaxNode, ISymbol> references,
            out ISet<SyntaxNode> rewrittenNodes)
        {
            var symbolsToRewrite = references.Values.ToHashSet();

            var localDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.IntKeyword)).WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.SeparatedList(symbolsToRewrite.Select(
                        s => SyntaxFactory.VariableDeclarator(privateFields[s].UniqueName)))));

            var rewrittenNodesBuilder = new HashSet<SyntaxNode>();
            var newBody = body.ReplaceSyntax(
                references.Keys,
                (original, partiallyRewritten) =>
                {
                    rewrittenNodesBuilder.Add(original);
                    return SyntaxFactory.IdentifierName(privateFields[references[original]].UniqueName);
                },
                null,
                null,
                null,
                null);

            var newStatements = newBody.Statements.Insert(0, localDeclaration);
            newBody = newBody.WithStatements(newStatements);

            rewrittenNodes = rewrittenNodesBuilder;

            return newBody;
        }

        private static bool ExcludePrivateFieldsBasedOnRewrittenNodes(
            IImmutableDictionary<ISymbol, PrivateField> privateFields,
            IImmutableDictionary<SyntaxNode, ISymbol> references,
            ISet<SyntaxNode> rewrittenNodes)
        {
            var partiallyRewrittenSymbols = references
                .Where(r => !rewrittenNodes.Contains(r.Key))
                .Select(r => r.Value)
                .ToHashSet();

            foreach (var partiallyRewrittenSymbol in partiallyRewrittenSymbols)
            {
                privateFields[partiallyRewrittenSymbol].Excluded = true;
            }

            var allSymbolsToRewrite = references.Values.ToHashSet();

            return partiallyRewrittenSymbols.Count == allSymbolsToRewrite.Count;
        }

        private class PrivateField
        {
            public PrivateField(VariableDeclaratorSyntax syntax, ISymbol symbol)
            {
                Syntax = syntax;
                Symbol = symbol;
                UniqueName = nameof(PrivateFieldUsedAsLocalVariable) + Guid.NewGuid().ToString("N");
                Excluded = false;
            }

            public VariableDeclaratorSyntax Syntax { get; private set; }
            public ISymbol Symbol { get; private set; }
            public string UniqueName { get; private set; }
            public bool Excluded { get; set; }
        }
    }
}
