﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using System.Linq;

namespace SonarAnalyzer.UnitTest.Helpers
{
    [TestClass]
    public class NavigationHelperTest
    {
        private const string Source = @"
namespace Test
{
    class TestClass
    {
        public void DoSomething(){}
        public void IfMethod()
        {
            if (true)
                DoSomething();
            else if (true)
                DoSomething();
            else
                DoSomething();
        }

        public void SwitchMethod()
        {
            var i = 5;
            switch(i)
            {
                case 3:
                    DoSomething();
                    break;
                case 5:
                    DoSomething();
                    break;
                default:
                    DoSomething();
                    break;
            }
        }
    }
}";

        private MethodDeclarationSyntax ifMethod;
        private MethodDeclarationSyntax switchMethod;

        [TestInitialize]
        public void TestSetup()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(Source);

            ifMethod = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.ValueText == "IfMethod");
            switchMethod = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.ValueText == "SwitchMethod");
        }

        [TestMethod]
        public void GetPrecedingIfsInConditionChain()
        {
            var ifStatement1 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().First();
            ifStatement1.GetPrecedingIfsInConditionChain()
                .Should().BeEmpty();

            var ifStatement2 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().Last();
            var preceding = ifStatement2.GetPrecedingIfsInConditionChain();
            preceding.Should().HaveCount(1);

            ifStatement1.ShouldBeEquivalentTo(preceding[0]);
        }

        [TestMethod]
        public void GetPrecedingStatementsInConditionChain()
        {
            var ifStatement1 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().First();
            ifStatement1.GetPrecedingStatementsInConditionChain()
                .Should().BeEmpty();

            var ifStatement2 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().Last();
            var preceding = ifStatement2.GetPrecedingStatementsInConditionChain().ToList();
            preceding.Should().HaveCount(1);

            ifStatement1.Statement.ShouldBeEquivalentTo(preceding[0]);
        }

        [TestMethod]
        public void GetPrecedingConditionsInConditionChain()
        {
            var ifStatement1 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().First();
            ifStatement1.GetPrecedingConditionsInConditionChain()
                .Should().BeEmpty();

            var ifStatement2 = ifMethod.DescendantNodes().OfType<IfStatementSyntax>().Last();
            var preceding = ifStatement2.GetPrecedingConditionsInConditionChain().ToList();
            preceding.Should().HaveCount(1);

            ifStatement1.Condition.ShouldBeEquivalentTo(preceding[0]);
        }

        [TestMethod]
        public void GetPrecedingSections()
        {
            var sections = switchMethod.DescendantNodes().OfType<SwitchSectionSyntax>().ToList();

            sections.Last().GetPrecedingSections().Should().HaveCount(2);
            sections.First().GetPrecedingSections().Should().BeEmpty();
            sections.Last().GetPrecedingSections().First().ShouldBeEquivalentTo(sections.First());
        }

        [TestMethod]
        public void GetPrecedingSections_Empty()
        {
            var sections = ifMethod.DescendantNodes().OfType<SwitchSectionSyntax>().ToList();

            sections.FirstOrDefault().GetPrecedingSections().Should().BeEmpty();
        }

        [TestMethod]
        public void GetPrecedingStatement()
        {
            var statements = switchMethod.Body.Statements.ToList();

            statements[1].GetPrecedingStatement().ShouldBeEquivalentTo(statements[0]);

            statements[0].GetPrecedingStatement().ShouldBeEquivalentTo(null);
        }
    }
}
