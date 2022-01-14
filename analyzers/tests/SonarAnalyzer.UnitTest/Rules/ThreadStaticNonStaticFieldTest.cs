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
    public class ThreadStaticNonStaticFieldTest
    {
        [TestMethod]
        public void ThreadStaticNonStaticField() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ThreadStaticNonStaticField.cs", new ThreadStaticNonStaticField());

#if NET
        [TestMethod]
        public void ThreadStaticNonStaticField_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\ThreadStaticNonStaticField.CSharp9.cs", new ThreadStaticNonStaticField());

        [TestMethod]
        public void ThreadStaticNonStaticField_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\ThreadStaticNonStaticField.CSharp10.cs", new ThreadStaticNonStaticField());

        [TestMethod]
        public void ThreadStaticNonStaticField_CodeFix_CSharp10() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\ThreadStaticNonStaticField.CSharp10.cs",
                @"TestCases\ThreadStaticNonStaticField.CSharp10.Fixed.cs",
                new ThreadStaticNonStaticField(),
                new ThreadStaticNonStaticFieldCodeFixProvider());
#endif

        [TestMethod]
        public void ThreadStaticNonStaticField_CodeFix() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\ThreadStaticNonStaticField.cs",
                @"TestCases\ThreadStaticNonStaticField.Fixed.cs",
                new ThreadStaticNonStaticField(),
                new ThreadStaticNonStaticFieldCodeFixProvider());
    }
}
