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

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class InheritedCollidingInterfaceMembersTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<InheritedCollidingInterfaceMembers>();

        [TestMethod]
        public void InheritedCollidingInterfaceMembers() =>
            builder.AddPaths("InheritedCollidingInterfaceMembers.cs").Verify();

        [TestMethod]
        public void InheritedCollidingInterfaceMembers_CSharp8() =>
            builder.AddPaths("InheritedCollidingInterfaceMembers.CSharp8.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .AddReferences(MetadataReferenceFacade.NetStandard21)
                .Verify();

#if NET

        [TestMethod]
        public void InheritedCollidingInterfaceMembers_CSharp11() =>
            builder.AddPaths("InheritedCollidingInterfaceMembers.CSharp11.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp11)
                .Verify();

#endif

    }
}
