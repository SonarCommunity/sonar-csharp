﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using csharp::SonarAnalyzer.Rules.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CertificateValidationCheckTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void CertificateValidationCheck_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\CertificateValidationCheck.cs",
                new CertificateValidationCheck(),
                additionalReferences: GetAdditionalReferences());

        [TestMethod]
        [TestCategory("Rule")]
        public void CertificateValidationCheck_CS_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\CertificateValidationCheck.CSharp9.cs",
                new CertificateValidationCheck(),
                GetAdditionalReferences());

        [TestMethod]
        [TestCategory("Rule")]
        public void CertificateValidationCheck_CS_TopLevelStatements() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\CertificateValidationCheck.TopLevelStatements.cs",
                new CertificateValidationCheck(),
                GetAdditionalReferences());

        [TestMethod]
        [TestCategory("Rule")]
        public void CertificateValidationCheck_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\CertificateValidationCheck.vb",
                new SonarAnalyzer.Rules.VisualBasic.CertificateValidationCheck(),
                additionalReferences: GetAdditionalReferences()
            );

        private static IEnumerable<MetadataReference> GetAdditionalReferences() =>
            MetadataReferenceFacade.GetSystemNetHttp()
                .Concat(MetadataReferenceFacade.GetSystemSecurityCryptography())
                .Concat(NetStandardMetadataReference.Netstandard);
    }
}

