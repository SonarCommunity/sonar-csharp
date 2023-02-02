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

using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules;

[TestClass]
public class RemoveObsoleteCodeTest
{
    private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.RemoveObsoleteCode>();
    private readonly VerifierBuilder builderVB = new VerifierBuilder<VB.RemoveObsoleteCode>();

    [TestMethod]
    public void RemoveObsoleteCode_CS() =>
        builderCS.AddPaths("RemoveObsoleteCode.cs").Verify();

    [TestMethod]
    public void RemoveObsoleteCode_CSharp10() =>
        builderCS.AddPaths("RemoveObsoleteCode.CSharp10.cs")
        .WithTopLevelStatements()
        .WithOptions(ParseOptionsHelper.FromCSharp10)
        .Verify();

    [TestMethod]
    public void RemoveObsoleteCode_VB() =>
        builderVB.AddPaths("RemoveObsoleteCode.vb").Verify();

    [DataTestMethod]
    // All attribute targets of [Obsolete]
    [DataRow("bool field;")]                   // AttributeTargets.Field
    [DataRow("event EventHandler SomeEvent;")] // AttributeTargets.Event
    [DataRow("bool Prop { get; set; }")]       // AttributeTargets.Property
    [DataRow("void Method() { }")]             // AttributeTargets.Method
    [DataRow("class C { }")]                   // AttributeTargets.Class
    [DataRow("struct S { }")]                  // AttributeTargets.Struct
    [DataRow("interface I { }")]               // AttributeTargets.Interface
    [DataRow("enum E { A }")]                  // AttributeTargets.Enum
    [DataRow("public Test() { }")]             // AttributeTargets.Constructor
    [DataRow("delegate void Del();")]          // AttributeTargets.Delegate
    [DataRow("int this[int i] => 1;")]         // Indexer
    public void RemoveObsoleteCode_AttributeTargetTest_CS(string attributeTargetDeclaration)
    {
        builderCS.AddSnippet(WrapInTestCode(string.Empty)).VerifyNoIssueReported();
        builderCS.AddSnippet(WrapInTestCode("[Obsolete] // Noncompliant")).Verify();
        builderCS.AddSnippet(WrapInTestCode("[Custom]")).VerifyNoIssueReported();
        builderCS.AddSnippet(WrapInTestCode("""
            [Obsolete] // Noncompliant
            [Custom]
            """)).Verify();

        string WrapInTestCode(string attribute) =>
            $$"""
            using System;

            [AttributeUsage(AttributeTargets.All)]
            public sealed class CustomAttribute: Attribute
            {
            }

            public class Test
            {
                {{attribute}}
                {{attributeTargetDeclaration}}
            }
            """;
    }

    [DataTestMethod]
    // All attribute targets of [Obsolete]
    [DataRow("Private field As Boolean")]        // AttributeTargets.Field
    [DataRow("Event SomeEvent As EventHandler")] // AttributeTargets.Event
    [DataRow("Property Prop As Boolean")]        // AttributeTargets.Property
    [DataRow("""
            Private Sub Method()
            End Sub
        """)]                                    // AttributeTargets.Method
    [DataRow("""
            Class C
            End Class
        """)]                                    // AttributeTargets.Class
    [DataRow("""
            Structure S
            End Structure
        """)]                                    // AttributeTargets.Struct
    [DataRow("""
            Interface I
            End Interface
        """)]                                    // AttributeTargets.Interface
    [DataRow("""
            Enum E
                A
            End Enum
        """)]                                    // AttributeTargets.Enum
    [DataRow("""
            Public Sub New()
            End Sub
        """)]                                    // AttributeTargets.Constructor
    [DataRow("Delegate Sub Del()")]              // AttributeTargets.Delegate
    [DataRow("""
            Default ReadOnly Property Item(ByVal i As Integer) As Integer
                Get
                    Return 1
                End Get
            End Property
        """)]                                    // Indexer
    public void RemoveObsoleteCode_AttributeTargetTest_VB(string attributeTargetDeclaration)
    {
        builderVB.AddSnippet(WrapInTestCode(string.Empty)).VerifyNoIssueReported();
        builderVB.AddSnippet(WrapInTestCode("<Obsolete> ' Noncompliant")).Verify();
        builderVB.AddSnippet(WrapInTestCode("<Custom>")).VerifyNoIssueReported();
        builderVB.AddSnippet(WrapInTestCode("""
            <Obsolete> ' Noncompliant
            <Custom>
            """)).Verify();

        string WrapInTestCode(string attribute) =>
            $$"""
            Imports System

            <AttributeUsage(AttributeTargets.All)>
            Public NotInheritable Class CustomAttribute
                Inherits Attribute
            End Class

            Public Class Test
                {{attribute}}
                {{attributeTargetDeclaration}}
            End Class
            """;
    }
}
