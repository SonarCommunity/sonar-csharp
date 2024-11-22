﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class TestClassShouldHaveTestMethodTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<TestClassShouldHaveTestMethod>();

        [DataTestMethod]
        [DataRow("2.5.7.10213")]
        [DataRow("3.14.0")] // Breaking changes in NUnit 4.0 would fail the test https://github.com/SonarSource/sonar-dotnet/issues/8409
        public void TestClassShouldHaveTestMethod_NUnit(string testFwkVersion) =>
            builder
                .AddPaths("TestClassShouldHaveTestMethod.NUnit.cs")
                .AddReferences(NuGetMetadataReference.NUnit(testFwkVersion))
                .Verify();

        [DataTestMethod]
        [DataRow("3.0.0")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestClassShouldHaveTestMethod_NUnit3(string testFwkVersion) =>
            builder
                .AddPaths("TestClassShouldHaveTestMethod.NUnit3.cs")
                .AddReferences(NuGetMetadataReference.NUnit(testFwkVersion))
                .Verify();

        [DataTestMethod]
        [DataRow("1.1.11")]
        [DataRow(Constants.NuGetLatestVersion)]
        public void TestClassShouldHaveTestMethod_MSTest(string testFwkVersion) =>
            builder
                .AddPaths("TestClassShouldHaveTestMethod.MsTest.cs")
                .AddReferences(NuGetMetadataReference.MSTestTestFramework(testFwkVersion))
                .Verify();

#if NET

        [DataTestMethod]
        public void TestClassShouldHaveTestMethod_CSharp9() =>
            builder
                .WithOptions(ParseOptionsHelper.FromCSharp9)
                .AddPaths("TestClassShouldHaveTestMethod.CSharp9.cs")
                .AddReferences(NuGetMetadataReference.MSTestTestFramework(Constants.NuGetLatestVersion))
                .AddReferences(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion))
                .Verify();

        [DataTestMethod]
        public void TestClassShouldHaveTestMethod_CSharp11() =>
            builder
                .WithOptions(ParseOptionsHelper.FromCSharp11)
                .AddPaths("TestClassShouldHaveTestMethod.CSharp11.cs")
                .AddReferences(NuGetMetadataReference.MSTestTestFramework(Constants.NuGetLatestVersion))
                .AddReferences(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion))
                .Verify();

        [DataTestMethod]
        public void TestClassShouldHaveTestMethod_CSharp12() =>
            builder
                .WithOptions(ParseOptionsHelper.FromCSharp12)
                .AddPaths("TestClassShouldHaveTestMethod.CSharp12.cs")
                .AddReferences(NuGetMetadataReference.MSTestTestFramework(Constants.NuGetLatestVersion))
                .AddReferences(NuGetMetadataReference.NUnit(Constants.NuGetLatestVersion))
                .Verify();

#endif

    }
}
