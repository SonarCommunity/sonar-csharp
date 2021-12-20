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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class TestMethodShouldNotBeIgnoredTest
    {
        [TestMethod]
        public void TestMethodShouldNotBeIgnored_MsTest_Legacy() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.MsTest.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    CompilationErrorBehavior.Ignore,    // IgnoreAttribute doesn't contain any reason param
                                    NuGetMetadataReference.MSTestTestFramework("1.1.11"));

        [DataTestMethod]
        [DataRow("1.2.0")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldNotBeIgnored_MsTest(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.MsTest.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    NuGetMetadataReference.MSTestTestFramework(testFwkVersion));

        [DataTestMethod]
        [DataRow("2.5.7.10213")]
        [DataRow("2.7.0")]
        public void TestMethodShouldNotBeIgnored_NUnit_V2(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.NUnit.V2.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    NuGetMetadataReference.MSTestTestFrameworkV1
                                        .Concat(NuGetMetadataReference.NUnit(testFwkVersion))
                                        .ToArray());

        [DataTestMethod]
        [DataRow("3.0.0")] // Ignore without reason no longer exist
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldNotBeIgnored_NUnit(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.NUnit.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    NuGetMetadataReference.MSTestTestFrameworkV1
                                        .Concat(NuGetMetadataReference.NUnit(testFwkVersion))
                                        .ToArray());

        [DataTestMethod]
        [DataRow("2.0.0")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestMethodShouldNotBeIgnored_Xunit(string testFwkVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.Xunit.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    NuGetMetadataReference.MSTestTestFrameworkV1
                                        .Concat(NuGetMetadataReference.XunitFramework(testFwkVersion))
                                        .ToArray());

        [TestMethod]
        public void TestMethodShouldNotBeIgnored_Xunit_v1() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.Xunit.v1.cs",
                                    new TestMethodShouldNotBeIgnored(),
                                    NuGetMetadataReference.MSTestTestFrameworkV1
                                        .Concat(NuGetMetadataReference.XunitFrameworkV1)
                                        .ToArray());

#if NET
        [TestMethod]
        public void TestMethodShouldNotBeIgnored_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\TestMethodShouldNotBeIgnored.CSharp9.cs",
                                                new TestMethodShouldNotBeIgnored(),
                                                NuGetMetadataReference.MSTestTestFrameworkV1
                                                    .Concat(NuGetMetadataReference.XunitFrameworkV1)
                                                    .Concat(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion))
                                                    .ToArray());
#endif
    }
}
