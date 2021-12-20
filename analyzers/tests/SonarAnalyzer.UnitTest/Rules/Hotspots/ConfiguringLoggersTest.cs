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
    public class ConfiguringLoggersTest
    {
        [TestMethod]
        public void ConfiguringLoggers_Log4Net_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_Log4Net.cs",
                new CS.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                Log4NetReferences);

        [TestMethod]
        public void ConfiguringLoggers_Log4Net_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_Log4Net.vb",
                new VB.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                Log4NetReferences);

        [TestMethod]
        public void ConfiguringLoggers_NLog_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_NLog.cs",
                new CS.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                NLogReferences);

        [TestMethod]
        public void ConfiguringLoggers_NLog_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_NLog.vb",
                new VB.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                NLogReferences);

        [TestMethod]
        public void ConfiguringLoggers_Serilog_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_Serilog.cs",
                new CS.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                SeriLogReferences);

        [TestMethod]
        public void ConfiguringLoggers_Serilog_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_Serilog.vb",
                new VB.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                SeriLogReferences);

#if NET
        [TestMethod]
        public void ConfiguringLoggers_AspNetCore_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_AspNetCore.cs",
                                    new CS.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                                    AspNetCoreLoggingReferences);

        [TestMethod]
        public void ConfiguringLoggers_AspNetCore_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\Hotspots\ConfiguringLoggers_AspNetCore.vb",
                                    new VB.ConfiguringLoggers(AnalyzerConfiguration.AlwaysEnabled),
                                    AspNetCoreLoggingReferences);
#endif

        internal static IEnumerable<MetadataReference> Log4NetReferences =>
            // See: https://github.com/SonarSource/sonar-dotnet/issues/3548
            NuGetMetadataReference.Log4Net("2.0.8", "net45-full")
            .Concat(MetadataReferenceFacade.SystemXml);

        private static IEnumerable<MetadataReference> NLogReferences =>
            NuGetMetadataReference.NLog(Constants.NuGetLatestVersion);

        private static IEnumerable<MetadataReference> SeriLogReferences =>
            NuGetMetadataReference.SerilogPackages(Constants.NuGetLatestVersion);

#if NET
        private static IEnumerable<MetadataReference> AspNetCoreLoggingReferences =>
            NetStandardMetadataReference.Netstandard
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCore(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHosting(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHostingAbstractions(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftAspNetCoreHttpAbstractions(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftExtensionsConfigurationAbstractions(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftExtensionsOptions(Constants.DotNetCore220Version))
                                        .Concat(NuGetMetadataReference.MicrosoftExtensionsLoggingPackages(Constants.DotNetCore220Version))
                                        .Concat(new[] {CoreMetadataReference.MicrosoftExtensionsDependencyInjectionAbstractions});
#endif
    }
}
