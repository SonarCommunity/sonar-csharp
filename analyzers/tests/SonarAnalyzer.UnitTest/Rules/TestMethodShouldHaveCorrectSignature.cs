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

#if NET
using System.Linq;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class TestMethodShouldHaveCorrectSignatureTest
    {
        [DataTestMethod]
        [DataRow("1.1.11")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldHaveCorrectSignature_MsTest(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldHaveCorrectSignature.MsTest.cs",
                                    new TestMethodShouldHaveCorrectSignature(),
                                    NuGetMetadataReference.MSTestTestFramework(testFwkVersion));

        [DataTestMethod]
        [DataRow("2.5.7.10213")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldHaveCorrectSignature_NUnit(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldHaveCorrectSignature.NUnit.cs",
                                    new TestMethodShouldHaveCorrectSignature(),
                                    NuGetMetadataReference.NUnit(testFwkVersion));

        [DataTestMethod]
        [DataRow("2.0.0")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldHaveCorrectSignature_Xunit(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldHaveCorrectSignature.Xunit.cs",
                                    new TestMethodShouldHaveCorrectSignature(),
                                    NuGetMetadataReference.XunitFramework(testFwkVersion));

        [TestMethod]
        public void TestMethodShouldHaveCorrectSignature_Xunit_Legacy() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldHaveCorrectSignature.Xunit.Legacy.cs",
                                    new TestMethodShouldHaveCorrectSignature(),
                                    NuGetMetadataReference.XunitFrameworkV1);

        [TestMethod]
        public void TestMethodShouldHaveCorrectSignature_MSTest_Miscellaneous() =>
            // Additional test cases e.g. partial classes, and methods with multiple faults.
            // We have to specify a test framework for the tests, but it doesn't really matter which
            // one, so we're using MSTest and only testing a single version.
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldHaveCorrectSignature.Misc.cs",
                                    new TestMethodShouldHaveCorrectSignature(),
                                    NuGetMetadataReference.MSTestTestFrameworkV1);

#if NET
        [TestMethod]
        public void TestMethodShouldHaveCorrectSignature_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\TestMethodShouldHaveCorrectSignature.CSharp9.cs",
                                                new TestMethodShouldHaveCorrectSignature(),
                                                NuGetMetadataReference.MSTestTestFrameworkV1
                                                    .Concat(NuGetMetadataReference.XunitFramework(Constants.NuGetLatestVersion))
                                                    .Concat(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion))
                                                    .ToArray());
#endif
    }
}
