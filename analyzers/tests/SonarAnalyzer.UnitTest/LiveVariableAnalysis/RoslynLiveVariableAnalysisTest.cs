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

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.CFG;
using SonarAnalyzer.CFG.LiveVariableAnalysis;
using SonarAnalyzer.CFG.Roslyn;

namespace SonarAnalyzer.UnitTest.LiveVariableAnalysis
{
    [TestClass]
    public partial class RoslynLiveVariableAnalysisTest
    {
        [TestMethod]
        public void WriteOnly()
        {
            var code = @"
int a = 1;
var b = Method();
var c = 2 + 3;";
            new Context(code).ValidateAllEmpty();
        }

        [TestMethod]
        public void ProcessParameterReference_LiveIn()
        {
            var code = @"
Method(intParameter);
IsMethod(boolParameter);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("intParameter", "boolParameter"), new LiveOut("intParameter", "boolParameter"));
            context.Validate(context.Block("Method(intParameter);"), new LiveIn("intParameter", "boolParameter"));
        }

        [TestMethod]
        public void ProcessParameterReference_UsedAsOutArgument_NotLiveIn_NotLiveOut()
        {
            var code = @"Main(true, 0, out outParameter, ref refParameter);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("refParameter"), new LiveOut("refParameter"));
            context.Validate(context.Block(code), new LiveIn("refParameter"));
        }

        [TestMethod]
        public void ProcessParameterReference_InNameOf_NotLiveIn_NotLiveOut()
        {
            var code = @"Method(nameof(intParameter));";
            new Context(code).ValidateAllEmpty();
        }

        [TestMethod]
        public void ProcessParameterReference_Assigned_NotLiveIn_LiveOut()
        {
            var code = @"
intParameter = 42;
if (boolParameter)
    return;
Method(intParameter);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(intParameter);"), new LiveIn("intParameter"));
        }

        [TestMethod]
        public void UsedAfterBranch_LiveOut()
        {
            /*       Binary
             *       /   \
             *    Jump   Simple
             *   return  Method()
             *       \   /
             *        Exit
             */
            var code = @"
if (boolParameter)
    return;
Method(intParameter);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter", "intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Block("Method(intParameter);"), new LiveIn("intParameter"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [DataTestMethod]
        [DataRow("Capturing(x => field + variable + intParameter);", DisplayName = "SimpleLambda")]
        [DataRow("Capturing((x) => field + variable + intParameter);", DisplayName = "ParenthesizedLambda")]
        [DataRow("Capturing(x => { System.Func<int> xxx = () => field + variable + intParameter; return xxx();});", DisplayName = "NestedLambda")]
        [DataRow("VoidDelegate d = delegate { return field + variable + intParameter;};", DisplayName = "AnonymousMethod")]
        [DataRow("var items = from xxx in new int[] { 42, 100 } where xxx > field + variable + intParameter select xxx;", DisplayName = "Query")]
        public void Captured_NotLiveIn_NotLiveOut(string capturingStatement)
        {
            /*       Entry
             *         |
             *       Block 1
             *       /   \
             *      |   Block 2
             *      |   Method()
             *       \   /
             *        Exit
             */
            var code = @$"
var variable = 42;
{capturingStatement}
if (boolParameter)
    return;
Method(field, variable, intParameter);";
            capturingStatement.Should().Contain("field + variable + intParameter");
            var context = new Context(code);
            var expectedCaptured = capturingStatement.Contains("xxx")
                ? new Captured("variable", "intParameter", "xxx")
                : new Captured("variable", "intParameter");
            context.Validate(context.Cfg.EntryBlock, expectedCaptured, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), expectedCaptured, new LiveIn("boolParameter"));
            context.Validate(context.Block("Method(field, variable, intParameter);"), expectedCaptured);
            context.Validate(context.Cfg.ExitBlock, expectedCaptured);
        }

        [TestMethod]
        public void Assigned_NotLiveIn_NotLiveOut()
        {
            /*       Entry
             *         |
             *       Block 1
             *       boolParameter
             *       /   \
             *      |   Block 2
             *      |   intParameter=0
             *       \   /
             *        Exit
             */
            var code = @"
if (boolParameter)
    return;
intParameter = 0;";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter"));
            context.Validate(context.Block("intParameter = 0;"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void LongPropagationChain_LiveIn_LiveOut()
        {
            /*    Entry
             *      |
             *    Block 1 -------+
             *    declare        |
             *      |            |
             *    Block 2 ------+|
             *    use & assign  ||
             *      |           ||
             *    Block 3 -----+||
             *    assign       |||
             *      |          vvv
             *    Block 4 ---> Exit
             *    use
             */
            var code = @"
var value = 0;
if (boolParameter)
    return;
Method(value);
value = 1;
if (boolParameter)
    return;
value = 42;
if (boolParameter)
    return;
Method(intParameter, value);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("value = 0"), new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter", "value"));
            context.Validate(context.Block("Method(value);"), new LiveIn("boolParameter", "intParameter", "value"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("value = 42;"), new LiveIn("boolParameter", "intParameter"), new LiveOut("value", "intParameter"));
            context.Validate(context.Block("Method(intParameter, value);"), new LiveIn("value", "intParameter"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void BranchedPropagationChain_LiveIn_LiveOut_CS()
        {
            /*              Binary
             *              boolParameter
             *              /           \
             *             /             \
             *            /               \
             *       Binary             Binary
             *       firstBranch        secondBranch
             *       /      \              /      \
             *      /        \            /        \
             *  Simple     Simple      Simple      Simple
             *  firstTrue  firstFalse  secondTrue  secondFalse
             *      \        /            \        /
             *       \      /              \      /
             *        Simple                Simple
             *        first                 second
             *             \               /
             *              \             /
             *                  Simple
             *                  reassigned
             *                  everywhere
             */
            var code = @"
var everywhere = 42;
var reassigned = 42;
var first = 42;
var firstTrue = 42;
var firstFalse = 42;
var second = 42;
var secondTrue = 42;
var secondFalse = 42;
var firstCondition = boolParameter;
var secondCondition = boolParameter;
if (boolParameter)
{
    if (firstCondition)
    {
        Method(firstTrue);
    }
    else
    {
        Method(firstFalse);
    }
    Method(first);
}
else
{
    if (secondCondition)
    {
        Method(secondTrue);
    }
    else
    {
        Method(secondFalse);
    }
    Method(second);
}
reassigned = 0;
Method(everywhere, reassigned);";
            var context = new Context(code);
            context.Validate(
                context.Block("everywhere = 42"),
                new LiveIn("boolParameter"),
                new LiveOut("everywhere", "firstCondition", "firstTrue", "firstFalse", "first", "secondCondition", "secondTrue", "secondFalse", "second"));
            // First block
            context.Validate(
                context.Block("firstCondition"),
                new LiveIn("everywhere", "firstCondition", "firstTrue", "firstFalse", "first"),
                new LiveOut("everywhere", "firstTrue", "firstFalse", "first"));
            context.Validate(
                context.Block("Method(firstTrue);"),
                new LiveIn("everywhere", "firstTrue", "first"),
                new LiveOut("everywhere", "first"));
            context.Validate(
                context.Block("Method(firstFalse);"),
                new LiveIn("everywhere", "firstFalse", "first"),
                new LiveOut("everywhere", "first"));
            context.Validate(
                context.Block("Method(first);"),
                new LiveIn("everywhere", "first"),
                new LiveOut("everywhere"));
            // Second block
            context.Validate(
                context.Block("secondCondition"),
                new LiveIn("everywhere", "secondCondition", "secondTrue", "secondFalse", "second"),
                new LiveOut("everywhere", "secondTrue", "secondFalse", "second"));
            context.Validate(
                context.Block("Method(secondTrue);"),
                new LiveIn("everywhere", "secondTrue", "second"),
                new LiveOut("everywhere", "second"));
            context.Validate(
                context.Block("Method(secondFalse);"),
                new LiveIn("everywhere", "secondFalse", "second"),
                new LiveOut("everywhere", "second"));
            context.Validate(
                context.Block("Method(second);"),
                new LiveIn("everywhere", "second"),
                new LiveOut("everywhere"));
            // Common end
            context.Validate(context.Block("Method(everywhere, reassigned);"), new LiveIn("everywhere"));
        }

        [TestMethod]
        public void BranchedPropagationChain_LiveIn_LiveOut_VB()
        {
            /*              Binary
             *              boolParameter
             *              /           \
             *             /             \
             *            /               \
             *       Binary             Binary
             *       firstBranch        secondBranch
             *       /      \              /      \
             *      /        \            /        \
             *  Simple     Simple      Simple      Simple
             *  firstTrue  firstFalse  secondTrue  secondFalse
             *      \        /            \        /
             *       \      /              \      /
             *        Simple                Simple
             *        first                 second
             *             \               /
             *              \             /
             *                  Simple
             *                  reassigned
             *                  everywhere
             */
            var code = @"
Dim Everywhere As Integer = 42
Dim Reassigned As Integer = 42
Dim First As Integer = 42
Dim FirstTrue As Integer = 42
Dim FirstFalse As Integer = 42
Dim Second As Integer = 42
Dim SecondTrue As Integer = 42
Dim SecondFalse As Integer = 42
Dim FirstCondition As Boolean = BoolParameter
Dim SecondCondition As Boolean = BoolParameter
If BoolParameter Then
    If (FirstCondition) Then
        Method(FirstTrue)
    Else
        Method(FirstFalse)
    End If
    Method(First)
Else
    If SecondCondition Then
        Method(SecondTrue)
    Else
        Method(SecondFalse)
    End If
    Method(Second)
End If
Reassigned = 0
Method(Everywhere, Reassigned)";
            var context = new Context(code, isCSharp: false);
            context.Validate(
                context.Block("Everywhere As Integer = 42"),
                new LiveIn("BoolParameter"),
                new LiveOut("Everywhere", "FirstCondition", "FirstTrue", "FirstFalse", "First", "SecondCondition", "SecondTrue", "SecondFalse", "Second"));
            // First block
            context.Validate(
                context.Block("FirstCondition"),
                new LiveIn("Everywhere", "FirstCondition", "FirstTrue", "FirstFalse", "First"),
                new LiveOut("Everywhere", "FirstTrue", "FirstFalse", "First"));
            context.Validate(
                context.Block("Method(FirstTrue)"),
                new LiveIn("Everywhere", "FirstTrue", "First"),
                new LiveOut("Everywhere", "First"));
            context.Validate(
                context.Block("Method(FirstFalse)"),
                new LiveIn("Everywhere", "FirstFalse", "First"),
                new LiveOut("Everywhere", "First"));
            context.Validate(
                context.Block("Method(First)"),
                new LiveIn("Everywhere", "First"),
                new LiveOut("Everywhere"));
            // Second block
            context.Validate(
                context.Block("SecondCondition"),
                new LiveIn("Everywhere", "SecondCondition", "SecondTrue", "SecondFalse", "Second"),
                new LiveOut("Everywhere", "SecondTrue", "SecondFalse", "Second"));
            context.Validate(
                context.Block("Method(SecondTrue)"),
                new LiveIn("Everywhere", "SecondTrue", "Second"),
                new LiveOut("Everywhere", "Second"));
            context.Validate(
                context.Block("Method(SecondFalse)"),
                new LiveIn("Everywhere", "SecondFalse", "Second"),
                new LiveOut("Everywhere", "Second"));
            context.Validate(
                context.Block("Method(Second)"),
                new LiveIn("Everywhere", "Second"),
                new LiveOut("Everywhere"));
            // Common end
            context.Validate(context.Block("Method(Everywhere, Reassigned)"), new LiveIn("Everywhere"));
        }

        [TestMethod]
        public void ProcessBlockInternal_EvaluationOrder_UsedBeforeAssigned_LiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(variable, variable = 42);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(variable, variable = 42);"), new LiveIn("variable"));
        }

        [TestMethod]
        public void ProcessBlockInternal_EvaluationOrder_UsedBeforeAssignedInSubexpression_LiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(1 + 1 + Method(variable), variable = 42);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(1 + 1 + Method(variable), variable = 42);"), new LiveIn("variable"));
        }

        [TestMethod]
        public void ProcessBlockInternal_EvaluationOrder_AssignedBeforeUsed_NotLiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(variable = 42, variable);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(variable = 42, variable);"));
        }

        [TestMethod]
        public void ProcessBlockInternal_EvaluationOrder_AssignedBeforeUsedInSubexpression_NotLiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(variable = 42, 1 + 1 + Method(variable));";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(variable = 42, 1 + 1 + Method(variable));"));
        }

        [TestMethod]
        public void ProcessLocalReference_InNameOf_NotLiveIn_NotLiveOut()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(nameof(variable));";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Method(nameof(variable));"));
        }

        [TestMethod]
        public void ProcessLocalReference_LocalScopeSymbol_LiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
Method(intParameter, variable);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(intParameter, variable);"), new LiveIn("intParameter", "variable"));
        }

        [TestMethod]
        public void ProcessLocalReference_ReassignedBeforeLastBlock_LiveIn()
        {
            var code = @"
var variable = 0;
variable = 42;
if (boolParameter)
    return;
Method(intParameter, variable);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(intParameter, variable);"), new LiveIn("intParameter", "variable"));
        }

        [TestMethod]
        public void ProcessLocalReference_ReassignedInLastBlock_NotLiveIn()
        {
            var code = @"
var variable = 0;
if (boolParameter)
    return;
variable = 42;
Method(intParameter, variable);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(intParameter, variable);"), new LiveIn("intParameter"));
        }

        [TestMethod]
        public void ProcessLocalReference_GlobalScopeSymbol_NotLiveIn_NotLiveOut()
        {
            var code = @"
var s = new Sample();
Method(field, s.Property);";
            new Context(code).ValidateAllEmpty();
        }

        [TestMethod]
        public void ProcessLocalReference_UndefinedSymbol_NotLiveIn_NotLiveOut()
        {
            var code = @"Method(undefined);";
            new Context(code).ValidateAllEmpty();
        }

        [TestMethod]
        public void ProcessLocalReference_UsedAsOutArgument_NotLiveIn()
        {
            var code = @"
var refVariable = 42;
var outVariable = 42;
if (boolParameter)
    return;
Main(true, 0, out outVariable, ref refVariable);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("Main(true, 0, out outVariable, ref refVariable);"), new LiveIn("refVariable"));
        }

        [TestMethod]
        public void ProcessLocalReference_NotAssigned_LiveIn_LiveOut()
        {
            var code = @"
var variable = intParameter;
if (boolParameter)
    return;
Method(variable, intParameter);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("variable = intParameter"), new LiveIn("intParameter", "boolParameter"), new LiveOut("variable", "intParameter"));
            context.Validate(context.Block("Method(variable, intParameter);"), new LiveIn("variable", "intParameter"));
        }

        [TestMethod]
        public void ProcessLocalReference_VariableDeclarator_NotLiveIn_LiveOut()
        {
            var code = @"
int intValue = 42;
var varValue = 42;
if (intValue == 0)
    Method(intValue, varValue);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock);
            context.Validate(context.Block("intValue = 42"), new LiveOut("intValue", "varValue"));
            context.Validate(context.Block("Method(intValue, varValue);"), new LiveIn("intValue", "varValue"));
        }

        [TestMethod]
        public void ProcessSimpleAssignment_Discard_NotLiveIn_NotLiveOut()
        {
            var code = @"_ = intParameter;";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Block("_ = intParameter;"), new LiveIn("intParameter"));
        }

        [TestMethod]
        public void ProcessSimpleAssignment_UndefinedSymbol_NotLiveIn_NotLiveOut()
        {
            var code = @"
undefined = intParameter;
if (undefined == 0)
    Method(undefined);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Block("Method(undefined);"));
        }

        [TestMethod]
        public void ProcessSimpleAssignment_GlobalScoped_NotLiveIn_NotLiveOut()
        {
            var code = @"
field = intParameter;
if (field == 0)
    Method(field);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Block("Method(field);"));
        }

        [TestMethod]
        public void ProcessSimpleAssignment_LocalScoped_NotLiveIn_LiveOut()
        {
            var code = @"
int value;
value = 42;
if (value == 0)
    Method(value);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock);
            context.Validate(context.Block("value = 42;"), new LiveOut("value"));
            context.Validate(context.Block("Method(value);"), new LiveIn("value"));
        }

        [TestMethod]
        public void ProcessVariableInForeach_Declared_LiveIn_LiveOut()
        {
            /*
             * Entry
             *    |
             * Block 1
             * new[] {1, 2, 3}
             *    |
             * Block 2 <------------------------+
             * MoveNext branch                  |
             * F|   \ Else                      |
             *  v    \                          |
             * Exit  Block 3                    |
             *       i=capture.Current          |
             *       Method(i, intParameter) -->+
             *        |                         A
             *       Block 4 ------------------>+
             *       Method(i)
             */
            var code = @"
foreach(var i in new int[] {1, 2, 3})
{
    Method(i, intParameter);
    if (boolParameter)
      Method(i);
}";
            var context = new Context(code);
            context.Validate(context.Cfg.Blocks[1], new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Cfg.Blocks[2], new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(i, intParameter);"), new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter", "i"));
            context.Validate(context.Block("Method(i);"), new LiveIn("boolParameter", "intParameter", "i"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void ProcessVariableInForeach_Reused_LiveIn_LiveOut()
        {
            var code = @"
int i = 42;
foreach(i in new int[] {1, 2, 3})
{
    Method(i, intParameter);
}";
            var context = new Context(code);
            context.Validate(context.Cfg.Blocks[1], new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Cfg.Blocks[2], new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Block("Method(i, intParameter);"), new LiveIn("intParameter"), new LiveOut("intParameter"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void StaticLocalFunction_Expression_LiveIn()
        {
            var code = @"
outParameter = LocalFunction(intParameter);
static int LocalFunction(int a) => a + 1;";
            var context = new Context(code, "LocalFunction");
            context.Validate(context.Cfg.EntryBlock, new LiveIn("a"), new LiveOut("a"));
            context.Validate(context.Block("a + 1"), new LiveIn("a"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void StaticLocalFunction_Expression_NotLiveIn_NotLiveOut()
        {
            var code = @"
outParameter = LocalFunction(0);
static int LocalFunction(int a) => 42;";
            var context = new Context(code, "LocalFunction");
            context.Validate(context.Cfg.EntryBlock);
            context.Validate(context.Block("42"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void StaticLocalFunction_LiveIn()
        {
            var code = @"
outParameter = LocalFunction(intParameter);
static int LocalFunction(int a)
{
    return a + 1;
}";
            var context = new Context(code, "LocalFunction");
            context.Validate(context.Cfg.EntryBlock, new LiveIn("a"), new LiveOut("a"));
            context.Validate(context.Block("a + 1"), new LiveIn("a"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void StaticLocalFunction_NotLiveIn_NotLivOut()
        {
            var code = @"
outParameter = LocalFunction(0);
static int LocalFunction(int a)
{
    return 42;
}";
            var context = new Context(code, "LocalFunction");
            context.Validate(context.Cfg.EntryBlock);
            context.Validate(context.Block("42"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void StaticLocalFunction_Recursive()
        {
            var code = @"
outParameter = LocalFunction(intParameter);
static int LocalFunction(int a)
{
    if(a <= 0)
        return 0;
    else
        return LocalFunction(a - 1);
};";
            var context = new Context(code, "LocalFunction");
            context.Validate(context.Cfg.EntryBlock, new LiveIn("a"), new LiveOut("a"));
            context.Validate(context.Block("0"));
            context.Validate(context.Block("LocalFunction(a - 1)"), new LiveIn("a"));
            context.Validate(context.Cfg.ExitBlock);
        }

        [TestMethod]
        public void LocalFunctionInvocation_LiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
LocalFunction();

int LocalFunction() => variable;";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter") /* ToDo: "variable" should LiveIn and LiveOut("variable")*/);
            context.Validate(context.Block("LocalFunction();") /* ToDo: Should be new LiveIn("variable") */);
        }

        [TestMethod]
        public void LocalFunctionInvocation_FunctionDeclaredBeforeCode_LiveIn()
        {
            var code = @"
int LocalFunction() => variable;

var variable = 42;
if (boolParameter)
    return;
LocalFunction();";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter") /* ToDo: "variable" should LiveIn and LiveOut("variable")*/);
            context.Validate(context.Block("LocalFunction();") /* ToDo: Should be new LiveIn("variable") */);
        }

        [TestMethod]
        public void LocalFunctionInvocation_Generic_LiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
LocalFunction<int>();

T LocalFunction<T>() => variable;";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter") /* ToDo: "variable" should LiveIn and LiveOut("variable")*/);
            context.Validate(context.Block("LocalFunction<int>();") /* ToDo: Should be new LiveIn("variable") */);
        }

        [TestMethod]
        public void LocalFunctionInvocation_NotLiveIn()
        {
            var code = @"
var variable = 42;
if (boolParameter)
    return;
LocalFunction();
Method(variable);

void LocalFunction()
{
    variable = 0;
}";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter"), new LiveOut("boolParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter"), /*ToDo: Should not LiveOut, because it's overriden in LocalFunction */ new LiveOut("variable"));
            context.Validate(context.Block("LocalFunction();"), /* ToDo: Should not LiveIn, because it's overriden in LocalFunction*/ new LiveIn("variable"));
        }

        [TestMethod]
        public void Loop_Propagates_LiveIn_LiveOut()
        {
            var code = @"
A:
Method(intParameter);
if (boolParameter)
    goto B;
Method(0);
goto A;
B:
Method(1);";
            var context = new Context(code);
            context.Validate(context.Cfg.EntryBlock, new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("boolParameter"), new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(0);"), new LiveIn("boolParameter", "intParameter"), new LiveOut("boolParameter", "intParameter"));
            context.Validate(context.Block("Method(1);"));
        }

        private class Context
        {
            public readonly RoslynLiveVariableAnalysis Lva;
            public readonly ControlFlowGraph Cfg;

            public Context(string methodBody, string localFunctionName = null, bool isCSharp = true)
            {
                var code = isCSharp
                    ? @$"
using System.Linq;
public class Sample
{{
    delegate void VoidDelegate();

    private int field;
    public int Property {{ get; set; }};

    public void Main(bool boolParameter, int intParameter, out int outParameter, ref int refParameter)
    {{
        {methodBody}
    }}

    private int Method(params int[] args) => 42;
    private string Method(params string[] args) => null;
    private bool IsMethod(params bool[] args) => true;
    private void Capturing(System.Func<int, int> f) {{ }}
}}"
                    : @$"
Public Class Sample

    Public Delegate Sub VoidDelegate()

    Private Field As Integer
    Private Property Prop As Integer

    Public Sub Main(BoolParameter As Boolean, IntParameter As Integer, ByRef RefParameter As Integer)
        {methodBody}
    End Sub

    Private Function Method(ParamArray Args() As Integer) As Integer
    End Function

    Private Function Method(ParamArray Args() As String) As String
    End Function

    Private Function IsMethod(ParamArray Args() As Boolean) As Boolean
    End Function

    Private Sub Capturing(f As Func(Of Integer, Integer))
    End Sub

End Class";
                Cfg = TestHelper.CompileCfg(code, isCSharp);
                if (localFunctionName != null)
                {
                    Cfg = Cfg.GetLocalFunctionControlFlowGraph(Cfg.LocalFunctions.Single(x => x.Name == localFunctionName));
                }
                Console.WriteLine(CfgSerializer.Serialize(Cfg));
                Lva = new RoslynLiveVariableAnalysis(Cfg);
            }

            public BasicBlock Block(string withSyntax = null) =>
                Cfg.Blocks.Single(x => x.Kind == BasicBlockKind.Block && (withSyntax == null || x.OperationsAndBranchValue.Any(operation => operation.Syntax.ToString() == withSyntax)));

            public BasicBlock Block<TOperation>(string withSyntax) where TOperation : IOperation =>
                Cfg.Blocks.Single(x => x.Kind == BasicBlockKind.Block && x.OperationsAndBranchValue.OfType<TOperation>().Any(operation => operation.Syntax.ToString() == withSyntax));

            public void ValidateAllEmpty()
            {
                foreach (var block in Cfg.Blocks)
                {
                    Validate(block);
                }
            }

            public void Validate(BasicBlock block, params Expected[] expected)
            {
                // This is not very nice from OOP perspective, but it makes UTs above easy to read.
                var expectedLiveIn = expected.OfType<LiveIn>().SingleOrDefault() ?? new LiveIn();
                var expectedLiveOut = expected.OfType<LiveOut>().SingleOrDefault() ?? new LiveOut();
                var expectedCaptured = expected.OfType<Captured>().SingleOrDefault() ?? new Captured();
                Lva.LiveIn(block).Select(x => x.Name).Should().BeEquivalentTo(expectedLiveIn.Names, $"{block.Kind} #{block.Ordinal}");
                Lva.LiveOut(block).Select(x => x.Name).Should().BeEquivalentTo(expectedLiveOut.Names, $"{block.Kind} #{block.Ordinal}");
                Lva.CapturedVariables.Select(x => x.Name).Should().BeEquivalentTo(expectedCaptured.Names, $"{block.Kind} #{block.Ordinal}");
            }
        }

        private abstract class Expected
        {
            public readonly string[] Names;

            protected Expected(string[] names) =>
                Names = names;
        }

        private class LiveIn : Expected
        {
            public LiveIn(params string[] names) : base(names) { }
        }

        private class LiveOut : Expected
        {
            public LiveOut(params string[] names) : base(names) { }
        }

        private class Captured : Expected
        {
            public Captured(params string[] names) : base(names) { }
        }
    }
}
