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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MemberShouldBeStaticTest
    {
        [DataTestMethod]
        [DataRow("1.0.0", "3.0.20105.1")]
        [DataRow(Constants.NuGetLatestVersion, Constants.NuGetLatestVersion)]
        public void MemberShouldBeStatic(string aspnetCoreVersion, string aspnetVersion) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\MemberShouldBeStatic.cs",
                                    new MemberShouldBeStatic(),
                                    NuGetMetadataReference.MicrosoftAspNetCoreMvcWebApiCompatShim(aspnetCoreVersion)
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetMvc(aspnetVersion))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreMvcCore(aspnetCoreVersion))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreMvcViewFeatures(aspnetCoreVersion))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreRoutingAbstractions(aspnetCoreVersion))
                                        .ToImmutableArray());

#if NET
        [TestMethod]
        public void MemberShouldBeStatic_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\MemberShouldBeStatic.CSharp9.cs", new MemberShouldBeStatic());
#endif

        [TestMethod]
        public void MemberShouldBeStatic_CSharp8() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\MemberShouldBeStatic.CSharp8.cs",
                new MemberShouldBeStatic(),
                ParseOptionsHelper.FromCSharp8,
                MetadataReferenceFacade.NETStandard21);

#if NETFRAMEWORK // HttpApplication is available only on .Net Framework
        [TestMethod]
        public void MemberShouldBeStatic_HttpApplication() =>
            OldVerifier.VerifyCSharpAnalyzer(@"
public class HttpApplication1 : System.Web.HttpApplication
{
    public int Foo() => 0;

    protected int FooFoo() => 0; // Noncompliant
}",
                new MemberShouldBeStatic(),
                CompilationErrorBehavior.Ignore);
#endif

        [TestMethod]
        public void MemberShouldBeStatic_InvalidCode() =>
            // Handle invalid code causing NullReferenceException: https://github.com/SonarSource/sonar-dotnet/issues/819
            OldVerifier.VerifyCSharpAnalyzer(@"
public class Class7
{
    public async Task<Result<T> Function<T>(Func<Task<Result<T>>> f)
    {
        Result<T> result;
        result = await f();
        return result;
    }
}", new MemberShouldBeStatic(), CompilationErrorBehavior.Ignore);
    }
}
