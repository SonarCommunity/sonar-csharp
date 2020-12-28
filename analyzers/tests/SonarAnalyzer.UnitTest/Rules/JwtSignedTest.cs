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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using csharp = SonarAnalyzer.Rules.CSharp;
using vbnet = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class JwtSignedTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void JwtSigned_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\JwtSigned.cs", new csharp.JwtSigned(), additionalReferences: NuGetMetadataReference.JWT("6.1.0"));

        [TestMethod]
        [TestCategory("Rule")]
        public void JwtSigned_JWTDecoderExtensions_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\JwtSigned.Extensions.cs", new csharp.JwtSigned(), additionalReferences: NuGetMetadataReference.JWT("7.3.1"));

#if NET
        [TestMethod]
        [TestCategory("Rule")]
        public void JwtSigned_CS_FromCSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\JwtSigned.CSharp9.cs", new csharp.JwtSigned(), NuGetMetadataReference.JWT("6.1.0"));
#endif

        [TestMethod]
        [TestCategory("Rule")]
        public void JwtSigned_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\JwtSigned.vb", new vbnet.JwtSigned(), additionalReferences: NuGetMetadataReference.JWT("6.1.0"));

        [TestMethod]
        [TestCategory("Rule")]
        public void JwtSigned_JWTDecoderExtensions_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\JwtSigned.Extensions.vb", new vbnet.JwtSigned(), additionalReferences: NuGetMetadataReference.JWT("7.3.1"));
    }
}
