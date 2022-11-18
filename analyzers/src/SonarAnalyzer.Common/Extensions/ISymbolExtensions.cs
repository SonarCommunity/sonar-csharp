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

namespace SonarAnalyzer.Helpers
{
    internal static class ISymbolExtensions
    {
        public static bool HasAttribute(this ISymbol symbol, KnownType type) =>
            symbol.GetAttributes(type).Any();

        public static SyntaxNode GetFirstSyntaxRef(this ISymbol symbol) =>
            symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

        public static bool IsAutoProperty(this ISymbol symbol) =>
            symbol.Kind == SymbolKind.Property && symbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(x => symbol.Equals(x.AssociatedSymbol));

        public static bool IsTopLevelMain(this ISymbol symbol) =>
            symbol is IMethodSymbol { Name: TopLevelStatements.MainMethodImplicitName };

        public static bool IsGlobalNamespace(this ISymbol symbol) =>
            symbol is INamespaceSymbol { Name: "" };
    }
}
