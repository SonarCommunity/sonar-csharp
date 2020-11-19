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
using csharp::SonarAnalyzer.Rules.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Rules.SymbolicExecution;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ConditionEvaluatesToConstantTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void ConditionEvaluatesToConstant() =>
            Verifier.VerifyAnalyzer(@"TestCases\ConditionEvaluatesToConstant.cs",
                GetAnalyzer(),
                additionalReferences: NuGetMetadataReference.MicrosoftExtensionsPrimitives("3.1.7"));

        [TestMethod]
        [TestCategory("Rule")]
        public void ConditionEvaluatesToConstant_FromCSharp7() =>
            Verifier.VerifyAnalyzer(@"TestCases\ConditionEvaluatesToConstant.CSharp7.cs",
                GetAnalyzer(),
                ParseOptionsHelper.FromCSharp7);

        [TestMethod]
        [TestCategory("Rule")]
        public void ConditionEvaluatesToConstant_FromCSharp8() =>
            Verifier.VerifyAnalyzer(@"TestCases\ConditionEvaluatesToConstant.CSharp8.cs",
                GetAnalyzer(),
#if NETFRAMEWORK
                additionalReferences: NuGetMetadataReference.NETStandardV2_1_0,
#endif
                options: ParseOptionsHelper.FromCSharp8);

#if NET
        [TestMethod]
        [TestCategory("Rule")]
        public void ConditionEvaluatesToConstant_FromCSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\ConditionEvaluatesToConstant.CSharp9.cs", GetAnalyzer());

        [TestMethod]
        [TestCategory("Rule")]
        public void ConditionEvaluatesToConstant_FromCSharp9_TopLevelStatements() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\ConditionEvaluatesToConstant.CSharp9.TopLevelStatements.cs", GetAnalyzer());
#endif

        private static SonarDiagnosticAnalyzer GetAnalyzer() =>
            new SymbolicExecutionRunner(new ConditionEvaluatesToConstant());
    }
}
