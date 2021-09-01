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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CertificateValidationCheckTest
    {
        [TestMethod]
        public void CertificateValidationCheck_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\CertificateValidationCheck.cs", new CS.CertificateValidationCheck(), GetAdditionalReferences());

#if NET
        [TestMethod]
        public void CertificateValidationCheck_CSharp8() =>
            Verifier.VerifyAnalyzer(@"TestCases\CertificateValidationCheck.CSharp8.cs",
                                    new CS.CertificateValidationCheck(),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetAdditionalReferences());

        [TestMethod]
        public void CertificateValidationCheck_CS_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Library(
                new[] { @"TestCases\CertificateValidationCheck.CSharp9.cs", @"TestCases\CertificateValidationCheck.CSharp9.Partial.cs" },
                new CS.CertificateValidationCheck(),
                GetAdditionalReferences());

        [TestMethod]
        public void CertificateValidationCheck_CS_TopLevelStatements() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\CertificateValidationCheck.TopLevelStatements.cs",
                                                      new CS.CertificateValidationCheck(),
                                                      GetAdditionalReferences());
#endif

        [TestMethod]
        public void CertificateValidationCheck_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\CertificateValidationCheck.vb",
                                    new VB.CertificateValidationCheck(),
                                    GetAdditionalReferences());

        [TestMethod]
        public void CreateParameterLookup_CS_ThrowsException()
        {
            var analyzer = new CS.CertificateValidationCheck();
            Action a = () => analyzer.CreateParameterLookup(null, null);
            a.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void CreateParameterLookup_VB_ThrowsException()
        {
            var analyzer = new VB.CertificateValidationCheck();
            Action a = () => analyzer.CreateParameterLookup(null, null);
            a.Should().Throw<ArgumentException>();
        }

        private static IEnumerable<MetadataReference> GetAdditionalReferences() =>
            MetadataReferenceFacade.SystemNetHttp
                                   .Concat(MetadataReferenceFacade.SystemSecurityCryptography)
                                   .Concat(NetStandardMetadataReference.Netstandard);
    }
}
