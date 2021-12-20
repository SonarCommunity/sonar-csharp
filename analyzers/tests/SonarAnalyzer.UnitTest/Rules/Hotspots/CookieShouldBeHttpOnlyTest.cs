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

using System.Collections.Generic;
#if NETFRAMEWORK
using System.IO;
#endif
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CookieShouldBeHttpOnlyTest
    {
        [TestMethod]
        public void CookiesShouldBeHttpOnly_Nancy() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly_Nancy.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                AdditionalReferences);

#if NETFRAMEWORK // The analyzed code is valid only for .Net Framework

        private const string WebConfig = "Web.config";

        [TestMethod]
        public void CookiesShouldBeHttpOnly() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                MetadataReferenceFacade.SystemWeb);

        [DataTestMethod]
        [DataRow(@"TestCases\WebConfig\CookieShouldBeHttpOnly\HttpOnlyCookiesConfig")]
        [DataRow(@"TestCases\WebConfig\CookieShouldBeHttpOnly\Formatting")]
        public void CookiesShouldBeHttpOnly_WithWebConfigValueSetToTrue(string root)
        {
            var webConfigPath = Path.Combine(root, WebConfig);
            OldVerifier.VerifyAnalyzer(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly_WithWebConfig.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                MetadataReferenceFacade.SystemWeb,
                TestHelper.CreateSonarProjectConfig(root, TestHelper.CreateFilesToAnalyze(root, webConfigPath)));
        }

        [DataTestMethod]
        [DataRow(@"TestCases\WebConfig\CookieShouldBeHttpOnly\NonHttpOnlyCookiesConfig")]
        [DataRow(@"TestCases\WebConfig\CookieShouldBeHttpOnly\UnrelatedConfig")]
        [DataRow(@"TestCases\WebConfig\CookieShouldBeHttpOnly\ConfigWithoutAttribute")]
        public void CookiesShouldBeHttpOnly_WithWebConfigValueSetToFalse(string root)
        {
            var webConfigPath = Path.Combine(root, WebConfig);
            OldVerifier.VerifyAnalyzer(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                MetadataReferenceFacade.SystemWeb,
                TestHelper.CreateSonarProjectConfig(root, TestHelper.CreateFilesToAnalyze(root, webConfigPath)));
        }

#else
        [TestMethod]
        public void CookiesShouldBeHttpOnly_NetCore() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly_NetCore.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                GetAdditionalReferences_NetCore());

        [TestMethod]
        public void CookiesShouldBeHttpOnly_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly.CSharp9.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                GetAdditionalReferences_NetCore().Concat(NuGetMetadataReference.Nancy()));

        [TestMethod]
        public void CookiesShouldBeHttpOnly_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Console(
                @"TestCases\Hotspots\CookieShouldBeHttpOnly.CSharp10.cs",
                new CookieShouldBeHttpOnly(AnalyzerConfiguration.AlwaysEnabled),
                GetAdditionalReferences_NetCore().Concat(NuGetMetadataReference.Nancy()));

        private static IEnumerable<MetadataReference> GetAdditionalReferences_NetCore() =>
            NuGetMetadataReference.MicrosoftAspNetCoreHttpFeatures(Constants.NuGetLatestVersion);
#endif

        internal static IEnumerable<MetadataReference> AdditionalReferences =>
            NetStandardMetadataReference.Netstandard.Concat(NuGetMetadataReference.Nancy());
    }
}
