﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class UnaryPrefixOperatorRepeatedTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void UnaryPrefixOperatorRepeated() =>
            Verifier.VerifyConcurrentAnalyzer(@"TestCases\UnaryPrefixOperatorRepeated.cs", new CS.UnaryPrefixOperatorRepeated());

#if NET
        [TestMethod]
        [TestCategory("Rule")]
        public void UnaryPrefixOperatorRepeated_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\UnaryPrefixOperatorRepeated.CSharp9.cs", new CS.UnaryPrefixOperatorRepeated());
#endif

        [TestMethod]
        [TestCategory("CodeFix")]
        public void UnaryPrefixOperatorRepeated_CodeFix() =>
            Verifier.VerifyCodeFix(
                @"TestCases\UnaryPrefixOperatorRepeated.cs",
                @"TestCases\UnaryPrefixOperatorRepeated.Fixed.cs",
                new CS.UnaryPrefixOperatorRepeated(),
                new CS.UnaryPrefixOperatorRepeatedCodeFixProvider());

        [TestMethod]
        [TestCategory("Rule")]
        public void UnaryPrefixOperatorRepeated_VB() =>
            Verifier.VerifyConcurrentAnalyzer(@"TestCases\UnaryPrefixOperatorRepeated.vb", new VB.UnaryPrefixOperatorRepeated());
    }
}
