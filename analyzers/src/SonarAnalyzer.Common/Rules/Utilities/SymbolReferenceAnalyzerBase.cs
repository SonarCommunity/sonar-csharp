﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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

using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Protobuf;

namespace SonarAnalyzer.Rules
{
    public abstract class SymbolReferenceAnalyzerBase<TSyntaxKind> : UtilityAnalyzerBase<TSyntaxKind, SymbolReferenceInfo>
        where TSyntaxKind : struct
    {
        private const string DiagnosticId = "S9999-symbolRef";
        private const string Title = "Symbol reference calculator";
        private const int TokenCountThreshold = 40_000;

        protected sealed override string FileName => "symrefs.pb";

        protected abstract SyntaxNode GetBindableParent(SyntaxToken token);

        protected abstract ReferenceInfo[] CreateDeclarationReferenceInfo(SyntaxNode node, SemanticModel model);

        protected abstract IList<SyntaxNode> GetDeclarations(SyntaxNode node);

        protected abstract string GetMappedFilePath(SyntaxNode root);

        protected SymbolReferenceAnalyzerBase() : base(DiagnosticId, Title) { }

        protected sealed override SymbolReferenceInfo CreateMessage(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            // If the syntax tree is constructed for a razor generated file, we need to provide the original file path.
            var filePath = syntaxTree.FilePath;
            if (GeneratedCodeRecognizer.IsRazorGeneratedFile(syntaxTree) && syntaxTree.GetRoot() is var root && root.ContainsDirectives)
            {
                filePath = GetMappedFilePath(root);
            }

            var symbolReferenceInfo = new SymbolReferenceInfo { FilePath = filePath };
            var references = GetReferences(syntaxTree.GetRoot(), semanticModel);

            foreach (var symbol in references.Keys)
            {
                if (GetSymbolReference(references[symbol], syntaxTree) is { } reference)
                {
                    symbolReferenceInfo.Reference.Add(reference);
                }
            }

            return symbolReferenceInfo;
        }

        protected override bool ShouldGenerateMetrics(SyntaxTree tree) =>
            base.ShouldGenerateMetrics(tree)
            && !HasTooManyTokens(tree);

        private Dictionary<ISymbol, List<ReferenceInfo>> GetReferences(SyntaxNode root, SemanticModel model)
        {
            var references = new Dictionary<ISymbol, List<ReferenceInfo>>();
            var knownIdentifiers = new HashSet<string>(Language.NameComparer);
            var knownNodes = new List<SyntaxNode>();
            var declarations = GetDeclarations(root);

            for (var i = 0; i < declarations.Count; i++)
            {
                var declarationReferences = CreateDeclarationReferenceInfo(declarations[i], model);
                if (declarationReferences == null)
                {
                    continue;
                }

                for (var j = 0; j < declarationReferences.Length; j++)
                {
                    var currentDeclaration = declarationReferences[j];
                    if (currentDeclaration.Symbol != null)
                    {
                        references.GetOrAdd(currentDeclaration.Symbol, _ => new List<ReferenceInfo>()).Add(currentDeclaration);
                        knownNodes.Add(currentDeclaration.Node);
                        knownIdentifiers.Add(currentDeclaration.Identifier.ValueText);
                    }
                }
            }

            foreach (var token in root.DescendantTokens())
            {
                if (Language.Syntax.IsKind(token, Language.SyntaxKind.IdentifierToken)
                    && knownIdentifiers.Contains(token.Text)
                    && GetBindableParent(token) is { } parent
                    && !knownNodes.Contains(parent)
                    && GetReferenceSymbol(parent, model) is { } symbol
                    && references.ContainsKey(symbol)
                    && references[symbol] is { } symbolRefs)
                {
                    symbolRefs.Add(new ReferenceInfo(parent, token, symbol, false));
                }
            }

            return references;
        }

        private static ISymbol GetReferenceSymbol(SyntaxNode node, SemanticModel model) =>
            model.GetSymbolInfo(node).Symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor, IsImplicitlyDeclared: true } constructor => constructor.ContainingType,
                var symbol => symbol
            };

        private static SymbolReferenceInfo.Types.SymbolReference GetSymbolReference(List<ReferenceInfo> references, SyntaxTree tree)
        {
            var declarationSpan = GetDeclarationSpan(references);
            if (!declarationSpan.HasValue)
            {
                return null;
            }

            var symbolReference = new SymbolReferenceInfo.Types.SymbolReference { Declaration = GetTextRange(declarationSpan.Value) };
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                if (!reference.IsDeclaration)
                {
                    if (reference.Identifier.GetLocation().TryEnsureMappedLocation(out var mappedLocation))
                    {
                        symbolReference.Reference.Add(GetTextRange(mappedLocation.GetLineSpan()));
                    }
                }
            }
            return symbolReference;
        }

        private static FileLinePositionSpan? GetDeclarationSpan(List<ReferenceInfo> references)
        {
            for (var i = 0; i < references.Count; i++)
            {
                if (references[i].IsDeclaration)
                {
                    if (GeneratedCodeRecognizer.IsRazorGeneratedFile(references[i].Identifier.SyntaxTree))
                    {
                        if (references[i].Identifier.GetLocation().TryEnsureMappedLocation(out var mappedLocation))
                        {
                            return mappedLocation.GetLineSpan();
                        }
                    }
                    else
                    {
                        return references[i].Identifier.GetLocation().GetLineSpan();
                    }
                }
            }
            return null;
        }

        private static bool HasTooManyTokens(SyntaxTree syntaxTree) =>
            syntaxTree.GetRoot().DescendantTokens().Count() > TokenCountThreshold;

        protected sealed record ReferenceInfo(SyntaxNode Node, SyntaxToken Identifier, ISymbol Symbol, bool IsDeclaration);
    }
}
