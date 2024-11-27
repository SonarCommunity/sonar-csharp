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
public class CollectionPropertiesShouldBeReadOnlyTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<CollectionPropertiesShouldBeReadOnly>().AddReferences(MetadataReferenceFacade.SystemRuntimeSerialization);

    [TestMethod]
    public void CollectionPropertiesShouldBeReadOnly() =>
        builder.AddPaths("CollectionPropertiesShouldBeReadOnly.cs")
            .Verify();

#if NET

    [TestMethod]
    public void CollectionPropertiesShouldBeReadOnly_CS_Latest() =>
        builder.AddPaths("CollectionPropertiesShouldBeReadOnly.Latest.cs")
            .WithTopLevelStatements()
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();

    [TestMethod]
    public void CollectionPropertiesShouldBeReadOnly_Razor() =>
        builder.AddPaths("CollectionPropertiesShouldBeReadOnly.razor", "CollectionPropertiesShouldBeReadOnly.razor.cs")
               .Verify();

#endif

}
