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
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class SwitchCaseFallsThroughToDefaultTest
    {
        [TestMethod]
        public void SwitchCaseFallsThroughToDefault() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\SwitchCaseFallsThroughToDefault.cs", new SwitchCaseFallsThroughToDefault());

#if NET
        [TestMethod]
        public void SwitchCaseFallsThroughToDefault_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\SwitchCaseFallsThroughToDefault.CSharp9.cs", new SwitchCaseFallsThroughToDefault());
#endif

        [TestMethod]
        public void SwitchCaseFallsThroughToDefault_CodeFix() =>
            OldVerifier.VerifyCodeFix(@"TestCases\SwitchCaseFallsThroughToDefault.cs",
                                   @"TestCases\SwitchCaseFallsThroughToDefault.Fixed.cs",
                                   new SwitchCaseFallsThroughToDefault(),
                                   new SwitchCaseFallsThroughToDefaultCodeFixProvider());
    }
}
