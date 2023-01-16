﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class NativeMethodsShouldBeWrappedTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<NativeMethodsShouldBeWrapped>();

        [TestMethod]
        public void NativeMethodsShouldBeWrapped() =>
            builder.AddPaths("NativeMethodsShouldBeWrapped.cs").WithErrorBehavior(CompilationErrorBehavior.Ignore).Verify();

#if NET

        [TestMethod]
        public void NativeMethodsShouldBeWrapped_CSharp9() =>
            builder.AddPaths("NativeMethodsShouldBeWrapped.CSharp9.cs").WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void NativeMethodsShouldBeWrapped_CSharp10() =>
            builder.AddPaths("NativeMethodsShouldBeWrapped.CSharp10.cs").WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

        // NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs contains the code as generated by the SourceGenerator. To regenerate it:
        // * Take the code from NativeMethodsShouldBeWrapped.CSharp11.cs
        // * Copy it to a new .Net 7 project
        // * press F12 on any of the partial methods
        // * copy the result to NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs
        [TestMethod]
        public void NativeMethodsShouldBeWrapped_CSharp11() =>
            builder
                .AddPaths("NativeMethodsShouldBeWrapped.CSharp11.cs")
                .AddPaths("NativeMethodsShouldBeWrapped.CSharp11.SourceGenerator.cs")
                .WithOptions(ParseOptionsHelper.FromCSharp11)
                .WithConcurrentAnalysis(false)
                .Verify();

#endif

        [TestMethod]
        public void NativeMethodsShouldBeWrapped_InvalidCode() =>
            builder.AddSnippet(@"
public class InvalidSyntax
{
    extern public void Extern1
    extern public void Extern2;
    extern private void Extern3(int x);
    public void Wrapper
    {
        Extern3(x);
    }
    public void Wrapper(
    {
        Extern3(x);
    }
    public void Wrapper()
    {
        Extern3(x);
    }
}").WithErrorBehavior(CompilationErrorBehavior.Ignore).Verify();
    }
}
