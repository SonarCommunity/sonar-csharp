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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class FieldShadowsParentFieldTest
    {
        [TestMethod]
        public void FieldShadowsParentField_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\FieldShadowsParentField.cs", new CS.FieldShadowsParentField());

        [TestMethod]
        public void FieldShadowsParentField_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\FieldShadowsParentField.vb", new VB.FieldShadowsParentField());

        [TestMethod]
        public void FieldShadowsParentField_DoesNotRaiseIssuesForTestProject_CS() =>
            OldVerifier.VerifyNoIssueReportedInTest(@"TestCases\FieldShadowsParentField.cs", new CS.FieldShadowsParentField());

        [TestMethod]
        public void FieldShadowsParentField_DoesNotRaiseIssuesForTestProject_VB() =>
            OldVerifier.VerifyNoIssueReportedInTest(@"TestCases\FieldShadowsParentField.vb", new VB.FieldShadowsParentField());

#if NET
        [TestMethod]
        public void FieldShadowsParentField_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\FieldShadowsParentField.CSharp9.cs", new CS.FieldShadowsParentField());

        [TestMethod]
        public void FieldsShouldNotDifferByCapitalization_CShar9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\FieldsShouldNotDifferByCapitalization.CSharp9.cs", new CS.FieldShadowsParentField());
#endif

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void FieldsShouldNotDifferByCapitalization_CS(ProjectType projectType) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\FieldsShouldNotDifferByCapitalization.cs", new CS.FieldShadowsParentField(), TestHelper.ProjectTypeReference(projectType));

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void FieldsShouldNotDifferByCapitalization_VB(ProjectType projectType) =>
            OldVerifier.VerifyAnalyzer(@"TestCases\FieldsShouldNotDifferByCapitalization.vb", new VB.FieldShadowsParentField(), TestHelper.ProjectTypeReference(projectType));
    }
}
