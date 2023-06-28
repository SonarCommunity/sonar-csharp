﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules;

[TestClass]
public class DateAndTimeShouldNotBeUsedAsTypeForPrimaryKeyTest
{
    private readonly VerifierBuilder verifierCS = CreateVerifier<DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey>();

    [TestMethod]
    public void DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey_CS() =>
        verifierCS.AddPaths("DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey.cs").Verify();

    [TestMethod]
    public void DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey_CSharp9() =>
        verifierCS
            .AddPaths("DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey.CSharp9.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp9)
            .Verify();

    [TestMethod]
    public void DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey_NoReferenceToEntityFramework_CS() =>
        new VerifierBuilder<DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey>().AddPaths("DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey.NoReferenceToEntityFramework.cs").Verify();

#if NET

    [TestMethod]
    public void DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey_EntityFrameworkCore_CS() =>
        verifierCS.AddPaths("DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey.EntityFrameworkCore.cs").Verify();

    [TestMethod]
    public void DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey_FluentApi_CS() =>
        verifierCS.AddPaths("DateAndTimeShouldNotBeUsedAsTypeForPrimaryKey.FluentApi.cs").Verify();

#endif

    private static VerifierBuilder CreateVerifier<TAnalyzer>()
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        new VerifierBuilder<TAnalyzer>()
            .AddReferences(NuGetMetadataReference.SystemComponentModelAnnotations())

#if NET

            .AddReferences(NuGetMetadataReference.MicrosoftEntityFrameworkCore("7.0.0"))
            .AddReferences(NuGetMetadataReference.MicrosoftEntityFrameworkCoreAbstractions("7.0.0"));

#else

            .AddReferences(NuGetMetadataReference.MicrosoftEntityFramework("6.0.0"));

#endif

}
