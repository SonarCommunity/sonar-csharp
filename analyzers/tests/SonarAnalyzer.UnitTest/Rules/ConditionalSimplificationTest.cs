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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ConditionalSimplificationTest
    {
        [TestMethod]
        public void ConditionalSimplification_BeforeCSharp8() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ConditionalSimplification.BeforeCSharp8.cs",
                                    new ConditionalSimplification(),
                                    ParseOptionsHelper.BeforeCSharp8);

        [TestMethod]
        public void ConditionalSimplification_CSharp8() =>
            new VerifierBuilder<ConditionalSimplification>()
                .AddPaths("ConditionalSimplification.CSharp8.cs")
                .WithLanguageVersion(LanguageVersion.CSharp8)
                .Verify();

        [TestMethod]
        public void ConditionalSimplification_CSharp8_CodeFix() =>
            OldVerifier.VerifyCodeFix<ConditionalSimplificationCodeFix>(
                @"TestCases\ConditionalSimplification.CSharp8.cs",
                @"TestCases\ConditionalSimplification.CSharp8.Fixed.cs",
                new ConditionalSimplification(),
                ImmutableArray.Create<ParseOptions>(new CSharpParseOptions(LanguageVersion.CSharp8)));  // ToDo: Use WithLanguageVersion instead

        [TestMethod]
        public void ConditionalSimplification_FromCSharp8() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ConditionalSimplification.FromCSharp8.cs",
                                    new ConditionalSimplification(),
                                    ParseOptionsHelper.FromCSharp8);
#if NET
        [TestMethod]
        public void ConditionalSimplification_FromCSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\ConditionalSimplification.FromCSharp9.cs",
                                                      new ConditionalSimplification());

        [TestMethod]
        public void ConditionalSimplification_FromCSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Console(@"TestCases\ConditionalSimplification.FromCSharp10.cs",
                                                       new ConditionalSimplification());
#endif

        [TestMethod]
        public void ConditionalSimplification_BeforeCSharp8_CodeFix() =>
            OldVerifier.VerifyCodeFix<ConditionalSimplificationCodeFix>(
                @"TestCases\ConditionalSimplification.BeforeCSharp8.cs",
                @"TestCases\ConditionalSimplification.BeforeCSharp8.Fixed.cs",
                new ConditionalSimplification(),
                ParseOptionsHelper.BeforeCSharp8);

        [TestMethod]
        public void ConditionalSimplification_FromCSharp8_CodeFix() =>
            OldVerifier.VerifyCodeFix<ConditionalSimplificationCodeFix>(
                @"TestCases\ConditionalSimplification.FromCSharp8.cs",
                @"TestCases\ConditionalSimplification.FromCSharp8.Fixed.cs",
                new ConditionalSimplification(),
                ParseOptionsHelper.FromCSharp8);

#if NET
        [TestMethod]
        public void ConditionalSimplification_FromCSharp9_CodeFix() =>
            OldVerifier.VerifyCodeFix<ConditionalSimplificationCodeFix>(
                @"TestCases\ConditionalSimplification.FromCSharp9.cs",
                @"TestCases\ConditionalSimplification.FromCSharp9.Fixed.cs",
                new ConditionalSimplification(),
                ParseOptionsHelper.FromCSharp9,
                OutputKind.ConsoleApplication);

        [TestMethod]
        public void ConditionalSimplification_FromCSharp10_CodeFix() =>
            OldVerifier.VerifyCodeFix<ConditionalSimplificationCodeFix>(
                @"TestCases\ConditionalSimplification.FromCSharp10.cs",
                @"TestCases\ConditionalSimplification.FromCSharp10.Fixed.cs",
                new ConditionalSimplification(),
                ParseOptionsHelper.FromCSharp10,
                OutputKind.ConsoleApplication);
#endif
    }
}
