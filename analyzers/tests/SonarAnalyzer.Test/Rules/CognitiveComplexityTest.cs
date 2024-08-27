/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

using Microsoft.CodeAnalysis.CSharp;
using SonarAnalyzer.Test.Helpers;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class CognitiveComplexityTest
    {
        private readonly VerifierBuilder builderCS = new VerifierBuilder().AddAnalyzer(() => new CS.CognitiveComplexity { Threshold = 0, PropertyThreshold = 0 });
        private readonly VerifierBuilder builderVB = new VerifierBuilder().AddAnalyzer(() => new VB.CognitiveComplexity { Threshold = 0, PropertyThreshold = 0 });

        [TestMethod]
        public void CognitiveComplexity_CS() =>
            builderCS.AddPaths("CognitiveComplexity.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();

        [TestMethod]
        public void CognitiveComplexity_CS_LocalFunctions() =>
            builderCS.AddPaths("CognitiveComplexity.LocalFunctions.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();

#if NET

        [TestMethod]
        public void CognitiveComplexity_CS_CSharp9() =>
            builderCS.AddPaths("CognitiveComplexity.CSharp9.cs")
                .WithTopLevelStatements()
                .Verify();

        [TestMethod]
        public void CognitiveComplexity_CS_CSharp10() =>
            builderCS.AddPaths("CognitiveComplexity.CSharp10.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp10)
                .Verify();

        [TestMethod]
        public void CognitiveComplexity_CS_CSharp11() =>
            builderCS.AddPaths("CognitiveComplexity.CSharp11.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp11)
                .Verify();

#endif

        [TestMethod]
        public void CognitiveComplexity_VB() => builderVB.AddPaths("CognitiveComplexity.vb").Verify();

        [TestMethod]
        public void CognitiveComplexity_StackOverflow_CS()
        {
            if (!TestContextHelper.IsAzureDevOpsContext) // ToDo: Test throws OOM on Azure DevOps
            {
                builderCS.AddPaths("SyntaxWalker_InsufficientExecutionStackException.cs").VerifyNoIssues();
            }
        }

        [TestMethod]
        public void CognitiveComplexity_StackOverflow_VB()
        {
            if (!TestContextHelper.IsAzureDevOpsContext) // ToDO: Test throws OOM on Azure DevOps
            {
                builderVB.AddPaths("SyntaxWalker_InsufficientExecutionStackException.vb").VerifyNoIssues();
            }
        }
    }
}
