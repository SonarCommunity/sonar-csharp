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

using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules
{
    [TestClass]
    public class StringLiteralShouldNotBeDuplicatedTest
    {
#if NET
        private static readonly ImmutableArray<MetadataReference> DapperReferences = [
            CoreMetadataReference.SystemDataCommon,
            CoreMetadataReference.SystemComponentModelPrimitives,
            ..NuGetMetadataReference.Dapper(),
            ..NuGetMetadataReference.SystemDataSqlClient()
        ];
#endif

        private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.StringLiteralShouldNotBeDuplicated>();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_CS() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.cs").Verify();

#if NET

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_CSharp9() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.CSharp9.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp9)
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_TopLevelStatements() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.TopLevelStatements.cs")
                .WithTopLevelStatements()
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_CSharp10() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.CSharp10.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp10)
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_CSharp11() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.CSharp11.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp11)
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_CS_Dapper() =>
            builderCS.AddPaths("StringLiteralShouldNotBeDuplicated.Dapper.cs")
                .AddReferences(DapperReferences)
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_VB_Dapper() =>
            new VerifierBuilder<VB.StringLiteralShouldNotBeDuplicated>()
                .AddPaths("StringLiteralShouldNotBeDuplicated.Dapper.vb")
                .AddReferences(DapperReferences)
                .Verify();

#endif

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_Attributes_CS() =>
            new VerifierBuilder().AddAnalyzer(() => new CS.StringLiteralShouldNotBeDuplicated { Threshold = 2 })
                .AddPaths("StringLiteralShouldNotBeDuplicated_Attributes.cs")
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_VB() =>
            new VerifierBuilder<VB.StringLiteralShouldNotBeDuplicated>()
                .AddPaths("StringLiteralShouldNotBeDuplicated.vb")
                .Verify();

        [TestMethod]
        public void StringLiteralShouldNotBeDuplicated_Attributes_VB() =>
            new VerifierBuilder().AddAnalyzer(() => new VB.StringLiteralShouldNotBeDuplicated() { Threshold = 2 })
                .AddPaths("StringLiteralShouldNotBeDuplicated_Attributes.vb")
                .WithConcurrentAnalysis(false)
                .Verify();
    }
}
