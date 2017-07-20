/*
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.Utilities;
using System.Linq;
using System.Reflection;

namespace SonarAnalyzer.UnitTest.Common
{
    [TestClass]
    public class RuleFinderTest
    {
        [TestMethod]
        public void GetPackagedRuleAssembly()
        {
            RuleFinder.PackagedRuleAssemblies
                .Should().HaveCount(3);
        }

        [TestMethod]
        public void GetParameterlessAnalyzerTypes()
        {
            new RuleFinder().GetParameterlessAnalyzerTypes(AnalyzerLanguage.CSharp)
                .Should().NotBeEmpty();
            new RuleFinder().GetParameterlessAnalyzerTypes(AnalyzerLanguage.VisualBasic)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void GetAnalyzerTypes()
        {
            var analyzers = new RuleFinder().GetAnalyzerTypes(AnalyzerLanguage.CSharp);
            analyzers.Should().Contain(typeof(EmptyStatement).GetTypeInfo());
        }

        [TestMethod]
        public void GetAllAnalyzerTypes()
        {
            var finder = new RuleFinder();

            var countParameterless = finder.GetParameterlessAnalyzerTypes(AnalyzerLanguage.CSharp).Count();
            finder.GetAllAnalyzerTypes().Count()
                .Should().BeGreaterThan(countParameterless);

            countParameterless = finder.GetParameterlessAnalyzerTypes(AnalyzerLanguage.VisualBasic).Count();
            finder.GetAllAnalyzerTypes().Count()
                .Should().BeGreaterThan(countParameterless);
        }
    }
}
