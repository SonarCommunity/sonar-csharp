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

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Rules;
using FluentAssertions;
using System.Collections.Generic;

namespace SonarAnalyzer.UnitTest.ResourceTests
{
    [TestClass]
    public class GeneratedResourcesTest
    {
        [TestMethod]
        public void AnalyzersHaveCorrespondingResource_CS()
        {
            var rulesFromResources = GetRulesFromResources(@"..\..\..\..\..\rspec\cs");

            var rulesFromClasses = GetRulesFromClasses(typeof(SyntaxHelper).Assembly);

            rulesFromResources.Should().Equal(rulesFromClasses);
        }

        [TestMethod]
        public void AnalyzersHaveCorrespondingResource_VB()
        {
            var rulesFromResources = GetRulesFromResources(@"..\..\..\..\..\rspec\vbnet");

            var rulesFromClasses = GetRulesFromClasses(typeof(SonarAnalyzer.Helpers.VisualBasic.SyntaxHelper).Assembly);

            rulesFromResources.Should().Equal(rulesFromClasses);
        }

        private static string[] GetRulesFromClasses(Assembly assembly)
        {
            return assembly.GetTypes()
                           .Where(typeof(SonarDiagnosticAnalyzer).IsAssignableFrom)
                           .Where(t => !t.IsAbstract)
                           .Where(IsNotUtilityAnalyzer)
                           .SelectMany(GetRuleNamesFromAttributes)
                           .OrderBy(name => name)
                           .ToArray();
        }

        private static bool IsNotUtilityAnalyzer(Type arg)
        {
            return !typeof(UtilityAnalyzerBase).IsAssignableFrom(arg);
        }

        private static string[] GetRulesFromResources(string relativePath)
        {
            var resourcesRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

            var resources = Directory.GetFiles(resourcesRoot);

            var ruleNames = resources.Where(IsHtmlFile)
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .Select(GetRuleFromFileName)
                                     .OrderBy(name => name)
                                     .ToArray();

            return ruleNames;
        }

        private static IEnumerable<string> GetRuleNamesFromAttributes(Type analyzerType)
        {
            return analyzerType.GetCustomAttributes(typeof(RuleAttribute), true)
                                .OfType<RuleAttribute>()
                                .Select(attr => attr.Key);
        }

        private static string GetRuleFromFileName(string fileName)
        {
            // S1234_c# or S1234_vb.net
            var match = Regex.Match(fileName, @"(S\d+)_.*");

            return match.Success ? match.Groups[1].Value : null;
        }

        private static bool IsHtmlFile(string name)
        {
            return Path.GetExtension(name).Equals(".html", StringComparison.OrdinalIgnoreCase);
        }
    }
}