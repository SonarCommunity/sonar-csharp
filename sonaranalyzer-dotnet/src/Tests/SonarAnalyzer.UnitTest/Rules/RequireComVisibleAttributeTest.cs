/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
using System;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class RequireComVisibleAttributeTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void RequireComVisibleAttribute()
        {
            Verifier.VerifyAnalyzer(@"TestCases\RequireComVisibleAttribute.cs",
                new RequireComVisibleAttribute());
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void RequireComVisibleAttributeNoncompliant()
        {
            Action action = () => Verifier.VerifyAnalyzer(@"TestCases\RequireComVisibleAttributeNoncompliant.cs", new RequireComVisibleAttribute());
            action.ShouldThrow<AssertFailedException>();
        }
    }
}
