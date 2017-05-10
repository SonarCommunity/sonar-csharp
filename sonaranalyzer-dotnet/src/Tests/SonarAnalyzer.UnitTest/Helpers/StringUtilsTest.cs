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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using System.Linq;

namespace SonarAnalyzer.UnitTest.Helpers
{
    [TestClass]
    public class StringUtilsTest
    {
        [TestMethod]
        public void TestSplitCamelCaseToWords()
        {
            AssertSplitEquivalent("thisIsAName", "this", "is", "a", "name");
            AssertSplitEquivalent("ThisIsIt", "this", "is", "it");
            AssertSplitEquivalent("bin2hex", "bin", "hex");
            AssertSplitEquivalent("HTML", "h", "t", "m", "l");
            AssertSplitEquivalent("PEHeader", "p", "e", "header");
            AssertSplitEquivalent("__url_foo", "url", "foo");
            AssertSplitEquivalent("");
            AssertSplitEquivalent(null);
        }

        private void AssertSplitEquivalent(string name, params string[] words)
        {
            CollectionAssert.AreEquivalent(name.SplitCamelCaseToWords().ToList(), words);
        }
    }
}
