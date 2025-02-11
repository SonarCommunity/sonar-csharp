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
public class OperatorsShouldBeOverloadedConsistentlyTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<OperatorsShouldBeOverloadedConsistently>();

    [TestMethod]
    public void OperatorsShouldBeOverloadedConsistently() =>
        builder.AddPaths("OperatorsShouldBeOverloadedConsistently.cs").Verify();

#if NET

    [TestMethod]
    public void OperatorsShouldBeOverloadedConsistently_CSharp9() =>
        builder.AddPaths("OperatorsShouldBeOverloadedConsistently.CSharp9.cs").WithOptions(LanguageOptions.FromCSharp9).VerifyNoIssues();

    [TestMethod]
    public void OperatorsShouldBeOverloadedConsistently_CSharp11() =>
        builder.AddPaths("OperatorsShouldBeOverloadedConsistently.CSharp11.cs").WithOptions(LanguageOptions.FromCSharp11).VerifyNoIssues();

#endif

}
