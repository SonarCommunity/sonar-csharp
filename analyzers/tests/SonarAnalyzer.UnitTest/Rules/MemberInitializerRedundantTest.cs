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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MemberInitializerRedundantTest
    {
        [TestMethod]
        public void MemberInitializerRedundant_RoslynCfg() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\MemberInitializerRedundant.cs", new MemberInitializerRedundant(), ParseOptionsHelper.FromCSharp8);

        [TestMethod]
        public void MemberInitializerRedundant_RoslynCfg_FlowCaptureOperationNotSupported() =>
            OldVerifier.VerifyNoIssueReported(@"TestCases\MemberInitializerRedundant.RoslynCfg.FlowCaptureBug.cs", new MemberInitializerRedundant(), ParseOptionsHelper.FromCSharp8);

        [TestMethod]
        public void MemberInitializerRedundant_SonarCfg() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\MemberInitializerRedundant.cs", new MemberInitializerRedundant(AnalyzerConfiguration.AlwaysEnabledWithSonarCfg), ParseOptionsHelper.FromCSharp8);

        [TestMethod]
        [TestCategory("CodeFix")]
        public void MemberInitializerRedundant_CodeFix() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\MemberInitializerRedundant.cs",
                @"TestCases\MemberInitializerRedundant.Fixed.cs",
                new MemberInitializerRedundant(),
                new MemberInitializedToDefaultCodeFixProvider());

#if NET
        [TestMethod]
        public void MemberInitializerRedundant_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\MemberInitializerRedundant.CSharp9.cs", new MemberInitializerRedundant());

        [TestMethod]
        public void MemberInitializerRedundant_CSharp9_CodeFix() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\MemberInitializerRedundant.CSharp9.cs",
                @"TestCases\MemberInitializerRedundant.CSharp9.Fixed.cs",
                new MemberInitializerRedundant(),
                new MemberInitializedToDefaultCodeFixProvider(),
                ParseOptionsHelper.FromCSharp9);

        [TestMethod]
        public void MemberInitializerRedundant_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\MemberInitializerRedundant.CSharp10.cs", new MemberInitializerRedundant());

        [TestMethod]
        public void MemberInitializerRedundant_CSharp10_CodeFix() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\MemberInitializerRedundant.CSharp10.cs",
                @"TestCases\MemberInitializerRedundant.CSharp10.Fixed.cs",
                new MemberInitializerRedundant(),
                new MemberInitializedToDefaultCodeFixProvider(),
                ParseOptionsHelper.FromCSharp10);

#endif
    }
}
