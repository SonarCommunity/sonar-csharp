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

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn;

public partial class RoslynSymbolicExecutionTest
{
    [DataTestMethod]
    [DataRow("for (var i = 0; i < items.Length; i++)")]
    [DataRow("while (Condition)")]
    public void Loops_InstructionVisitedMaxTwice(string loop)
    {
        var code = $$"""
            {{loop}}
            {
                arg.ToString(); // Add another constraint to 'arg'
            }
            Tag("End", arg);
            """;
        var validator = SETestContext.CreateCS(code, "int arg, int[] items", new AddConstraintOnInvocationCheck(), new PreserveTestCheck("arg")).Validator;
        validator.ValidateExitReachCount(2);    // PreserveTestCheck is needed for this, otherwise, variables are thrown away by LVA when going to the Exit block
        validator.TagValues("End").Should().HaveCount(2)
            .And.SatisfyRespectively(
                x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull),
                x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First));
    }

    [DataTestMethod]
    [DataRow("for (var i = 0; i < 10; i++)")]
    [DataRow("for (var i = 0; i < 10; ++i)")]
    [DataRow("for (var i = 10; i > 0; i--)")]
    [DataRow("for (var i = 10; i > 0; --i)")]
    public void Loops_InstructionVisitedMaxTwice_For_FixedCount(string loop)
    {
        var code = $$"""
            {{loop}}
            {
                arg.ToString(); // Add another constraint to 'arg'
            }
            Tag("End", arg);
            """;
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(1);
        validator.ValidateTag("End", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First));   // Loop was entered, arg has only it's final constraints after looping once
    }

    [TestMethod]
    public void Loops_InstructionVisitedMaxTwice_For_FixedCount_Expanded()
    {
        const string code = """
            for (var i = 0; i < 10; i++)
            {
                Tag("Inside", i);
            }
            Tag("End");
            """;
        var validator = SETestContext.CreateCS(code).Validator;
        validator.TagValues("Inside").Should().SatisfyRespectively(
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(0)),
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 9))); // 1 to 9 would be more precise
        validator.TagStates("End").Should().SatisfyRespectively(
            x => x[validator.Symbol("i")].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(10, null)));   // We can assert because LVA did not kick in yet
    }

    [TestMethod]
    public void Loops_For_ComplexCondition_MultipleVariables()
    {
        const string code = """
            for (int i = 0, j = 10; i < 10 && j > 0; i++, j++)
            {
                arg.ToString(); // Add another constraint to 'arg'
            }
            Tag("End");
            """;
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        var arg = validator.Symbol("arg");
        var i = validator.Symbol("i");
        var j = validator.Symbol("j");
        validator.ValidateExitReachCount(1);
        validator.TagStates("End").Should().SatisfyRespectively(
            x =>
            {
                x[i].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(10, null));
                x[j].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(11));
                x[arg].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First);
            },
            x =>
            {
                x[i].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 9));
                x[j].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 0));
                x[arg].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First);
            });
    }

    [TestMethod]
    public void Loops_For_ComplexCondition_AlwaysTrue()
    {
        const string code = """
            boolParameter = true;
            for (var i = 0; i < 10 || boolParameter; i++)
            {
                arg.ToString(); // Add another constraint to 'arg'
                Tag("InLoop");
            }
            Tag("Unreachable");
            """;
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        var arg = validator.Symbol("arg");
        var i = validator.Symbol("i");
        validator.ValidateExitReachCount(0);
        validator.ValidateTagOrder("InLoop", "InLoop", "InLoop");
        validator.TagStates("InLoop").Should().SatisfyRespectively(
            x =>
            {
                x[i].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(0));
                x[arg].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First);
            },
            x =>
            {
                x[i].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 9));
                x[arg].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First, BoolConstraint.True);
            },
            x =>
            {
                x[i].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(10, null));
                x[arg].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First, BoolConstraint.True);
            });
    }

    [TestMethod]
    public void Loops_For_ComplexCondition_AlwaysFalse()
    {
        const string code = """
            boolParameter = false;
            for (var i = 0; i < 10 && boolParameter; i++)
            {
                arg.ToString(); // Add another constraint to 'arg'
                Tag("Unreachable");
            }
            Tag("End", arg);
            """;
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(1);
        validator.ValidateTagOrder("End");
        validator.TagStates("End").Should().SatisfyRespectively(
            x =>
            {
                x[validator.Symbol("i")].Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(0));  // We can assert because LVA did not kick in yet
                x[validator.Symbol("arg")].Should().HaveOnlyConstraints(ObjectConstraint.NotNull);                          // AddConstraintOnInvocationCheck didn't add anything
            });
    }

    [TestMethod]
    public void Loops_While_FixedCount()
    {
        const string code = """
            var i = 0;
            while(i < 10)   // Same as: for(var i=0; i < 10; i++)
            {
                arg.ToString(); // Add another constraint to 'arg'
                i++;
                Tag("Inside", i);
            }
            Tag("After", i);
            Tag("End", arg);
            """;
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(1);
        validator.TagValues("Inside").Should().SatisfyRespectively(
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(1)),
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 10))); // 1 to 10 would be more precise
        validator.ValidateTag("After", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(10, null)));
        validator.ValidateTag("End", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First));   // arg has only it's final constraints after looping once
    }

    [TestMethod]
    public void Loops_NestedBinaryIf_BehavesLikeLoopConditionIf()
    {
        const string code = """
            var i = 0;
            while (Condition)   // We are inside a loop => binary operations are evaluated to true/false for 1st pass, and learn range condition for 2nd pass
            {
                if (i < 10)
                {
                    Tag("Inside", i);
                    i++;
                }
                Tag("After", i);
            }
            """;
        var validator = SETestContext.CreateCS(code, new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(2);
        validator.TagValues("Inside").Should().SatisfyRespectively(
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(0)),
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 9)));     // 1 to 9 would be more precise
        validator.TagValues("After").Should().SatisfyRespectively(
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(1)),            // Initial pass through "if"
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(10, null)),     // Broke away from "loop", assuming it looped until the "if" condition resulted in false
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, NumberConstraint.From(null, 10)));    // Second pass through "if", for inner range
    }

    [TestMethod]
    public void Loops_InstructionVisitedMaxTwice_ForEach()
    {
        const string code = @"
foreach (var i in items)
{{
    arg.ToString(); // Add another constraint to 'arg'
    arg -= 1;
}}
Tag(""End"", arg);";
        var validator = SETestContext.CreateCS(code, "int arg, int[] items", new AddConstraintOnInvocationCheck()).Validator;
        // In the case of foreach, there are implicit method calls that in the current implementation can throw:
        // - IEnumerable<>.GetEnumerator()
        // - System.Collections.IEnumerator.MoveNext()
        // - System.IDisposable.Dispose()
        validator.ValidateExitReachCount(12);                       // foreach produces implicit TryFinally region where it can throw - these flows do not reach the Tag("End") line
        validator.TagValues("End").Should().SatisfyRespectively(
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull),  // items with Null flow generated by implicty Finally block that has items?.Dispose()
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull),  // items with NotNull flow
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First),
            x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, TestConstraint.First));
    }

    [TestMethod]
    public void Loops_InstructionVisitedMaxTwice_EvenWithMultipleStates()
    {
        const string code = @"
bool condition;
if (Condition)      // This generates two different ProgramStates, each tracks its own visits
    condition = true;
else
    condition = false;
do
{
    arg.ToString(); // Add another constraint to 'arg'
} while (Condition);
Tag(""End"", arg);";
        var validator = SETestContext.CreateCS(code, "int arg, int[] items", new AddConstraintOnInvocationCheck(), new PreserveTestCheck("condition", "arg")).Validator;
        validator.ValidateExitReachCount(4);    // PreserveTestCheck is needed for this, otherwise, variables are thrown away by LVA when going to the Exit block
        var states = validator.TagStates("End");
        var condition = validator.Symbol("condition");
        var arg = validator.Symbol("arg");
        states.Should().HaveCount(4)
            .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.True) && x[arg].HasConstraint(TestConstraint.First) && !x[arg].HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.True) && x[arg].HasConstraint(TestConstraint.First) && x[arg].HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.False) && x[arg].HasConstraint(TestConstraint.First) && !x[arg].HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.False) && x[arg].HasConstraint(TestConstraint.First) && x[arg].HasConstraint(BoolConstraint.True));
    }

    [TestMethod]
    public void DoLoop_InstructionVisitedMaxTwice()
    {
        var code = $@"
do
{{
    arg.ToString(); // Add another constraint to 'arg'
    arg -= 1;
}} while (arg > 0);
Tag(""End"", arg);";
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(1);
        validator.TagValues("End").Should().HaveCount(2)
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
    }

    [DataTestMethod]
    [DataRow("for( ; ; )")]
    [DataRow("while (true)")]
    public void InfiniteLoopWithNoExitBranch_InstructionVisitedMaxTwice(string loop)
    {
        var code = @$"
{loop}
{{
    arg.ToString(); // Add another constraint to 'arg'
    Tag(""Inside"", arg);
}}";
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(0);                    // There's no branch to 'Exit' block, or the exist is never reached
        validator.TagValues("Inside").Should().HaveCount(2)
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
    }

    [TestMethod]
    public void GoTo_InfiniteWithNoExitBranch_InstructionVisitedMaxTwice()
    {
        const string code = @"
Start:
arg.ToString(); // Add another constraint to 'arg'
Tag(""Inside"", arg);
goto Start;";
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(0);                    // There's no branch to 'Exit' block
        validator.TagValues("Inside").Should().HaveCount(2)
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
    }

    [TestMethod]
    public void GoTo_InstructionVisitedMaxTwice()
    {
        const string code = @"
Start:
arg.ToString(); // Add another constraint to 'arg'
arg -= 1;
if (arg > 0)
{
    goto Start;
}
Tag(""End"", arg);";
        var validator = SETestContext.CreateCS(code, "int arg", new AddConstraintOnInvocationCheck()).Validator;
        validator.ValidateExitReachCount(1);
        validator.TagValues("End").Should().HaveCount(2)
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
            .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
    }

    [DataTestMethod]
    [DataRow("for (var i = 0; condition; i++)")]
    [DataRow("while (condition)")]
    public void Loops_FalseConditionNotExecuted(string loop)
    {
        var code = $@"
var condition = false;
{loop}
{{
    Tag(""Loop"");
}}
Tag(""End"");";
        SETestContext.CreateCS(code).Validator.ValidateTagOrder("End");
    }

    [TestMethod]
    public void DoWhileLoopWithTryCatchAndNullFlows()
    {
        var code = @"
Exception lastEx = null;
do
{
    try
    {
        InstanceMethod(); // May throw
        Tag(""BeforeReturn"", lastEx);
        return;
    }
    catch (InvalidOperationException e)
    {
        lastEx = e;
        Tag(""InCatch"", lastEx);
    }
} while(boolParameter);

Tag(""End"", lastEx);
";
        var validator = SETestContext.CreateCS(code).Validator;
        validator.ValidateTagOrder("BeforeReturn", "InCatch", "End", "BeforeReturn", "InCatch");
        validator.TagValues("BeforeReturn").Should().SatisfyRespectively(
            x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue(),                              // InstanceMethod did not throw
            x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());                          // InstanceMethod did throw, was caught, and flow continued
        validator.ValidateTag("End", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue()); // InstanceMethod did throw and was caught
    }
}
