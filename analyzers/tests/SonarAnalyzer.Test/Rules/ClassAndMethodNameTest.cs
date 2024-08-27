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
    public class ClassAndMethodNameTest
    {
        private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.ClassAndMethodName>();

        [TestMethod]
        public void ClassAndMethodName_CS() =>
            builderCS.AddPaths("ClassAndMethodName.cs", "ClassAndMethodName.Partial.cs")
                .AddReferences(MetadataReferenceFacade.NetStandard21)
                .WithOptions(ParseOptionsHelper.FromCSharp8)
                .Verify();

        [TestMethod]
        public void ClassAndMethodName_InTestProject_CS() =>
            builderCS.AddPaths("ClassAndMethodName.Tests.cs").AddTestReference().WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

#if NET

        [TestMethod]
        public void ClassAndMethodName_TopLevelStatement_CS() =>
            builderCS.AddPaths("ClassAndMethodName.TopLevelStatement.cs").WithTopLevelStatements().Verify();

        [TestMethod]
        public void ClassAndMethodName_TopLevelStatement_InTestProject_CS() =>
            builderCS.AddPaths("ClassAndMethodName.TopLevelStatement.Test.cs").AddTestReference().WithTopLevelStatements().Verify();

        [TestMethod]
        public void ClassAndMethodName_Record_CS() =>
            builderCS.AddPaths("ClassAndMethodName.Record.cs").WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void ClassAndMethodName_Record_InTestProject_CS() =>
            builderCS.AddPaths("ClassAndMethodName.Record.cs").AddTestReference().WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void ClassAndMethodName_RecordStruct_CS() =>
            builderCS.AddPaths("ClassAndMethodName.RecordStruct.cs").WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

        [TestMethod]
        public void ClassAndMethodName_RecordStruct_InTestProject_CS() =>
            builderCS.AddPaths("ClassAndMethodName.RecordStruct.cs").AddTestReference().WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

        [TestMethod]
        public void ClassAndMethodName_MethodName_CSharp9() =>
            builderCS.AddPaths("ClassAndMethodName.MethodName.CSharp9.cs").WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void ClassAndMethodName_MethodName_InTestProject_CSharp9() =>
            builderCS.AddPaths("ClassAndMethodName.MethodName.CSharp9.cs").AddTestReference().WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void ClassAndMethodName_MethodName_CSharp11() =>
            builderCS.AddPaths("ClassAndMethodName.MethodName.CSharp11.cs").WithOptions(ParseOptionsHelper.FromCSharp11).Verify();

#endif

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void ClassAndMethodName_VB(ProjectType projectType) =>
            new VerifierBuilder<VB.ClassName>().AddPaths("ClassAndMethodName.vb").AddReferences(TestHelper.ProjectTypeReference(projectType)).Verify();

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void ClassAndMethodName_MethodName(ProjectType projectType) =>
            builderCS.AddPaths("ClassAndMethodName.MethodName.cs", "ClassAndMethodName.MethodName.Partial.cs")
                .AddReferences(TestHelper.ProjectTypeReference(projectType))
                .WithOptions(ParseOptionsHelper.FromCSharp8).Verify();

        [DataTestMethod]
        [DataRow("foo", "foo")]
        [DataRow("Foo", "Foo")]
        [DataRow("FFF", "FFF")]
        [DataRow("FfF", "Ff", "F")]
        [DataRow("Ff9F", "Ff", "9", "F")]
        [DataRow("你好", "你", "好")]
        [DataRow("FFf", "F", "Ff")]
        [DataRow("")]
        [DataRow("FF9d", "FF", "9", "d")]
        [DataRow("y2x5__w7", "y", "2", "x", "5", "_", "_", "w", "7")]
        [DataRow("3%c#account", "3", "%", "c", "#", "account")]
        public void TestSplitToParts(string name, params string[] expectedParts) =>
            CS.ClassAndMethodName.SplitToParts(name).Should().BeEquivalentTo(expectedParts);
    }
}
