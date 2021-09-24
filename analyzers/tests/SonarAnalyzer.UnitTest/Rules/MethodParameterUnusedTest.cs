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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MethodParameterUnusedTest
    {
        [TestMethod]
        public void MethodParameterUnused_CS_SonarCfg() =>
            Verifier.VerifyAnalyzer(@"TestCases\MethodParameterUnused.SonarCfg.cs", new CS.MethodParameterUnused(AnalyzerConfiguration.AlwaysEnabledWithSonarCfg));

        [TestMethod]
        public void MethodParameterUnused_CS_RoslynCfg() =>
            Verifier.VerifyAnalyzer(@"TestCases\MethodParameterUnused.RoslynCfg.cs", new CS.MethodParameterUnused());   // Default constructor uses Roslyn CFG

#if NETFRAMEWORK

        [TestMethod]
        public void MethodParameterUnused_CS_RoslynCfg_NetFx() =>
            Verifier.VerifyAnalyzer(@"TestCases\MethodParameterUnused.RoslynCfg.NetFx.cs", new CS.MethodParameterUnused());

#endif

        [TestMethod]
        public void MethodParameterUnused_CodeFix_CS() =>
            Verifier.VerifyCodeFix(@"TestCases\MethodParameterUnused.RoslynCfg.cs",
                                   @"TestCases\MethodParameterUnused.RoslynCfg.Fixed.cs",
                                   new CS.MethodParameterUnused(),
                                   new CS.MethodParameterUnusedCodeFixProvider());

        [TestMethod]
        public void MethodParameterUnused_CSharp7_CS() =>
            Verifier.VerifyNoIssueReported(@"TestCases\MethodParameterUnused.CSharp7.cs",
                                           new CS.MethodParameterUnused(),
                                           ParseOptionsHelper.FromCSharp7,
                                           NuGetMetadataReference.SystemValueTuple("4.5.0"));

        [TestMethod]
        public void MethodParameterUnused_CSharp8_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\MethodParameterUnused.CSharp8.cs",
                                    new CS.MethodParameterUnused(),
#if NETFRAMEWORK
                                    ParseOptionsHelper.FromCSharp8,
                                    NuGetMetadataReference.NETStandardV2_1_0);
#else
                                    ParseOptionsHelper.FromCSharp8);
#endif

        [TestMethod]
        public void MethodParameterUnused_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\MethodParameterUnused.vb", new VB.MethodParameterUnused());
    }
}
