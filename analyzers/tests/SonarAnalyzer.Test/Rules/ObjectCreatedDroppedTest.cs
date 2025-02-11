﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
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

using SonarAnalyzer.CSharp.Rules;

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class ObjectCreatedDroppedTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<ObjectCreatedDropped>();

    [TestMethod]
    public void ObjectCreatedDropped() =>
        builder.AddPaths("ObjectCreatedDropped.cs").Verify();

    [TestMethod]
    public void ObjectCreatedDropped_CSharp9() =>
        builder
            .WithOptions(LanguageOptions.FromCSharp9)
            .AddPaths("ObjectCreatedDropped.CSharp9.cs")
            .Verify();

    [TestMethod]
    public void ObjectCreatedDropped_InTest() =>
       builder.AddPaths("ObjectCreatedDropped.cs")
       .AddTestReference()
       .VerifyNoIssues();
}
