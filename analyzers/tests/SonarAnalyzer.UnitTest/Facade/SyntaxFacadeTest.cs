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

using SonarAnalyzer.Helpers.Facade;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace SonarAnalyzer.UnitTest.Helpers
{
    [TestClass]
    public class SyntaxFacadeTest
    {
        private readonly CSharpSyntaxFacade cs = new();
        private readonly VisualBasicSyntaxFacade vb = new();

        [TestMethod]
        public void EnumMembers_Null_CS() =>
            cs.EnumMembers(null).Should().BeEmpty();

        [TestMethod]
        public void EnumMembers_Null_VB() =>
            vb.EnumMembers(null).Should().BeEmpty();

        [TestMethod]
        public void InvocationIdentifier_Null_CS() =>
            cs.InvocationIdentifier(null).Should().BeNull();

        [TestMethod]
        public void InvocationIdentifier_Null_VB() =>
            vb.InvocationIdentifier(null).Should().BeNull();

        [TestMethod]
        public void InvocationIdentifier_UnexpectedTypeThrows_CS() =>
            cs.Invoking(x => x.InvocationIdentifier(CS.SyntaxFactory.IdentifierName("ThisIsNotInvocation"))).Should().Throw<InvalidCastException>();

        [TestMethod]
        public void InvocationIdentifier_UnexpectedTypeThrows_VB() =>
            vb.Invoking(x => x.InvocationIdentifier(VB.SyntaxFactory.IdentifierName("ThisIsNotInvocation"))).Should().Throw<InvalidCastException>();

        [TestMethod]
        public void NodeExpression_Null_CS() =>
            cs.NodeExpression(null).Should().BeNull();

        [TestMethod]
        public void NodeExpression_Null_VB() =>
            vb.NodeExpression(null).Should().BeNull();

        [TestMethod]
        public void NodeExpression_UnexpectedTypeThrows_CS() =>
            cs.Invoking(x => x.NodeExpression(CS.SyntaxFactory.IdentifierName("ThisTypeDoesNotHaveExpression"))).Should().Throw<InvalidOperationException>();

        [TestMethod]
        public void NodeExpression_UnexpectedTypeThrows_VB() =>
            vb.Invoking(x => x.NodeExpression(VB.SyntaxFactory.IdentifierName("ThisTypeDoesNotHaveExpression"))).Should().Throw<InvalidOperationException>();

        [TestMethod]
        public void NodeIdentifier_Null_CS() =>
            cs.NodeIdentifier(null).Should().BeNull();

        [TestMethod]
        public void NodeIdentifier_Null_VB() =>
            vb.NodeIdentifier(null).Should().BeNull();

        [TestMethod]
        public void NodeIdentifier_Unexpected_Returns_Null_CS() =>
           cs.NodeIdentifier(CS.SyntaxFactory.AttributeList()).Should().BeNull();

        [TestMethod]
        public void NodeIdentifier_Unexpected_Returns_Null_VB() =>
            vb.NodeIdentifier(VB.SyntaxFactory.AttributeList()).Should().BeNull();

        [TestMethod]
        public void StringValue_UnexpectedType_CS() =>
             cs.StringValue(CS.SyntaxFactory.ThrowStatement(), null).Should().BeNull();

        [TestMethod]
        public void StringValue_UnexpectedType_VB() =>
            vb.StringValue(VB.SyntaxFactory.ThrowStatement(), null).Should().BeNull();

        [TestMethod]
        public void StringValue_NodeIsNull_CS() =>
            cs.StringValue(null, null).Should().BeNull();

        [TestMethod]
        public void StringValue_NodeIsNull_VB() =>
            vb.StringValue(null, null).Should().BeNull();

        [TestMethod]
        public void RemoveConditionalAccess_Null_CS() =>
            cs.RemoveConditionalAccess(null).Should().BeNull();

        [DataTestMethod]
        [DataRow("M()", "M()")]
        [DataRow("this.M()", "this.M()")]
        [DataRow("A.B.C.M()", "A.B.C.M()")]
        [DataRow("A.B?.C.M()", ".C.M()")]
        [DataRow("A.B?.C?.M()", ".M()")]
        [DataRow("A.B?.C?.D", ".D")]
        public void RemoveConditionalAccess_SimpleInvocation_CS(string invocation, string expected) =>
            cs.RemoveConditionalAccess(CS.SyntaxFactory.ParseExpression(invocation)).ToString().Should().Be(expected);
    }
}
