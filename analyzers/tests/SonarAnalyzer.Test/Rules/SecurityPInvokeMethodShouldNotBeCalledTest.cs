﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class SecurityPInvokeMethodShouldNotBeCalledTest
    {
        private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.SecurityPInvokeMethodShouldNotBeCalled>();

        [TestMethod]
        public void SecurityPInvokeMethodShouldNotBeCalled_CS() =>
            builderCS.AddPaths("SecurityPInvokeMethodShouldNotBeCalled.cs").Verify();

#if NET

        [TestMethod]
        public void SecurityPInvokeMethodShouldNotBeCalled_CSharp11() =>
            builderCS.AddPaths("SecurityPInvokeMethodShouldNotBeCalled.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();

        [TestMethod]
        public void SecurityPInvokeMethodShouldNotBeCalled_CSharp12() =>
            builderCS.AddPaths("SecurityPInvokeMethodShouldNotBeCalled.CSharp12.cs").WithOptions(ParseOptionsHelper.FromCSharp12).Verify();

#endif

        [TestMethod]
        public void SecurityPInvokeMethodShouldNotBeCalled_VB() =>
            new VerifierBuilder<VB.SecurityPInvokeMethodShouldNotBeCalled>().AddPaths("SecurityPInvokeMethodShouldNotBeCalled.vb").Verify();
    }
}
