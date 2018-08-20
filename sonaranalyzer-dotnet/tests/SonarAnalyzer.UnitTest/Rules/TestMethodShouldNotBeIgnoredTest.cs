﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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

extern alias csharp;

using System.Linq;
using csharp::SonarAnalyzer.Rules.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class TestMethodShouldNotBeIgnoredTest
    {
        [DataTestMethod]
        [DataRow("1.1.11")]
        [DataRow(Constants.NuGetLatestVersion)]
        [TestCategory("Rule")]
        public void TestMethodShouldNotBeIgnored_MsTest(string testFwkVersion)
        {
            Verifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.MsTest.cs",
                new TestMethodShouldNotBeIgnored(),
                additionalReferences: NuGetMetadataReference.MSTestTestFramework[testFwkVersion]);
        }

        [DataTestMethod]
        [DataRow("2.5.7.10213")]
        [DataRow(Constants.NuGetLatestVersion)]
        [TestCategory("Rule")]
        public void TestMethodShouldNotBeIgnored_NUnit(string testFwkVersion)
        {
            Verifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.NUnit.cs",
                new TestMethodShouldNotBeIgnored(),
                additionalReferences: NuGetMetadataReference.MSTestTestFramework["1.1.11"]
                    .Concat(NuGetMetadataReference.NUnit[testFwkVersion])
                    .ToArray());
        }

        [DataTestMethod]
        [DataRow("2.0.0")]
        [DataRow(Constants.NuGetLatestVersion)]
        [TestCategory("Rule")]
        public void TestMethodShouldNotBeIgnored_Xunit(string testFwkVersion)
        {
            Verifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.Xunit.cs",
                new TestMethodShouldNotBeIgnored(),
                additionalReferences: NuGetMetadataReference.MSTestTestFramework["1.1.11"]
                    .Concat(NuGetMetadataReference.XunitAssert[testFwkVersion])
                    .Concat(NuGetMetadataReference.XunitExtensibilityCore[testFwkVersion])
                    .ToArray());
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void TestMethodShouldNotBeIgnored_Xunit_v1()
        {
            Verifier.VerifyAnalyzer(@"TestCases\TestMethodShouldNotBeIgnored.Xunit.v1.cs",
                new TestMethodShouldNotBeIgnored(),
                additionalReferences: MetadataReferenceHelper.FromNuGet("MSTest.TestFramework", "1.1.11")
                    .Concat(MetadataReferenceHelper.FromNuGet("xunit.extensions", "1.9.1"))
                    .Concat(MetadataReferenceHelper.FromNuGet("xunit", "1.9.1"))
                    .ToArray());
        }
    }
}
