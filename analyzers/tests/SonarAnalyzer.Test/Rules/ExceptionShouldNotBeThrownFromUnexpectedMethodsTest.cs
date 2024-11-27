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

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class ExceptionShouldNotBeThrownFromUnexpectedMethodsTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<ExceptionShouldNotBeThrownFromUnexpectedMethods>();

    [TestMethod]
    public void ExceptionShouldNotBeThrownFromUnexpectedMethods() =>
        builder.AddPaths("ExceptionShouldNotBeThrownFromUnexpectedMethods.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp8)
            .Verify();

#if NET

    [TestMethod]
    public void ExceptionShouldNotBeThrownFromUnexpectedMethods_CSharp9() =>
        builder.AddPaths("ExceptionShouldNotBeThrownFromUnexpectedMethods.CSharp9.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp9)
            .Verify();

    [TestMethod]
    public void ExceptionShouldNotBeThrownFromUnexpectedMethods_CSharp11() =>
        builder.AddPaths("ExceptionShouldNotBeThrownFromUnexpectedMethods.CSharp11.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp11)
            .Verify();

#endif

}
