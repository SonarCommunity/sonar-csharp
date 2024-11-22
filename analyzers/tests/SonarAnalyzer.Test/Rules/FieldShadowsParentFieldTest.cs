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
    public class FieldShadowsParentFieldTest
    {
        private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.FieldShadowsParentField>();
        private readonly VerifierBuilder builderVB = new VerifierBuilder<VB.FieldShadowsParentField>();

        [TestMethod]
        public void FieldShadowsParentField_CS() =>
            builderCS.AddPaths("FieldShadowsParentField.cs").Verify();

        [TestMethod]
        public void FieldShadowsParentField_VB() =>
            builderVB.AddPaths("FieldShadowsParentField.vb").Verify();

        [TestMethod]
        public void FieldShadowsParentField_DoesNotRaiseIssuesForTestProject_CS() =>
            builderCS.AddPaths("FieldShadowsParentField.cs")
                .AddTestReference()
                .VerifyNoIssues();

        [TestMethod]
        public void FieldShadowsParentField_DoesNotRaiseIssuesForTestProject_VB() =>
            builderVB.AddPaths("FieldShadowsParentField.vb")
                .AddTestReference()
                .VerifyNoIssues();

#if NET

        [TestMethod]
        public void FieldShadowsParentField_CSharp9() =>
            builderCS.AddPaths("FieldShadowsParentField.CSharp9.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp9)
                .Verify();

        [TestMethod]
        public void FieldsShouldNotDifferByCapitalization_CShar9() =>
            builderCS.AddPaths("FieldsShouldNotDifferByCapitalization.CSharp9.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp9)
                .Verify();

#endif

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void FieldsShouldNotDifferByCapitalization_CS(ProjectType projectType) =>
            builderCS.AddPaths("FieldsShouldNotDifferByCapitalization.cs")
                .AddReferences(TestHelper.ProjectTypeReference(projectType))
                .Verify();

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void FieldsShouldNotDifferByCapitalization_VB(ProjectType projectType) =>
            builderVB.AddPaths("FieldsShouldNotDifferByCapitalization.vb")
                .AddReferences(TestHelper.ProjectTypeReference(projectType))
                .Verify();
    }
}
