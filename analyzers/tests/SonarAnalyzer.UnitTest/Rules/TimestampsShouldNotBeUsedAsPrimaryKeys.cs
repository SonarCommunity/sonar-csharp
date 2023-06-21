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

using System.IO;
using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules;

[TestClass]
public class TimestampsShouldNotBeUsedAsPrimaryKeysTest
{
    [TestMethod]
    public void TimestampsShouldNotBeUsedAsPrimaryKeys_CSharp() =>
        CreateVerifier<TimestampsShouldNotBeUsedAsPrimaryKeys>("TimestampsShouldNotBeUsedAsPrimaryKeys.CSharp.cs").Verify();

    [TestMethod]
    public void TimestampsShouldNotBeUsedAsPrimaryKeys_CSharp9() =>
        CreateVerifier<TimestampsShouldNotBeUsedAsPrimaryKeys>("TimestampsShouldNotBeUsedAsPrimaryKeys.CSharp9.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp9)
            .Verify();

    [TestMethod]
    public void TimestampsShouldNotBeUsedAsPrimaryKeys_NoReferenceToEntityFramework_CSharp() =>
        new VerifierBuilder<TimestampsShouldNotBeUsedAsPrimaryKeys>()
            .AddPaths("TimestampsShouldNotBeUsedAsPrimaryKeys.NoReferenceToEntityFramework.CSharp.cs")
            .Verify();

#if NET
    [TestMethod]
    public void TimestampsShouldNotBeUsedAsPrimaryKeys_EntityFrameworkCore_CSharp() =>
        CreateVerifier<TimestampsShouldNotBeUsedAsPrimaryKeys>("TimestampsShouldNotBeUsedAsPrimaryKeys.EntityFrameworkCore.CSharp.cs").Verify();
#endif

    private static VerifierBuilder CreateVerifier<TAnalyzer>(string testFileName)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var builder = new VerifierBuilder<TAnalyzer>()
            .AddReferences(NuGetMetadataReference.SystemComponentModelAnnotations())

#if NET
            .AddReferences(NuGetMetadataReference.MicrosoftEntityFrameworkCore("7.0.0"))
            .AddReferences(NuGetMetadataReference.MicrosoftEntityFrameworkCoreAbstractions("7.0.0"));
        return AddTestFileWithExtraLinePrepended(builder, testFileName, "using Microsoft.EntityFrameworkCore;");
#else
            .AddReferences(NuGetMetadataReference.MicrosoftEntityFramework("6.0.0"));
        return AddTestFileWithExtraLinePrepended(builder, testFileName, "using System.Data.Entity;using ModelBuilder = System.Data.Entity.DbModelBuilder;");
#endif

    }

    private static VerifierBuilder AddTestFileWithExtraLinePrepended(VerifierBuilder builder, string testFileName, string extraLine)
    {
        var content = File.ReadAllText($@"TestCases\{testFileName}");
        return builder.AddSnippet($"{extraLine}{Environment.NewLine}{content}", testFileName);
    }
}
