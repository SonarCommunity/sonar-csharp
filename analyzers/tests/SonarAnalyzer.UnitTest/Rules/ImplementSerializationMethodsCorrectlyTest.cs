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

using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ImplementSerializationMethodsCorrectlyTest
    {
        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ImplementSerializationMethodsCorrectly.cs", new CS.ImplementSerializationMethodsCorrectly());

#if NET
        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_CS_FromCSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\ImplementSerializationMethodsCorrectly.CSharp9.cs", new CS.ImplementSerializationMethodsCorrectly());

        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_CS_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\ImplementSerializationMethodsCorrectly.CSharp10.cs", new CS.ImplementSerializationMethodsCorrectly());

        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_CS_CSharpPreview() =>
            OldVerifier.VerifyAnalyzerCSharpPreviewLibrary(@"TestCases\ImplementSerializationMethodsCorrectly.CSharp.Preview.cs", new CS.ImplementSerializationMethodsCorrectly());

#endif

        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_CS_InvalidCode() =>
            OldVerifier.VerifyCSharpAnalyzer(@"
[Serializable]
public class Foo
{
    [OnDeserializing]
    public int  { throw new NotImplementedException(); }
}", new CS.ImplementSerializationMethodsCorrectly(), CompilationErrorBehavior.Ignore);

        [TestMethod]
        public void ImplementSerializationMethodsCorrectly_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ImplementSerializationMethodsCorrectly.vb", new VB.ImplementSerializationMethodsCorrectly());
    }
}
