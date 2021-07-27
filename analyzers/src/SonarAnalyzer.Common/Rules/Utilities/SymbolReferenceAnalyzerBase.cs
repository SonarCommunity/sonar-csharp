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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        private readonly ISet<SymbolKind> declarationKinds = new HashSet<SymbolKind>
        {
            SymbolKind.Event,
            SymbolKind.Field,
            SymbolKind.Local,
            SymbolKind.Method,
            SymbolKind.NamedType,
            SymbolKind.Parameter,
            SymbolKind.Property,
            SymbolKind.TypeParameter
        };

        protected abstract SyntaxNode GetBindableParent(SyntaxToken token);

        protected sealed override string FileName => "symrefs.pb";

        protected SymbolReferenceAnalyzerBase() : base(DiagnosticId, Title) { }

        protected sealed override SymbolReferenceInfo CreateMessage(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            var allReferences = new List<SymRefInfo>();
            var tokens = syntaxTree.GetRoot().DescendantTokens();
            foreach (var token in tokens)
            {
                if (GetSymRefInfo(token, semanticModel) is { } reference)
                {
                    allReferences.Add(reference);
                }
            }

            var symbolReferenceInfo = new SymbolReferenceInfo { FilePath = syntaxTree.FilePath };
            foreach (var allReference in allReferences.GroupBy(r => r.Symbol))
            {
                if (GetSymbolReference(allReference.ToArray(), syntaxTree) is { } reference)
                {
                    symbolReferenceInfo.Reference.Add(reference);
                }
            }

            return symbolReferenceInfo;
        }

        protected override bool ShouldGenerateMetrics(SyntaxTree tree) =>
            base.ShouldGenerateMetrics(tree)
            && !HasTooManyTokens(tree);

        protected virtual SyntaxToken? GetSetKeyword(ISymbol valuePropertySymbol) => null;

        protected static bool IsValuePropertyParameter(ISymbol symbol) =>
            symbol is IParameterSymbol {IsImplicitlyDeclared: true, Name: "value"};

        private SymbolReferenceInfo.Types.SymbolReference GetSymbolReference(SymRefInfo[] allReference, SyntaxTree tree)
        {
            TextSpan declarationSpan;
            if (allReference.FirstOrDefault(r => r.IsDeclaration) is { } declaration)
            {
                declarationSpan = declaration.IdentifierToken.Span;
            }
            else
            {
                if (allReference.FirstOrDefault() is { } reference && GetSetKeyword(reference.Symbol) is { } setKeyword)
                {
                    declarationSpan = setKeyword.Span;
                }
                else
                {
                    return null;
                }
            }

            var sr = new SymbolReferenceInfo.Types.SymbolReference { Declaration = GetTextRange(Location.Create(tree, declarationSpan).GetLineSpan()) };
            foreach (var reference in allReference.Where(r => !r.IsDeclaration).Select(r => r.IdentifierToken))
            {
                sr.Reference.Add(GetTextRange(Location.Create(tree, reference.Span).GetLineSpan()));
            }
            return sr;
        }

        private SymRefInfo GetSymRefInfo(SyntaxToken token, SemanticModel semanticModel)
        {
            if (!Language.Syntax.IsKind(token, Language.SyntaxKind.IdentifierToken))
            {
                // For the time being, we only handle identifier tokens.
                // We could also handle keywords, such as this, base
                return null;
            }

            if (semanticModel.GetDeclaredSymbol(token.Parent) is { } declaredSymbol)
            {
                return declarationKinds.Contains(declaredSymbol.Kind)
                    ? new SymRefInfo(token, declaredSymbol, true)
                    : null;
            }

            if (GetBindableParent(token) is { } node)
            {
                var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol == null)
                {
                    return null;
                }
                else if (symbol.DeclaringSyntaxReferences.Any() || IsValuePropertyParameter(symbol))
                {
                    return new SymRefInfo(token, symbol);
                }
                else if (symbol is IMethodSymbol ctorSymbol && ctorSymbol.MethodKind == MethodKind.Constructor && ctorSymbol.IsImplicitlyDeclared)
                {
                    return new SymRefInfo(token, ctorSymbol.ContainingType);
                }
            }

            return null;
        }

        private static bool HasTooManyTokens(SyntaxTree syntaxTree) =>
            syntaxTree.GetRoot().DescendantTokens().Count() > TokenCountThreshold;

        private class SymRefInfo
        {
            public SyntaxToken IdentifierToken { get; }
            public ISymbol Symbol { get; }
            public bool IsDeclaration { get; }

            public SymRefInfo(SyntaxToken identifierToken, ISymbol symbol, bool isDeclaration = false)
            {
                IdentifierToken = identifierToken;
                Symbol = symbol;
                IsDeclaration = isDeclaration;
            }
        }
    }
}
