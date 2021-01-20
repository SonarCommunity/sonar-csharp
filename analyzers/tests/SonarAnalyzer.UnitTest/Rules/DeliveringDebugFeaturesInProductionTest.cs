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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class DeliveringDebugFeaturesInProductionTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore2_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore2.cs",
                new CS.DeliveringDebugFeaturesInProduction(AnalyzerConfiguration.AlwaysEnabled),
                AdditionalReferencesNetCore2);

        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore2_CS_Disabled() =>
            Verifier.VerifyNoIssueReported(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore2.cs",
                new CS.DeliveringDebugFeaturesInProduction(),
                AdditionalReferencesNetCore2);

        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore2_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore2.vb",
                new VB.DeliveringDebugFeaturesInProduction(AnalyzerConfiguration.AlwaysEnabled),
                AdditionalReferencesNetCore2);

        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore2_VB_Disabled() =>
            Verifier.VerifyNoIssueReported(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore2.vb",
                new VB.DeliveringDebugFeaturesInProduction(),
                AdditionalReferencesNetCore2);

#if NET

        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore3_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore3.cs",
                new CS.DeliveringDebugFeaturesInProduction(AnalyzerConfiguration.AlwaysEnabled),
                AdditionalReferencesNetCore3);

        [TestMethod]
        [TestCategory("Rule")]
        public void DeliveringDebugFeaturesInProduction_NetCore3_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\DeliveringDebugFeaturesInProduction.NetCore3.vb",
                new VB.DeliveringDebugFeaturesInProduction(AnalyzerConfiguration.AlwaysEnabled),
                AdditionalReferencesNetCore3);

        internal static IEnumerable<MetadataReference> AdditionalReferencesNetCore3 =>
            Enumerable.Empty<MetadataReference>()
                .Concat(AspNetCoreMetadataReference.MicrosoftAspNetCoreDiagnostics)
                .Concat(AspNetCoreMetadataReference.MicrosoftAspNetCoreHostingAbstractions)
                .Concat(AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpAbstractions)
                .Concat(AspNetCoreMetadataReference.MicrosoftExtensionsHostingAbstractions);

#endif

        internal static IEnumerable<MetadataReference> AdditionalReferencesNetCore2 =>
            Enumerable.Empty<MetadataReference>()
                .Concat(NetStandardMetadataReference.Netstandard)
                .Concat(NuGetMetadataReference.MicrosoftAspNetCoreDiagnostics(Constants.DotNetCore220Version))
                .Concat(NuGetMetadataReference.MicrosoftAspNetCoreDiagnosticsEntityFrameworkCore(Constants.DotNetCore220Version))
                .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHttpAbstractions(Constants.DotNetCore220Version))
                .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHostingAbstractions(Constants.DotNetCore220Version));
    }
}
