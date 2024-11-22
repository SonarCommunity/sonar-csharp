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

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class NonAsyncTaskShouldNotReturnNullTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<CS.NonAsyncTaskShouldNotReturnNull>();

    [TestMethod]
    public void NonAsyncTaskShouldNotReturnNull_CS() =>
        builder.AddPaths("NonAsyncTaskShouldNotReturnNull.cs").WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

#if NET
    [TestMethod]
    public void NonAsyncTaskShouldNotReturnNull__CS_Latest() =>
        builder
            .AddPaths("NonAsyncTaskShouldNotReturnNull.Latest.cs")
            .AddPaths("NonAsyncTaskShouldNotReturnNull.Latest.Partial.cs")
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();
#endif

    [TestMethod]
    public void NonAsyncTaskShouldNotReturnNull_VB() =>
        new VerifierBuilder<VB.NonAsyncTaskShouldNotReturnNull>().AddPaths("NonAsyncTaskShouldNotReturnNull.vb").Verify();
}
