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
    public class CreatingHashAlgorithmsTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\CreatingHashAlgorithms.cs",
                                    new CS.CreatingHashAlgorithms(AnalyzerConfiguration.AlwaysEnabled),
                                    ParseOptionsHelper.FromCSharp8,
                                    additionalReferences: GetAdditionalReferences());
#if NETFRAMEWORK // HMACRIPEMD160, MD5Cng, RIPEMD160Managed and RIPEMD160 are available only for .Net Framework
        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_CS_NetFx() =>
            Verifier.VerifyAnalyzer(@"TestCases\CreatingHashAlgorithms.NetFramework.cs",
                                    new CS.CreatingHashAlgorithms(AnalyzerConfiguration.AlwaysEnabled),
                                    ParseOptionsHelper.FromCSharp8,
                                    additionalReferences: GetAdditionalReferences());
#endif

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\CreatingHashAlgorithms.vb",
                                    new VB.CreatingHashAlgorithms(AnalyzerConfiguration.AlwaysEnabled),
                                    additionalReferences: GetAdditionalReferences());

#if NETFRAMEWORK // HMACRIPEMD160, MD5Cng, RIPEMD160Managed and RIPEMD160 are available only for .Net Framework
        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_VB_NetFx() =>
            Verifier.VerifyAnalyzer(@"TestCases\CreatingHashAlgorithms.NetFramework.vb",
                                    new VB.CreatingHashAlgorithms(AnalyzerConfiguration.AlwaysEnabled),
                                    additionalReferences: GetAdditionalReferences());
#endif

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_CS_RuleDisabled() =>
            Verifier.VerifyNoIssueReported(@"TestCases\CreatingHashAlgorithms.cs",
                                           new CS.CreatingHashAlgorithms(),
                                           ParseOptionsHelper.FromCSharp8,
                                           additionalReferences: GetAdditionalReferences());

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void CreatingHashAlgorithms_VB_RuleDisabled() =>
            Verifier.VerifyNoIssueReported(@"TestCases\CreatingHashAlgorithms.vb",
                                           new VB.CreatingHashAlgorithms(),
                                           additionalReferences: GetAdditionalReferences());

        private static IEnumerable<MetadataReference> GetAdditionalReferences() =>
            MetadataReferenceFacade.SystemSecurityCryptography;
    }
}
