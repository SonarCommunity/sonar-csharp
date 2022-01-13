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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class RedundantInheritanceListTest
    {
        [TestMethod]
        public void RedundantInheritanceList() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\RedundantInheritanceList.cs", new RedundantInheritanceList());

#if NET
        [TestMethod]
        public void RedundantInheritanceList_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\RedundantInheritanceList.CSharp9.cs", new RedundantInheritanceList());

        [TestMethod]
        public void RedundantInheritanceList_CSharp9_CodeFix() =>
            OldVerifier.VerifyCodeFix<RedundantInheritanceListCodeFixProvider>(
                @"TestCases\RedundantInheritanceList.CSharp9.cs",
                @"TestCases\RedundantInheritanceList.CSharp9.Fixed.cs",
                new RedundantInheritanceList(),
                ParseOptionsHelper.FromCSharp9);

        [TestMethod]
        public void RedundantInheritanceList_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\RedundantInheritanceList.CSharp10.cs", new RedundantInheritanceList());

        [TestMethod]
        public void RedundantInheritanceList_CSharp10_CodeFix() =>
            OldVerifier.VerifyCodeFix<RedundantInheritanceListCodeFixProvider>(
                @"TestCases\RedundantInheritanceList.CSharp10.cs",
                @"TestCases\RedundantInheritanceList.CSharp10.Fixed.cs",
                new RedundantInheritanceList(),
                ParseOptionsHelper.FromCSharp10);

#endif

        [TestMethod]
        public void RedundantInheritanceList_CodeFix() =>
            OldVerifier.VerifyCodeFix<RedundantInheritanceListCodeFixProvider>(
                @"TestCases\RedundantInheritanceList.cs",
                @"TestCases\RedundantInheritanceList.Fixed.cs",
                new RedundantInheritanceList());
    }
}
