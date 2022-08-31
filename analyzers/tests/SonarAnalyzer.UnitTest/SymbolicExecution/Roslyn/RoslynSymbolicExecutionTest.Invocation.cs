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

using SonarAnalyzer.SymbolicExecution.Constraints;
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void PreProcess_IsNullOrEmpty_ExplodesGraph_ValidateOrder()
        {
            const string code = @"var isNullOrEmpy = string.IsNullOrEmpty(arg);";

            var validator = SETestContext.CreateCS(code, ", string arg").Validator;
            validator.ValidateOrder(
"LocalReference: isNullOrEmpy = string.IsNullOrEmpty(arg) (Implicit)",
"ParameterReference: arg",
"Argument: arg",
"Invocation: string.IsNullOrEmpty(arg)",
"Invocation: string.IsNullOrEmpty(arg)",
"Invocation: string.IsNullOrEmpty(arg)",
"SimpleAssignment: isNullOrEmpy = string.IsNullOrEmpty(arg) (Implicit)",
"SimpleAssignment: isNullOrEmpy = string.IsNullOrEmpty(arg) (Implicit)",
"SimpleAssignment: isNullOrEmpy = string.IsNullOrEmpty(arg) (Implicit)");
        }

        [TestMethod]
        public void PreProcess_IsNullOrEmpty_Tags()
        {
            const string code = @"
var isNullOrEmpy = string.IsNullOrEmpty(arg);
Tag(""IsNullOrEmpy"", isNullOrEmpy);
Tag(""Arg"", arg);";

            var validator = SETestContext.CreateCS(code, ", string arg").Validator;
            validator.TagValues("IsNullOrEmpy").Should().Equal(
                new SymbolicValue().WithConstraint(BoolConstraint.True),
                new SymbolicValue().WithConstraint(BoolConstraint.True),
                new SymbolicValue().WithConstraint(BoolConstraint.False));
            validator.TagValues("Arg").Should().Equal(
                new SymbolicValue().WithConstraint(ObjectConstraint.Null),
                new SymbolicValue().WithConstraint(ObjectConstraint.NotNull),
                new SymbolicValue().WithConstraint(ObjectConstraint.NotNull));
        }
    }
}
