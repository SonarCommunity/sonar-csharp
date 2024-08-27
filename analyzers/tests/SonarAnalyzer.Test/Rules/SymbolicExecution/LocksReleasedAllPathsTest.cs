/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

using Checks = SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class LocksReleasedAllPathsTest
    {
        private readonly VerifierBuilder builderCS = CreateVerifier(() => new CS.SymbolicExecutionRunner(AnalyzerConfiguration.AlwaysEnabled), Checks.CSharp.LocksReleasedAllPaths.S2222)
                                                        .WithOptions(ParseOptionsHelper.FromCSharp8);
        private readonly VerifierBuilder builderVB = CreateVerifier(() => new VB.SymbolicExecutionRunner(), Checks.VisualBasic.LocksReleasedAllPaths.S2222);

        [TestMethod]
        public void LocksReleasedAllPaths_CS() =>
            builderCS.AddPaths(
                "LocksReleasedAllPaths.Monitor.Conditions.cs",
                "LocksReleasedAllPaths.Monitor.GoTo.cs",
                "LocksReleasedAllPaths.Monitor.Linear.cs",
                "LocksReleasedAllPaths.Monitor.Loops.cs",
                "LocksReleasedAllPaths.Monitor.TryCatch.cs",
                "LocksReleasedAllPaths.Monitor.TryEnter.cs",
                "LocksReleasedAllPaths.Mutex.cs",
                "LocksReleasedAllPaths.ReaderWriterLock.cs",
                "LocksReleasedAllPaths.ReaderWriterLockSlim.cs",
                "LocksReleasedAllPaths.SpinLock.cs")
                .Verify();

        [TestMethod]
        public void LocksReleasedAllPaths_VB() =>
            builderVB.AddPaths(
                "LocksReleasedAllPaths.Monitor.Conditions.vb",
                "LocksReleasedAllPaths.Monitor.GoTo.vb",
                "LocksReleasedAllPaths.Monitor.Linear.vb",
                "LocksReleasedAllPaths.Monitor.Loops.vb",
                "LocksReleasedAllPaths.Monitor.TryCatch.vb",
                "LocksReleasedAllPaths.Monitor.TryEnter.vb",
                "LocksReleasedAllPaths.Mutex.vb",
                "LocksReleasedAllPaths.ReaderWriterLock.vb",
                "LocksReleasedAllPaths.ReaderWriterLockSlim.vb",
                "LocksReleasedAllPaths.SpinLock.vb")
                .WithOptions(ParseOptionsHelper.FromVisualBasic14)
                .Verify();

        [TestMethod]
        public void LocksReleasedAllPaths_CSharp8() =>
            builderCS.AddPaths("LocksReleasedAllPaths.Monitor.Conditions.CSharp8.cs").WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

#if NETFRAMEWORK

        [TestMethod]
        public void LocksReleasedAllPaths_CS_NetFx() =>
            builderCS.AddPaths("LocksReleasedAllPaths.Mutex.NetFx.cs").WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

#endif

#if NET

        [TestMethod]
        public void LocksReleasedAllPaths_CSharp11() =>
            builderCS.AddPaths("LocksReleasedAllPaths.Monitor.Conditions.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();

#endif

        private static VerifierBuilder CreateVerifier(Func<DiagnosticAnalyzer> createConfiguredAnalyzer, DiagnosticDescriptor onlyDiagnostics) =>
            new VerifierBuilder()
            .AddAnalyzer(createConfiguredAnalyzer)
            .WithOnlyDiagnostics(onlyDiagnostics)
            .AddReferences(MetadataReferenceFacade.SystemThreading)
            .WithBasePath(@"SymbolicExecution\Roslyn");
    }
}
