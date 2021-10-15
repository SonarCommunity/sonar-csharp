﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MethodsShouldUseBaseTypesTest
    {
        [TestMethod]
        public void MethodsShouldUseBaseTypes_Internals()
        {
            var solution = SolutionBuilder.Create()
                .AddProject(AnalyzerLanguage.CSharp)
                .AddSnippet(@"
internal interface IFoo
{
    bool IsFoo { get; }
}

public class Foo : IFoo
{
    public bool IsFoo { get; set; }
}
")              .GetSolution()
                .AddProject(AnalyzerLanguage.CSharp)
                .AddProjectReference(sln => sln.ProjectIds[0])
                .AddSnippet(@"
internal class Bar
{
    public void MethodOne(Foo foo)
    {
        var x = foo.IsFoo;
    }
}
")              .GetSolution();

            foreach (var compilation in solution.Compile())
            {
                DiagnosticVerifier.Verify(compilation, new MethodsShouldUseBaseTypes(), CompilationErrorBehavior.FailTest);
            }
        }

        [TestMethod]
        public void MethodsShouldUseBaseTypes() =>
            Verifier.VerifyAnalyzer(new[] { @"TestCases\MethodsShouldUseBaseTypes.cs", @"TestCases\MethodsShouldUseBaseTypes2.cs", }, new MethodsShouldUseBaseTypes());

#if NET
        [TestMethod]
        public void MethodsShouldUseBaseTypes_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\MethodsShouldUseBaseTypes.CSharp9.cs", new MethodsShouldUseBaseTypes());

        [TestMethod]
        public void MethodsShouldUseBaseTypes_CSharp10() =>
            Verifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\MethodsShouldUseBaseTypes.CSharp10.cs", new MethodsShouldUseBaseTypes());
#endif

        [TestMethod]
        public void MethodsShouldUseBaseTypes_InvalidCode() =>
            Verifier.VerifyCSharpAnalyzer(@"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Foo
{
    private void FooBar(IList<int> , IList<string>)
    {
        a.ToList();
    }

    // New test case - code doesn't compile but was making analyzer crash
    private void Foo(IList<int> a, IList<string> a)
    {
        a.ToList();
    }
}", new MethodsShouldUseBaseTypes(), CompilationErrorBehavior.Ignore);
    }
}
