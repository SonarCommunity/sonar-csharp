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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using csharp::SonarAnalyzer.Rules.CSharp;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ConfiguringLoggersTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ConfiguringLoggers_CS()
        {
            Verifier.VerifyAnalyzer(@"TestCases\ConfiguringLoggers_AspNetCore.cs",
                new ConfiguringLoggers(new TestAnalyzerConfiguration(null, "S4792")),
                additionalReferences: AspNetCoreLoggingReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ConfiguringLoggers_VB()
        {
            Verifier.VerifyAnalyzer(@"TestCases\ConfiguringLoggers_AspNetCore.vb",
                new SonarAnalyzer.Rules.VisualBasic.ConfiguringLoggers(new TestAnalyzerConfiguration(null, "S4792")),
                additionalReferences: AspNetCoreLoggingReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ConfiguringLoggers_CS_RuleDisabled()
        {
            Verifier.VerifyNoIssueReported(@"TestCases\ConfiguringLoggers_AspNetCore.cs",
                new ConfiguringLoggers(),
                additionalReferences: AspNetCoreLoggingReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ConfiguringLoggers_VB_RuleDisabled()
        {
            Verifier.VerifyNoIssueReported(@"TestCases\ConfiguringLoggers_AspNetCore.vb",
                new SonarAnalyzer.Rules.VisualBasic.ConfiguringLoggers(),
                additionalReferences: AspNetCoreLoggingReferences);
        }
        
        private static IEnumerable<MetadataReference> AspNetCoreLoggingReferences =>   
            FrameworkMetadataReference.Netstandard
            .Concat(NuGetMetadataReference.MicrosoftAspNetCore(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHosting(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHostingAbstractions(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHttpAbstractions(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftExtensionsConfigurationAbstractions(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftExtensionsDependencyInjectionAbstractions(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftExtensionsOptions(Constants.NuGetLatestVersion))
            .Concat(NuGetMetadataReference.MicrosoftExtensionsLoggingPackages(Constants.NuGetLatestVersion))
        ;
        
    }
}

