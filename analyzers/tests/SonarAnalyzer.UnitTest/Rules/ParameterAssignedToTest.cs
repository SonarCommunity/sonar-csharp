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
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ParameterAssignedToTest
    {
        [TestMethod]
        public void ParameterAssignedTo_CSharp() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ParameterAssignedTo.cs", new CS.ParameterAssignedTo());

#if NET
        [TestMethod]
        public void ParameterAssignedTo_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\ParameterAssignedTo.CSharp9.cs", new CS.ParameterAssignedTo());

        [TestMethod]
        public void ParameterAssignedTo_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\ParameterAssignedTo.CSharp10.cs", new CS.ParameterAssignedTo());
#endif

        [TestMethod]
        public void ParameterAssignedTo_VisualBasic() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\ParameterAssignedTo.vb", new VB.ParameterAssignedTo());
    }
}
