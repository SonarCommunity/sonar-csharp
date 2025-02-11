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

using Microsoft.CodeAnalysis.CSharp;
using SonarAnalyzer.CSharp.Rules;

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class NativeMethodsShouldBeWrappedTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<NativeMethodsShouldBeWrapped>();

    [TestMethod]
    public void NativeMethodsShouldBeWrapped() =>
        builder.AddPaths("NativeMethodsShouldBeWrapped.cs").Verify();

#if NET

    [TestMethod]
    public void NativeMethodsShouldBeWrapped_CSharp9() =>
        builder.AddPaths("NativeMethodsShouldBeWrapped.CSharp9.cs").WithTopLevelStatements().Verify();

    // NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs contains the code as generated by the SourceGenerator. To regenerate it:
    // * Take the code from NativeMethodsShouldBeWrapped.CSharp11.cs
    // * Copy it to a new .Net 7 project
    // * Press F12 on any of the partial methods
    // * Copy the result to NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs
    [TestMethod]
    public void NativeMethodsShouldBeWrapped_CSharp11() =>
        builder
            .AddPaths("NativeMethodsShouldBeWrapped.CSharp11.cs")
            .AddPaths("NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs")
            .WithOptions(LanguageOptions.FromCSharp11)
            .WithConcurrentAnalysis(false)
            .Verify();

#endif

    [TestMethod]
    public void NativeMethodsShouldBeWrapped_InvalidCode() =>
        builder.AddSnippet("""
            public class InvalidSyntax
            {
                extern public void Extern1  // Error [CS0670, CS0106, CS1002]
                extern public void Extern2; // Error [CS0670, CS0106]
                extern private void Extern3(int x);
                public void Wrapper         // Error [CS0547, CS0548]
                {
                    Extern3(x);             // Error [CS1014, CS1513, CS8124, CS1519]
                }
                public void Wrapper(        // Error [CS0106, CS8107, CS8803, CS8805, CS8112, CS1001]
                {
                    Extern3(x);             // Error [CS0246, CS1003, CS0246, CS8124, CS1001, CS1026, CS1001]
                }                           // Error [CS1022]
                public void Wrapper()       // Error [CS0106, CS0128]
                {
                    Extern3(x);             // Error [CS0103, CS0103]
                }
            }                               // Error [CS1022]
            """).WithLanguageVersion(LanguageVersion.CSharp7).Verify();
}
