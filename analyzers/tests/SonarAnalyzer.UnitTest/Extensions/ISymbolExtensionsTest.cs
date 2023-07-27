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

extern alias vbnet;

using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ISymbolExtensionsCommon = SonarAnalyzer.Helpers.ISymbolExtensions;
using ISymbolExtensionsVB = vbnet::SonarAnalyzer.Extensions.ISymbolExtensions;

namespace SonarAnalyzer.UnitTest.Extensions;

[TestClass]
public class ISymbolExtensionsTest
{
    [TestMethod]
    public void GetDescendantNodes_ForNullSourceTree_ReturnsEmpty_VB() =>
        ISymbolExtensionsVB.GetDescendantNodes(Location.None, SyntaxFactory.ModifiedIdentifier("a")).Should().BeEmpty();

    [TestMethod]
    public void GetDescendantNodes_ForDifferentSyntaxTrees_ReturnsEmpty_VB()
    {
        var first = SyntaxFactory.ParseSyntaxTree("Dim a As String");
        var identifier = first.Single<ModifiedIdentifierSyntax>();

        var second = SyntaxFactory.ParseSyntaxTree("Dim a As String");
        ISymbolExtensionsVB.GetDescendantNodes(identifier.GetLocation(), second.GetRoot()).Should().BeEmpty();
    }

    [TestMethod]
    public void GetDescendantNodes_ForMissingVariableDeclarator_ReturnsEmpty_VB()
    {
        var tree = SyntaxFactory.ParseSyntaxTree(@"new FileSystemAccessRule(""User"", FileSystemRights.ListDirectory, AccessControlType.Allow)");
        ISymbolExtensionsVB.GetDescendantNodes(tree.GetRoot().GetLocation(), tree.GetRoot()).Should().BeEmpty();
    }

    [DataTestMethod]
    [DataRow("{ get; set; }")]
    [DataRow("{ get; }")]
    [DataRow("{ get; } = string.Empty;")]
    [DataRow("{ get; set; } = string.Empty;")]

#if NET

    [DataRow("{ get; init; }")]

#endif

    public void IsAutoProperty_AutoProperty_CS(string getterSetter)
    {
        var code = $$"""
            public class Sample
            {
                public string SymbolMember {{getterSetter}}
            }
            """;
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.CSharp)).Should().BeTrue();
    }

    [TestMethod]
    public void IsAutoProperty_AutoProperty_VB()
    {
        const string code = @"
Public Class Sample

    Public Property SymbolMember As String

End Class";
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.VisualBasic)).Should().BeTrue();
    }

    [TestMethod]
    public void IsAutoProperty_ExplicitProperty_CS()
    {
        const string code = @"
public class Sample
{
    private string _SymbolMember; // Try to confuse the method with auto-like implementation

    public string SymbolMember
    {
        get => _SymbolMember;
        set { _SymbolMember = value; }
    }
}";
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.CSharp)).Should().BeFalse();
    }

    [TestMethod]
    public void IsAutoProperty_ExplicitProperty_VB()
    {
        const string code = @"
Public Class Sample

    Private _SymbolMember As String ' Try to confuse the method with auto-like implementation

    Public Property SymbolMember As String
        Get
            Return _SymbolMember
        End Get
        Set(value As String)
            _SymbolMember = value
        End Set
    End Property

End Class";
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.VisualBasic)).Should().BeFalse();
    }

    [TestMethod]
    public void IsAutoProperty_NonpropertySymbol_CS()
    {
        const string code = @"
public class Sample
{
    public void SymbolMember() { }
}";
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.CSharp)).Should().BeFalse();
    }

    [TestMethod]
    public void IsAutoProperty_NonpropertySymbol_VB()
    {
        const string code = @"
Public Class Sample

    Public Sub SymbolMember()
    End Sub

End Class";
        ISymbolExtensionsCommon.IsAutoProperty(CreateSymbol(code, AnalyzerLanguage.VisualBasic)).Should().BeFalse();
    }

    private static ISymbol CreateSymbol(string snippet, AnalyzerLanguage language)
    {
        var (tree, semanticModel) = TestHelper.Compile(snippet, false, language);
        var node = tree.GetRoot().DescendantNodes().Last(x => x.ToString().Contains(" SymbolMember"));
        return semanticModel.GetDeclaredSymbol(node);
    }
}
