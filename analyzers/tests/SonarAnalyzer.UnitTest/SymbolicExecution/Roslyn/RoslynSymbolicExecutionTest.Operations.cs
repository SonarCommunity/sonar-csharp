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

using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromLiteral()
        {
            var collector = SETestContext.CreateCS(@"var a = true; Tag(""a"", a);", new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.ValidateTag("a", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol()
        {
            var collector = SETestContext.CreateCS(@"bool a = true, b; b = a; Tag(""b"", b);", new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.Validate("SimpleAssignment: b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.Validate("SimpleAssignment: b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_Chained()
        {
            var collector = SETestContext.CreateCS(@"bool a = true, b, c; c = b = a; Tag(""c"", c);", new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.Validate("SimpleAssignment: c = b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.Validate("SimpleAssignment: c = b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.ValidateTag("c", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToParameter_FromLiteral()
        {
            var collector = SETestContext.CreateCS(@"boolParameter = true; Tag(""boolParameter"", boolParameter);", new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.Validate("SimpleAssignment: boolParameter = true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.Validate("SimpleAssignment: boolParameter = true", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            collector.ValidateTag("boolParameter", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_CS()
        {
            var setter = new PreProcessTestCheck(x =>
            {
                if (x.Operation.Instance.Kind == OperationKind.ParameterReference)
                {
                    x.State[x.Operation].SetConstraint(DummyConstraint.Dummy);
                }
                return x.State;
            });
            var collector = SETestContext.CreateCS(@"var b = boolParameter; Tag(""b"", b);", setter).Validator;
            collector.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_VB()
        {
            var setter = new PreProcessTestCheck(x =>
            {
                if (x.Operation.Instance.Kind == OperationKind.ParameterReference)
                {
                    x.State[x.Operation].SetConstraint(DummyConstraint.Dummy);
                }
                return x.State;
            });
            var collector = SETestContext.CreateVB(@"Dim B As Boolean = BoolParameter : Tag(""B"", B)", setter).Validator;
            collector.ValidateTag("B", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow(@"Sample.StaticField = 42; Tag(""Target"", Sample.StaticField);")]
        [DataRow(@"StaticField = 42; Tag(""Target"", StaticField);")]
        [DataRow(@"Sample.StaticProperty = 42; Tag(""Target"", Sample.StaticProperty);")]
        [DataRow(@"StaticProperty = 42; Tag(""Target"", StaticProperty);")]
        [DataRow(@"var dict = new Dictionary<string, int>(); dict[""key""] = 42; Tag(""Target"", dict[""key""]);")]
        [DataRow(@"var other = new Sample(); other.Property = 42; Tag(""Target"", other.Property);")]
        [DataRow(@"this.Property = 42; Tag(""Target"", this.Property);")]
        [DataRow(@"Property = 42; Tag(""Target"", Property);")]
        [DataRow(@"this.field = 42; Tag(""Target"", this.field);")]
        [DataRow(@"field = 42; Tag(""Target"", this.field);")]
        public void SimpleAssignment_ToSupported_FromLiteral(string snippet)
        {
            var collector = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.ValidateTag("Target", x => (x?.HasConstraint(DummyConstraint.Dummy) ?? false).Should().BeTrue());
        }



        [DataTestMethod]
        [DataRow(@"var arr = new byte[] { 13 }; arr[0] = 42; Tag(""Target"", arr[0]);")]
        public void SimpleAssignment_ToUnsupported_FromLiteral(string snippet)
        {
            var collector = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            collector.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            collector.ValidateTag("Target", x => (x?.HasConstraint(DummyConstraint.Dummy) ?? false).Should().BeFalse());
        }
    }
}
