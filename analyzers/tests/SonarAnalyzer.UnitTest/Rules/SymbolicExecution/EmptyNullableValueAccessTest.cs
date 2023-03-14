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

using SonarAnalyzer.Common;
using ChecksCS = SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks.CSharp;
using CS = SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules;

[TestClass]
public class EmptyNullableValueAccessTest
{
    private readonly VerifierBuilder sonar = new VerifierBuilder()
        .AddAnalyzer(() => new CS.SymbolicExecutionRunner(AnalyzerConfiguration.AlwaysEnabledWithSonarCfg))
        .WithBasePath(@"SymbolicExecution\Sonar")
        .WithOnlyDiagnostics(ChecksCS.EmptyNullableValueAccess.S3655);

    private readonly VerifierBuilder roslynCS = new VerifierBuilder()
        .AddAnalyzer(() => new CS.SymbolicExecutionRunner(AnalyzerConfiguration.AlwaysEnabled))
        .WithBasePath(@"SymbolicExecution\Roslyn")
        .WithOnlyDiagnostics(ChecksCS.EmptyNullableValueAccess.S3655);

    [DataTestMethod]
    [DataRow(ProjectType.Product)]
    [DataRow(ProjectType.Test)]
    public void EmptyNullableValueAccess_Sonar_CSharp8(ProjectType projectType) =>
        sonar.AddPaths("EmptyNullableValueAccess.cs")
            .AddReferences(TestHelper.ProjectTypeReference(projectType).Concat(MetadataReferenceFacade.NETStandard21))
            .WithOptions(ParseOptionsHelper.FromCSharp8)
            .Verify();

    [DataTestMethod]
    [DataRow(ProjectType.Product)]
    [DataRow(ProjectType.Test)]
    public void EmptyNullableValueAccess_Roslyn_CSharp8(ProjectType projectType) =>
        roslynCS.AddPaths("EmptyNullableValueAccess.cs")
            .AddReferences(TestHelper.ProjectTypeReference(projectType).Concat(MetadataReferenceFacade.NETStandard21))
            .WithOptions(ParseOptionsHelper.FromCSharp8)
            .Verify();

#if NET

    [TestMethod]
    public void EmptyNullableValueAccess_Sonar_CSharp9() =>
        sonar.AddPaths("EmptyNullableValueAccess.CSharp9.cs").WithTopLevelStatements().Verify();

    [TestMethod]
    public void EmptyNullableValueAccess_Roslyn_CSharp9() =>
        roslynCS.AddPaths("EmptyNullableValueAccess.CSharp9.cs").WithTopLevelStatements().Verify();

    [TestMethod]
    public void EmptyNullableValueAccess_Sonar_CSharp10() =>
        sonar.AddPaths("EmptyNullableValueAccess.CSharp10.cs").WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

    [TestMethod]
    public void EmptyNullableValueAccess_Roslyn_CSharp10() =>
        roslynCS.AddPaths("EmptyNullableValueAccess.CSharp10.cs").WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

#endif

    [TestMethod]
    public void EmptyNullableValueAccess_Sonar_CSharp11() =>
        sonar.AddPaths("EmptyNullableValueAccess.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();

    [TestMethod]
    public void EmptyNullableValueAccess_Roslyn_CSharp11() =>
        roslynCS.AddPaths("EmptyNullableValueAccess.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();
}
