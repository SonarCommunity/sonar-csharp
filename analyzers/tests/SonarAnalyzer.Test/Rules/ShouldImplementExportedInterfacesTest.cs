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
    public class ShouldImplementExportedInterfacesTest
    {
        private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.ShouldImplementExportedInterfaces>().AddReferences(MetadataReferenceFacade.SystemComponentModelComposition);

        [TestMethod]
        public void ShouldImplementExportedInterfaces_CS() =>
            builderCS.AddPaths("ShouldImplementExportedInterfaces.cs").Verify();

        [TestMethod]
        public void ShouldImplementExportedInterfaces_SystemComposition_CS() =>
            builderCS.AddPaths("ShouldImplementExportedInterfaces.System.Composition.cs").AddReferences(MetadataReferenceFacade.SystemCompositionAttributedModel).Verify();

        [TestMethod]
        public void ShouldImplementExportedInterfaces_Partial() =>
            builderCS.AddPaths("ShouldImplementExportedInterfaces_Part1.cs", "ShouldImplementExportedInterfaces_Part2.cs").Verify();

#if NET

        [TestMethod]
        public void ShouldImplementExportedInterfaces_CSharp9() =>
            builderCS.AddPaths("ShouldImplementExportedInterfaces.CSharp9.cs").WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

#endif

        [TestMethod]
        public void ShouldImplementExportedInterfaces_VB() =>
            new VerifierBuilder<VB.ShouldImplementExportedInterfaces>()
                .AddReferences(MetadataReferenceFacade.SystemComponentModelComposition)
                .AddPaths("ShouldImplementExportedInterfaces.vb")
                .Verify();
    }
}
