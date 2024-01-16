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

using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Extensions;

namespace SonarAnalyzer.UnitTest.Extensions;

[TestClass]
public class INamespaceSymbolExtensionsTest
{
    [DataTestMethod]
    [DataRow("System", "System")]
    [DataRow("System.Collections.Generic", "System.Collections.Generic")]
    // Odd cases but nothing that needs a fix:
    [DataRow("System.Collections.Generic", "System..Collections..Generic")]
    [DataRow("System.Collections.Generic", ".System.Collections.Generic.")]
    public void Is_ValidNameSpaces(string code, string test)
    {
        var snippet = $$"""
            using {{code}};
            """;
        var (tree, model) = TestHelper.CompileCS(snippet);
        var name = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single().Name;
        var symbol = model.GetSymbolInfo(name).Symbol;
        var ns = symbol.Should().BeAssignableTo<INamespaceSymbol>().Subject;
        ns.Is(test).Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow("", true)]
    [DataRow("System", false)]
    public void Is_Global(string test, bool expected)
    {
        var snippet = """
            using System;
            """;
        var (tree, model) = TestHelper.CompileCS(snippet);
        var name = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single().Name;
        var symbol = model.GetSymbolInfo(name).Symbol;
        var ns = symbol.Should().BeAssignableTo<INamespaceSymbol>().Subject;
        var globalNs = ns.ContainingNamespace;
        globalNs.IsGlobalNamespace.Should().BeTrue();
        globalNs.Is(test).Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("System", "Microsoft")]
    [DataRow("System", "System.Collections")]
    [DataRow("System.Collections", "System")]
    [DataRow("System.Collections", "Collections")]
    [DataRow("System.Collections", "System.Collections.Generic")]
    [DataRow("System.Collections", "Collections.Generic")]
    [DataRow("System.Collections.Generic", "System.Collections")]
    [DataRow("System.Collections.Generic", "Generic")]
    [DataRow("System.Collections.Generic", "")]
    [DataRow("System.Collections.Generic", "global::System.Collections.Generic")]
    [DataRow("System.Collections.Generic", "global.System.Collections.Generic")]
    public void Is_InvalidNameSpaces(string code, string test)
    {
        var snippet = $$"""
            using {{code}};
            """;
        var (tree, model) = TestHelper.CompileCS(snippet);
        var name = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single().Name;
        var symbol = model.GetSymbolInfo(name).Symbol;
        var ns = symbol.Should().BeAssignableTo<INamespaceSymbol>().Subject;
        ns.Is(test).Should().BeFalse();
    }

    [TestMethod]
    public void Is_ThrowsArgumentNullExceptionForName()
    {
        var snippet = """
            using System;
            """;
        var (tree, model) = TestHelper.CompileCS(snippet);
        var name = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single().Name;
        var symbol = model.GetSymbolInfo(name).Symbol;
        var ns = symbol.Should().BeAssignableTo<INamespaceSymbol>().Subject;
        var action = () => ns.Is(null);
        action.Should().Throw<ArgumentNullException>().WithMessage("*name*");
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("System")]
    [DataRow("System.Collection")]
    public void Is_ReturnsFalseForNullSymbol(string nameSpace)
    {
        INamespaceSymbol ns = null;
        ns.Is(nameSpace).Should().BeFalse();
    }
}
