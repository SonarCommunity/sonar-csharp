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

namespace SonarAnalyzer.UnitTest.TestFramework.Tests
{
    [TestClass]
    public class SnippetCompilerTest
    {
        [TestMethod]
        public void AllowsUnsafe_CS()
        {
            const string code = @"
public class Sample
{
    public void Main()
    {
        int i = 0;
        unsafe
        {
            i = 42;
        }
    }
}";
            var sut = new SnippetCompiler(code);
            sut.SyntaxTree.Should().NotBeNull();
            sut.SemanticModel.Should().NotBeNull();
        }

        [TestMethod]
        public void ImportsDefaultNamespaces_VB()
        {
            const string code = @"
' No Using statement for System.Exception
Public Class Sample
    Inherits Exception

End Class";
            var sut = new SnippetCompiler(code, false, AnalyzerLanguage.VisualBasic);
            sut.SyntaxTree.Should().NotBeNull();
            sut.SemanticModel.Should().NotBeNull();
        }

        [TestMethod]
        public void IgnoreErrors_CreatesCompilation()
        {
            const string code = @"
public class Sample
{
// Error CS1519 Invalid token '{' in class, record, struct, or interface member declaration
// Error CS1513 } expected
{";
            var sut = new SnippetCompiler(code, true, AnalyzerLanguage.CSharp);
            sut.SyntaxTree.Should().NotBeNull();
            sut.SemanticModel.Should().NotBeNull();
        }

        [TestMethod]
        public void ValidCode_DoNotIgnoreErrors_CreatesCompilation()
        {
            const string code = @"
public class Sample
{
    public void Main()
    {
    }
}";
            var sut = new SnippetCompiler(code);
            sut.SyntaxTree.Should().NotBeNull();
            sut.SemanticModel.Should().NotBeNull();
        }

        [TestMethod]
        public void CodeWithWarning_DoNotIgnoreErrors_CreatesCompilation()
        {
            const string code = @"
public class Sample
{
    public void Main()
    {
        // Warning CS0219 The variable 'i' is assigned but its value is never used
        int i = 42;
        // Warning CS1030 #warning: 'Show CS1030' on this line
#warning Show CS1030 on this line
    }
}";
            // Severity=Warning is not an error and should not throw
            var sut = new SnippetCompiler(code);
            sut.SyntaxTree.Should().NotBeNull();
            sut.SemanticModel.Should().NotBeNull();
        }

        [TestMethod]
        public void InvalidCode_DoNotIgnoreErrors_Throws()
        {
            const string code = @"
public class Sample
{
// Error CS1519 Invalid token '{' in class, record, struct, or interface member declaration
// Error CS1513 } expected
{";
            using var log = new LogTester();
            Func<SnippetCompiler> f = () => new SnippetCompiler(code);
            f.Should().Throw<InvalidOperationException>();
            log.AssertContain("CS1519 Line: 5: Invalid token '{' in class, record, struct, or interface member declaration");
            log.AssertContain("CS1513 Line: 5: } expected");
        }

        [TestMethod]
        public void GetNamespaceSymbol_NamespaceExists_ReturnsSymbol()
        {
            const string code = @"
namespace Test {
    public class Sample { }
}";
            var sut = new SnippetCompiler(code);
            var namespaceSymbol = sut.GetNamespaceSymbol("Test");
            namespaceSymbol.Name.Should().Be("Test");
        }

        [TestMethod]
        public void GetNamespaceSymbol_NestedNamespaces_ReturnsSymbols()
        {
            const string code = @"
namespace Test {
    namespace Nested {
        public class Sample { }
    }
}";
            var sut = new SnippetCompiler(code);
            var namespaceSymbol = sut.GetNamespaceSymbol("Test");
            namespaceSymbol.Name.Should().Be("Test");
            var nestedNamespaceSymbol = sut.GetNamespaceSymbol("Nested");
            nestedNamespaceSymbol.Name.Should().Be("Nested");
        }

        [TestMethod]
        public void GetNamespaceSymbol_NestedNamespacesWithDot_ReturnsSymbols()
        {
            const string code = @"
namespace Test.Nested {
     public class Sample { }
}";
            var sut = new SnippetCompiler(code);
            // Searching in nested namespaces of the form Namespace.NestedNamespace
            // is not supported by this method. We can get back the symbol of the most nested namespace.
            var namespaceSymbol = sut.GetNamespaceSymbol("Nested");
            namespaceSymbol.Name.Should().Be("Nested");
        }

        [TestMethod]
        public void GetNamespaceSymbol_FileScopedNamespace_ReturnsSymbol()
        {
            const string code = @"
namespace Test;
public class Sample { }
";
            var sut = new SnippetCompiler(code);
            var namespaceSymbol = sut.GetNamespaceSymbol("Test");
            namespaceSymbol.Name.Should().Be("Test");
        }
    }
}
