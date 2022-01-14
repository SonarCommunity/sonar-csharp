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

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class GetTypeWithIsAssignableFromTest
    {
        [TestMethod]
        public void GetTypeWithIsAssignableFrom() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\GetTypeWithIsAssignableFrom.cs", new GetTypeWithIsAssignableFrom());

#if NET
        [TestMethod]
        public void GetTypeWithIsAssignableFrom_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\GetTypeWithIsAssignableFrom.CSharp9.cs", new GetTypeWithIsAssignableFrom());

        [TestMethod]
        public void GetTypeWithIsAssignableFrom_CSharp9_CodeFix() =>
            OldVerifier.VerifyCodeFix(@"TestCases\GetTypeWithIsAssignableFrom.CSharp9.cs",
                                   @"TestCases\GetTypeWithIsAssignableFrom.CSharp9.Fixed.cs",
                                   new GetTypeWithIsAssignableFrom(),
                                   new GetTypeWithIsAssignableFromCodeFixProvider(),
                                   ParseOptionsHelper.FromCSharp9,
                                   OutputKind.ConsoleApplication);

        [TestMethod]
        public void GetTypeWithIsAssignableFrom_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Console(@"TestCases\GetTypeWithIsAssignableFrom.CSharp10.cs", new GetTypeWithIsAssignableFrom());

        [TestMethod]
        public void GetTypeWithIsAssignableFrom_CSharp10_CodeFix() =>
            OldVerifier.VerifyCodeFix(@"TestCases\GetTypeWithIsAssignableFrom.CSharp10.cs",
                                   @"TestCases\GetTypeWithIsAssignableFrom.CSharp10.Fixed.cs",
                                   new GetTypeWithIsAssignableFrom(),
                                   new GetTypeWithIsAssignableFromCodeFixProvider(),
                                   ParseOptionsHelper.FromCSharp10,
                                   OutputKind.ConsoleApplication);
#endif

        [TestMethod]
        public void GetTypeWithIsAssignableFrom_CodeFix() =>
            OldVerifier.VerifyCodeFix(@"TestCases\GetTypeWithIsAssignableFrom.cs",
                                   @"TestCases\GetTypeWithIsAssignableFrom.Fixed.cs",
                                   @"TestCases\GetTypeWithIsAssignableFrom.Fixed.Batch.cs",
                                   new GetTypeWithIsAssignableFrom(),
                                   new GetTypeWithIsAssignableFromCodeFixProvider());
    }
}
