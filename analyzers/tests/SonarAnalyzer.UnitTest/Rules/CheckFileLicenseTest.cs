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

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CheckFileLicenseTest
    {
        private const string SingleLineHeader = "// Copyright (c) SonarSource. All Rights Reserved. Licensed under the LGPL License.  See License.txt in the project root for license information.";
        private const string MultiLineHeader = @"/*
 * SonarQube, open source software quality management tool.
 * Copyright (C) 2008-2013 SonarSource
 * mailto:contact AT sonarsource DOT com
 *
 * SonarQube is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * SonarQube is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */";
        private const string MultiSingleLineCommentHeader = @"//-----
// MyHeader
//-----";
        private const string HeaderForcingLineBreak = @"//---


";
        private const string SingleLineRegexHeader = @"// Copyright \(c\) \w*\. All Rights Reserved\. " +
            @"Licensed under the LGPL License\.  See License\.txt in the project root for license information\.";
        private const string MultiLineRegexHeader = @"/\*
 \* SonarQube, open source software quality management tool\.
 \* Copyright \(C\) \d\d\d\d-\d\d\d\d SonarSource
 \* mailto:contact AT sonarsource DOT com
 \*
 \* SonarQube is free software; you can redistribute it and/or
 \* modify it under the terms of the GNU Lesser General Public
 \* License as published by the Free Software Foundation; either
 \* version 3 of the License, or \(at your option\) any later version\.
 \*
 \* SonarQube is distributed in the hope that it will be useful,
 \* but WITHOUT ANY WARRANTY; without even the implied warranty of
 \* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE\. See the GNU
 \* Lesser General Public License for more details\.
 \*
 \* You should have received a copy of the GNU Lesser General Public License
 \* along with this program; if not, write to the Free Software Foundation,
 \* Inc\., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA\.
 \*/";
        private const string MultiLineRegexWithNewLine = "//-{5}\r\n// MyHeader\r\n//-{5}";
        private const string MultiLineRegexWithDot = "//-{5}.+// MyHeader.+//-{5}";
        private const string FailingSingleLineRegexHeader = "[";

        [TestMethod]
        public void CheckFileLicense_WhenUnlicensedFileStartingWithUsing_ShouldBeNoncompliant_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_NoLicenseStartWithUsing.cs", new CS.CheckFileLicense { HeaderFormat = SingleLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedFileStartingWithUsing_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_SingleLineLicenseStartWithUsing.cs", new CS.CheckFileLicense { HeaderFormat = SingleLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedFileStartingWithUsingAndUsingCustomValues_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_SingleLineLicenseStartWithUsing.cs",
                new CS.CheckFileLicense { HeaderFormat = SingleLineRegexHeader, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultilineCommentStartingWithUsing_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiLineLicenseStartWithUsing.cs", new CS.CheckFileLicense { HeaderFormat = MultiLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultilineCommentStartingWithUsingWithCustomValues_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiLineLicenseStartWithUsing.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineRegexHeader, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenNoLicenseStartingWithNamespace_ShouldBeNonCompliant_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_NoLicenseStartWithNamespace.cs", new CS.CheckFileLicense { HeaderFormat = SingleLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithSingleLineCommentStartingWithNamespace_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_SingleLineLicenseStartWithNamespace.cs", new CS.CheckFileLicense { HeaderFormat = SingleLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithSingleLineCommentStartingWithNamespaceAndUsingCustomValues_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_SingleLineLicenseStartWithNamespace.cs",
                new CS.CheckFileLicense { HeaderFormat = SingleLineRegexHeader, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultilineCommentStartingWithNamespace_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiLineLicenseStartWithNamespace.cs", new CS.CheckFileLicense { HeaderFormat = MultiLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultilineCommentStartingWithNamespaceAndUsingCustomValues_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiLineLicenseStartWithNamespace.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineRegexHeader, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultiSingleLineCommentStartingWithNamespaceAndNoRegex_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiSingleLineLicenseStartWithNamespace.cs", new CS.CheckFileLicense { HeaderFormat = MultiSingleLineCommentHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultiSingleLineCommentStartingWithAdditionalComments_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiSingleLineLicenseStartWithAdditionalComment.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiSingleLineCommentHeader });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultiSingleLineCommentStartingWithAdditionalCommentOnSameLine_ShouldBeNonCompliant_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_MultiSingleLineLicenseStartWithAdditionalCommentOnSameLine.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiSingleLineCommentHeader });

        [TestMethod]
        public void CheckFileLicense_WithForcingEmptyLines_ShouldBeNonCompliant_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_ForcingEmptyLinesKo.cs", new CS.CheckFileLicense { HeaderFormat = HeaderForcingLineBreak });

        [TestMethod]
        public void CheckFileLicense_WithForcingEmptyLines_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_ForcingEmptyLinesOk.cs", new CS.CheckFileLicense { HeaderFormat = HeaderForcingLineBreak });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultiSingleLineCommentStartingWithNamespaceAndMultiLineRegexWithNewLine_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiSingleLineLicenseStartWithNamespace.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineRegexWithNewLine, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenLicensedWithMultiSingleLineCommentStartingWithNamespaceAndMultiLineRegexWithDot_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_MultiSingleLineLicenseStartWithNamespace.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineRegexWithDot, IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenEmptyFile_ShouldBeNonCompliant_CS()
        {
            Action action =
                () => OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_EmptyFile.cs", new CS.CheckFileLicense { HeaderFormat = SingleLineHeader });
            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage(
                "CSharp*: Unexpected primary issue on line 1, span (0,0)-(0,0) with message 'Add or update the header of this file.'." + Environment.NewLine
                + "See output to see all actual diagnostics raised on the file");
        }

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenThereIsAYearDifference_ShouldBeNonCompliant_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_YearDifference.cs", new CS.CheckFileLicense { HeaderFormat = MultiLineHeader });

        [TestMethod]
        public void CheckFileLicense_WhenProvidingAnInvalidRegex_ShouldThrowException_CS()
        {
            var compilation = SolutionBuilder.CreateSolutionFromPaths(new[] { @"TestCases\CheckFileLicense_NoLicenseStartWithUsing.cs" }).Compile(ParseOptionsHelper.CSharpLatest.ToArray()).Single();
            var diagnostics = DiagnosticVerifier.GetDiagnosticsIgnoreExceptions(compilation, new CS.CheckFileLicense { HeaderFormat = FailingSingleLineRegexHeader, IsRegularExpression = true });
            diagnostics.Should().ContainSingle(x => x.Id == "AD0001").Which.GetMessage().Should()
                .StartWith("Analyzer 'SonarAnalyzer.Rules.CSharp.CheckFileLicense' threw an exception of type 'System.InvalidOperationException' with message 'Invalid regular expression:");
        }

        [TestMethod]
        public void CheckFileLicense_WhenUsingComplexRegex_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_ComplexRegex.cs",
               new CS.CheckFileLicense { HeaderFormat = @"// <copyright file="".*\.cs"" company="".*"">\r\n// Copyright \(c\) 2012 All Rights Reserved\r\n// </copyright>\r\n// <author>.*</author>\r\n// <date>.*</date>\r\n// <summary>.*</summary>\r\n", IsRegularExpression = true });

        [TestMethod]
        public void CheckFileLicense_WhenUsingMultilinesHeaderAsSingleLineString_ShouldBeCompliant_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\CheckFileLicense_ComplexRegex.cs",
               new CS.CheckFileLicense { HeaderFormat = @"// <copyright file=""ProgramHeader2.cs"" company=""My Company Name"">\r\n// Copyright (c) 2012 All Rights Reserved\r\n// </copyright>\r\n// <author>Name of the Authour</author>\r\n// <date>08/22/2017 12:39:58 AM </date>\r\n// <summary>Class representing a Sample entity</summary>\r\n", IsRegularExpression = false });

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenNoLicenseStartWithNamespaceAndUsesDefaultValues_ShouldBeNoncompliant_CS() =>
            OldVerifier.VerifyCodeFix(@"TestCases\CheckFileLicense_DefaultValues.cs",
                                   @"TestCases\CheckFileLicense_DefaultValues.Fixed.cs",
                                   new CS.CheckFileLicense(),
                                   new CS.CheckFileLicenseCodeFixProvider());

#if NET
        [TestMethod]
        public void CheckFileLicenseCodeFix_CSharp9_ShouldBeNoncompliant_CS() =>
            OldVerifier.VerifyCodeFix(@"TestCases\CheckFileLicense_CSharp9.cs",
                                   @"TestCases\CheckFileLicense_CSharp9.Fixed.cs",
                                   new CS.CheckFileLicense(),
                                   new CS.CheckFileLicenseCodeFixProvider(),
                                   ParseOptionsHelper.FromCSharp9);
#endif

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenNoLicenseStartingWithUsing_ShouldBeFixedAsExpected_CS() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\CheckFileLicense_NoLicenseStartWithUsing.cs",
                @"TestCases\CheckFileLicense_NoLicenseStartWithUsing.Fixed.cs",
                new CS.CheckFileLicense { HeaderFormat = SingleLineHeader },
                new CS.CheckFileLicenseCodeFixProvider());

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenNoLicenseStartingWithNamespace_ShouldBeFixedAsExpected_CS() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\CheckFileLicense_NoLicenseStartWithNamespace.cs",
                @"TestCases\CheckFileLicense_NoLicenseStartWithNamespace.Fixed.cs",
                new CS.CheckFileLicense { HeaderFormat = SingleLineHeader },
                new CS.CheckFileLicenseCodeFixProvider());

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenThereIsAYearDifference_ShouldBeFixedAsExpected_CS() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\CheckFileLicense_YearDifference.cs",
                @"TestCases\CheckFileLicense_YearDifference.Fixed.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineHeader },
                new CS.CheckFileLicenseCodeFixProvider());

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenOutdatedLicenseStartingWithUsing_ShouldBeFixedAsExpected_CS() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\CheckFileLicense_OutdatedLicenseStartWithUsing.cs",
                @"TestCases\CheckFileLicense_OutdatedLicenseStartWithUsing.Fixed.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineHeader },
                new CS.CheckFileLicenseCodeFixProvider());

        [TestMethod]
        public void CheckFileLicenseCodeFix_WhenOutdatedLicenseStartingWithNamespace_ShouldBeFixedAsExpected_CS() =>
            OldVerifier.VerifyCodeFix(
                @"TestCases\CheckFileLicense_OutdatedLicenseStartWithNamespace.cs",
                @"TestCases\CheckFileLicense_OutdatedLicenseStartWithNamespace.Fixed.cs",
                new CS.CheckFileLicense { HeaderFormat = MultiLineHeader },
                new CS.CheckFileLicenseCodeFixProvider());

        [TestMethod]
        public void CheckFileLicense_NullHeader_NoIssueReported_CS() =>
            OldVerifier.VerifyNoIssueReported(@"TestCases\CheckFileLicense_NoLicenseStartWithNamespace.cs", new CS.CheckFileLicense { HeaderFormat = null });

        // No need to duplicate all test cases from C#, because we are sharing the implementation
        [TestMethod]
        public void CheckFileLicense_NonCompliant_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_NonCompliant.vb",
                new VB.CheckFileLicense());

        [TestMethod]
        public void CheckFileLicense_Compliant_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\CheckFileLicense_Compliant.vb",
                new VB.CheckFileLicense
                {
                    HeaderFormat = @"Copyright \(c\) [0-9]+ All Rights Reserved
",
                    IsRegularExpression = true
                });
    }
}
