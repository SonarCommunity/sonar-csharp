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

using SonarAnalyzer.SymbolicExecution.Constraints;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void Binary_BoolOperands_Equals_CS()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue == true)
    Tag(""True"");
else
    Tag(""True Unreachable"");

if (isFalse == false)
    Tag(""False"");
else
    Tag(""False Unreachable"");

if (isTrue == isFalse)
    Tag(""Variables Unreachable"");
else
    Tag(""Variables"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_Equals_VB()
        {
            const string code = @"
Dim IsTrue = True
Dim IsFalse = False

If IsTrue = True Then
    Tag(""True"")
Else
    Tag(""True Unreachable"")
End If

If IsFalse = False Then
    Tag(""False"")
Else
    Tag(""False Unreachable"")
End If

If IsTrue = IsFalse Then
    Tag(""Variables Unreachable"")
Else
    Tag(""Variables"")
End If";
            SETestContext.CreateVB(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_NotEquals_CS()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue != true)
    Tag(""True Unreachable"");
else
    Tag(""True"");

if (isFalse != false)
    Tag(""False Unreachable"");
else
    Tag(""False"");

if (isTrue != isFalse)
    Tag(""Variables"");
else
    Tag(""Variables Unreachable"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_NotEquals_VB()
        {
            const string code = @"
Dim IsTrue = True
Dim IsFalse = False

If IsTrue <> True Then
    Tag(""True Unreachable"")
Else
    Tag(""True"")
End If

If IsFalse <> False Then
    Tag(""False Unreachable"")
Else
    Tag(""False"")
End If

If IsTrue <> IsFalse Then
    Tag(""Variables"")
Else
    Tag(""Variables Unreachable"")
End If";
            SETestContext.CreateVB(code).Validator.ValidateTagOrder(
                "True",
                "False",
                "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_And()
        {
            const string code = """
                var isTrue = true;
                var isFalse = false;

                if (isTrue & true)
                    Tag("True & True");
                else
                    Tag("True & True Unreachable");

                if (false & isTrue)
                    Tag("False & True Unreachable");
                else
                    Tag("False & True");

                if (false & isFalse)
                    Tag("False & False Unreachable");
                else
                    Tag("False & False");

                if (isFalse & arg)
                    Tag("isFalse & arg True Unreachable");
                else
                    Tag("isFalse & arg False");

                if (arg & isFalse)
                    Tag("arg & isFalse True Unreachable");
                else
                    Tag("arg & isFalse False");

                if (isTrue & arg)
                    Tag("isTrue & arg True");
                else
                    Tag("isTrue & arg False");

                if (arg & isTrue)
                    Tag("arg & isTrue True");
                else
                    Tag("arg & isTrue False");

                if (isTrue && true)
                    Tag("True && True");
                else
                    Tag("True && True Unreachable");

                if (isFalse && true)
                    Tag("False && True Unreachable");
                else
                    Tag("False && True");
                """;
            SETestContext.CreateCS(code, ", bool arg").Validator.ValidateTagOrder(
                "True & True",
                "False & True",
                "False & False",
                "isFalse & arg False",
                "arg & isFalse False",
                "isTrue & arg True",
                "isTrue & arg False",
                "arg & isTrue True",
                "arg & isTrue False",
                "True && True",
                "False && True");
        }

        [TestMethod]
        public void Binary_BoolOperands_Or()
        {
            const string code = """
                var isTrue = true;
                var isFalse = false;

                if (isTrue | true)
                    Tag("True | True");
                else
                    Tag("True | True Unreachable");

                if (false | isTrue)
                    Tag("False | True");
                else
                    Tag("False | True Unreachable");

                if (false | isFalse)
                    Tag("False | False Unreachable");
                else
                    Tag("False | False");

                if (isTrue | arg)
                    Tag("isTrue | arg True");
                else
                    Tag("isTrue | arg False Unreachable");

                if (arg | isTrue)
                    Tag("arg | isTrue True");
                else
                    Tag("arg | isTrue False Unreachable");

                if (isFalse | arg)
                    Tag("isFalse | arg True");
                else
                    Tag("isFalse | arg False");

                if (arg | isFalse)
                    Tag("arg | isFalse True");
                else
                    Tag("arg | isFalse False");

                if (isTrue || true)
                    Tag("True || True");
                else
                    Tag("True || True Unreachable");

                if (isFalse || true)
                    Tag("False || True");
                else
                    Tag("False || True Unreachable");
                """;
            SETestContext.CreateCS(code, ", bool arg").Validator.ValidateTagOrder(
                "True | True",
                "False | True",
                "False | False",
                "isTrue | arg True",
                "arg | isTrue True",
                "isFalse | arg True",
                "isFalse | arg False",
                "arg | isFalse True",
                "arg | isFalse False",
                "True || True",
                "False || True");
        }

        [TestMethod]
        public void Binary_BoolOperands_Xor()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue ^ true)
    Tag(""True ^ True Unreachable"");
else
    Tag(""True ^ True"");

if (false ^ isTrue)
    Tag(""False ^ True"");
else
    Tag(""False ^ True Unreachable"");

if (isTrue ^ false)
    Tag(""True ^ False"");
else
    Tag(""True ^ False Unreachable"");

if (false ^ isFalse)
    Tag(""False ^ False Unreachable"");
else
    Tag(""False ^ False"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True ^ True", "False ^ True", "True ^ False", "False ^ False");
        }

        [DataTestMethod]
        [DataRow("boolParameter & isTrue")]
        [DataRow("isTrue & boolParameter")]
        public void Binary_NoConstraint_VisitsBothBranches(string condition)
        {
            var code = $@"
bool isTrue = true;
if ({condition})
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [DataTestMethod]
        [DataRow("boolParameter & isTrue")]
        [DataRow("isTrue & boolParameter")]
        public void Binary_OtherConstraint_VisitsBothBranches(string condition)
        {
            var code = $@"
bool isTrue = true;
if ({condition})
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            var check = new PostProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            SETestContext.CreateCS(code, check).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [TestMethod]
        public void Binary_UnexpectedOperator_VisitsBothBranches()
        {
            var code = $@"
if (a > b)      // Both, 'a' and 'b' have bool constraint (weird) and we do not produce bool constraint for '>' binary operator, because it doesn't make sense.
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            var check = new PostProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(BoolConstraint.True));
            SETestContext.CreateCS(code, ", int a, int b", check).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [TestMethod]
        public void BinaryEqualsNull_SetsBoolConstraint_KnownResult_CS()
        {
            const string code = @"
object nullValue = null;
object notNullValue = new object();
var isTrue = nullValue == null;
var isFalse = notNullValue == null;
var forNullNull = null == null;
var forNullSymbol = null == nullValue;
var forSymbolSymbolTrue = nullValue == nullValue;
var forSymbolSymbolFalse = notNullValue == nullValue;
var forSymbolSymbolNone = notNullValue == notNullValue;
Tag(""IsTrue"", isTrue);
Tag(""IsFalse"", isFalse);
Tag(""ForNullNull"", forNullNull);
Tag(""ForNullSymbol"", forNullSymbol);
Tag(""ForSymbolSymbolTrue"", forSymbolSymbolTrue);
Tag(""ForSymbolSymbolFalse"", forSymbolSymbolFalse);
Tag(""ForSymbolSymbolNone"", forSymbolSymbolNone);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.ValidateTag("IsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("IsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("ForNullNull", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());  // null == null is Literal with constant value 'true'
            validator.ValidateTag("ForNullSymbol", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolNone", x => x.HasConstraint<BoolConstraint>().Should().BeFalse("We can't tell if two instances are equivalent."));
        }

        [DataTestMethod]
        [DataRow("arg == null")]
        [DataRow("arg == nullValue")]
        public void BinaryEqualsNull_SetsBoolConstraint_Unknown_ComparedToNull_CS(string expression)
        {
            var code = @$"
object nullValue = null;
var value = {expression};
Tag(""End"");";
            var validator = SETestContext.CreateCS(code, ", object arg").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.Null).Any(x => x.Name == "arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "arg"));
        }

        [TestMethod]
        public void BinaryEqualsNull_SetsBoolConstraint_Unknown_ComparedToNotNull_CS()
        {
            const string code = @"
object notNullValue = new object();
var value = arg == notNullValue;
Tag(""End"");";
            var validator = SETestContext.CreateCS(code, ", object arg").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)    // When False, we can't tell what constraints "args" have
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).All(x => x.Name != "arg"));
        }

        [TestMethod]
        public void BinaryEqualsNull_SetsBoolConstraint_KnownResult_VB()
        {
            const string code = @"
Dim NullValue As Object = Nothing
Dim NotNullValue As New Object()
Dim IsTrue As Boolean = NullValue Is Nothing
Dim IsFalse = NotNullValue Is Nothing
Dim EqualsTrue As Boolean = NullValue = Nothing
Dim EqualsFalse = NotNullValue = Nothing
Tag(""IsTrue"", IsTrue)
Tag(""IsFalse"", IsFalse)
Tag(""EqualsTrue"", EqualsTrue)
Tag(""EqualsFalse"", EqualsFalse)";
            var validator = SETestContext.CreateVB(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.ValidateTag("IsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("IsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("EqualsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("EqualsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow("Arg Is Nothing")]
        [DataRow("Arg = Nothing")]
        public void BinaryEqualsNull_SetsBoolConstraint_Unknown_ComparedToNull_VB(string expression)
        {
            var code = @$"
Dim Value = {expression}
Tag(""End"")";
            var validator = SETestContext.CreateVB(code, ", Arg As Object").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "Value") && state.SymbolsWith(ObjectConstraint.Null).Any(x => x.Name == "Arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "Value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "Arg"));
        }

        [TestMethod]
        public void BinaryNotEqualsNull_SetsBoolConstraint_KnownResult_CS()
        {
            const string code = @"
object nullValue = null;
object notNullValue = new object();
var isTrue = notNullValue != null;
var isFalse = nullValue != null;
var forNullNull = null != null;
var forNullSymbol = null != nullValue;
var forSymbolSymbolTrue = notNullValue != nullValue;
var forSymbolSymbolFalse = nullValue != nullValue;
var forSymbolSymbolNone = notNullValue != notNullValue;
Tag(""IsTrue"", isTrue);
Tag(""IsFalse"", isFalse);
Tag(""ForNullNull"", forNullNull);
Tag(""ForNullSymbol"", forNullSymbol);
Tag(""ForSymbolSymbolTrue"", forSymbolSymbolTrue);
Tag(""ForSymbolSymbolFalse"", forSymbolSymbolFalse);
Tag(""ForSymbolSymbolNone"", forSymbolSymbolNone);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.ValidateTag("IsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("IsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("ForNullNull", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());  // null != null is Literal with constant value 'false'
            validator.ValidateTag("ForNullSymbol", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("ForSymbolSymbolNone", x => x.HasConstraint<BoolConstraint>().Should().BeFalse("We can't tell if two instances are equivalent."));
        }

        [DataTestMethod]
        [DataRow("arg != null")]
        [DataRow("arg != nullValue")]

        public void BinaryNotEqualsNull_SetsBoolConstraint_ComparedToNull_CS(string expression)
        {
            var code = @$"
object nullValue = null;
var value = {expression};
Tag(""End"");";
            var validator = SETestContext.CreateCS(code, ", object arg").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.Null).Any(x => x.Name == "arg"));
        }

        [TestMethod]
        public void BinaryNotEqualsNull_SetsBoolConstraint_ComparedToNotNull_CS()
        {
            const string code = @"
object notNullValue = new object();
var value = arg != notNullValue;
Tag(""End"");";
            var validator = SETestContext.CreateCS(code, ", object arg").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)    // When True, we can't tell what constraints "args" have
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).All(x => x.Name != "arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "arg"));
        }

        [TestMethod]
        public void BinaryNotEqualsNull_SetsBoolConstraint_KnownResult_VB()
        {
            const string code = @"
Dim NullValue As Object = Nothing
Dim NotNullValue As New Object()
Dim IsTrue = NotNullValue IsNot Nothing
Dim IsFalse As Boolean = NullValue IsNot Nothing
Dim EqualsTrue = NotNullValue <> Nothing
Dim EqualsFalse As Boolean = NullValue <> Nothing
Tag(""IsTrue"", IsTrue)
Tag(""IsFalse"", IsFalse)
Tag(""EqualsTrue"", EqualsTrue)
Tag(""EqualsFalse"", EqualsFalse)";
            var validator = SETestContext.CreateVB(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.ValidateTag("IsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("IsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.ValidateTag("EqualsTrue", x => x.HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.ValidateTag("EqualsFalse", x => x.HasConstraint(BoolConstraint.False).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow("Arg IsNot Nothing")]
        [DataRow("Arg <> Nothing")]
        public void BinaryNotEqualsNull_SetsBoolConstraint_Unknown_ComparedToNull_VB(string expression)
        {
            var code = @$"
Dim Value = {expression}
Tag(""End"")";
            var validator = SETestContext.CreateVB(code, ", Arg As Object").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.TagStates("End").Should().HaveCount(2)
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.True).Any(x => x.Name == "Value") && state.SymbolsWith(ObjectConstraint.NotNull).Any(x => x.Name == "Arg"))
                .And.ContainSingle(state => state.SymbolsWith(BoolConstraint.False).Any(x => x.Name == "Value") && state.SymbolsWith(ObjectConstraint.Null).Any(x => x.Name == "Arg"));
        }

        [DataTestMethod]
        [DataRow("arg >= null")]
        [DataRow("arg > null")]
        [DataRow("arg < null")]
        [DataRow("arg <= null")]
        [DataRow("null >= arg")]
        [DataRow("null > arg")]
        [DataRow("null < arg")]
        [DataRow("null <= arg")]
        [DataRow("arg > (int?)null")]
        [DataRow("arg > new Nullable<int>()")]
        [DataRow("arg > (null as int?)")]
        [DataRow("arg > nullValue")]
        [DataRow("nullValue > arg")]
        [DataRow("nullValue > 42")]
        [DataRow("nullValue > notNullValue")]
        [DataRow("notNullValue > nullValue")]
        public void Binary_NullableRelationalNull_SetsBoolConstraint_CS(string expression)
        {
            var code = $$"""
                int? notNullValue = 42;
                int? nullValue = null;
                var value = {{expression}};
                Tag("Value", value);
                if (value)
                {
                    Tag("If_Unreachable");
                }
                else
                {
                    Tag("Else");
                }
                Tag("End");
                """;

            var validator = SETestContext.CreateCS(code, ", int? arg").Validator;
            validator.ValidateContainsOperation(OperationKind.Binary);
            validator.ValidateTagOrder("Value", "Else", "End");
        }
    }
}
