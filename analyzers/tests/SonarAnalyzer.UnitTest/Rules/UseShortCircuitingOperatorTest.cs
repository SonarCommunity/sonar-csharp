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
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class UseShortCircuitingOperatorTest
    {
        [TestMethod]
        public void UseShortCircuitingOperators_VisualBasic() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\UseShortCircuitingOperator.vb", new VB.UseShortCircuitingOperator());

        [TestMethod]
        public void UseShortCircuitingOperators_VisualBasic_CodeFix() =>
            OldVerifier.VerifyCodeFix<VB.UseShortCircuitingOperatorCodeFix>(
                @"TestCases\UseShortCircuitingOperator.vb",
                @"TestCases\UseShortCircuitingOperator.Fixed.vb",
                new VB.UseShortCircuitingOperator());

        [TestMethod]
        public void UseShortCircuitingOperators_CSharp() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\UseShortCircuitingOperator.cs", new CS.UseShortCircuitingOperator());

#if NET
        [TestMethod]
        public void UseShortCircuitingOperators_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\UseShortCircuitingOperator.CSharp9.cs",
                                                      new CS.UseShortCircuitingOperator());

        [TestMethod]
        public void UseShortCircuitingOperators_CSharp9_CodeFix() =>
            OldVerifier.VerifyCodeFix<CS.UseShortCircuitingOperatorCodeFix>(
                @"TestCases\UseShortCircuitingOperator.CSharp9.cs",
                @"TestCases\UseShortCircuitingOperator.CSharp9.Fixed.cs",
                new CS.UseShortCircuitingOperator(),
                ParseOptionsHelper.FromCSharp9);
#endif

        [TestMethod]
        public void UseShortCircuitingOperators_CSharp_CodeFix() =>
            OldVerifier.VerifyCodeFix<CS.UseShortCircuitingOperatorCodeFix>(
                @"TestCases\UseShortCircuitingOperator.cs",
                @"TestCases\UseShortCircuitingOperator.Fixed.cs",
                new CS.UseShortCircuitingOperator());
    }
}
