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
using SonarAnalyzer.UnitTest.Helpers;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class FunctionComplexityTest
    {
        [TestMethod]
        public void FunctionComplexity_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\FunctionComplexity.cs", new CS.FunctionComplexity { Maximum = 3 }, ParseOptionsHelper.FromCSharp8);

#if NET
        [TestMethod]
        public void FunctionComplexity_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\FunctionComplexity.CSharp9.cs", new CS.FunctionComplexity { Maximum = 3 });

        [TestMethod]
        public void FunctionComplexity_CSharp10() =>
            Verifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\FunctionComplexity.CSharp10.cs", new CS.FunctionComplexity { Maximum = 1 });
#endif

        [TestMethod]
        public void FunctionComplexity_InsufficientExecutionStack_CS()
        {
            if (!TestContextHelper.IsAzureDevOpsContext) // ToDo: Test doesn't work on Azure DevOps
            {
                Verifier.VerifyAnalyzer(@"TestCases\SyntaxWalker_InsufficientExecutionStackException.cs", new CS.FunctionComplexity { Maximum = 3 });
            }
        }

        [TestMethod]
        public void FunctionComplexity_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\FunctionComplexity.vb", new VB.FunctionComplexity { Maximum = 3 });
    }
}
