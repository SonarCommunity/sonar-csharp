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
    public class UnusedReturnValueTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<UnusedReturnValue>();

        [TestMethod]
        public void UnusedReturnValue() =>
            builder.AddPaths("UnusedReturnValue.cs").WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

        [TestMethod]
        public void UnusedReturnValueWithPartialClasses() =>
            builder.AddPaths("UnusedReturnValue.part1.cs", "UnusedReturnValue.part2.cs", "UnusedReturnValue.External.cs").WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

#if NET

        [TestMethod]
        public void UnusedReturnValue_CSharp9() =>
            builder.AddPaths("UnusedReturnValue.CSharp9.cs").WithTopLevelStatements().Verify();

        [TestMethod]
        public void UnusedReturnValue_CSharp10() =>
            builder.AddPaths("UnusedReturnValue.CSharp10.cs").WithTopLevelStatements().WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

        [TestMethod]
        public void UnusedReturnValue_CSharp11() =>
            builder.AddPaths("UnusedReturnValue.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();

#endif

    }
}
