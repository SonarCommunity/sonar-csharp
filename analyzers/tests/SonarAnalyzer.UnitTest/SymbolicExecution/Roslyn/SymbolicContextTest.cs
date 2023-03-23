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
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    [TestClass]
    public class SymbolicContextTest
    {
        [TestMethod]
        public void NullArgument_Throws()
        {
            var operation = CreateOperation();
            ((Func<SymbolicContext>)(() => new SymbolicContext(operation, null))).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("state");
        }

        [TestMethod]
        public void NullOperation_SetsOperationToNull() =>
            new SymbolicContext(null, ProgramState.Empty).Operation.Should().Be(null);

        [TestMethod]
        public void PropertiesArePersisted()
        {
            var operation = CreateOperation();
            var state = ProgramState.Empty.SetOperationValue(operation, SymbolicValue.Empty);

            var sut = new SymbolicContext(operation, state);
            sut.Operation.Should().Be(operation);
            sut.State.Should().Be(state);
        }

        [TestMethod]
        public void SetOperationConstraint_WithExistingValue()
        {
            var operation = CreateOperation();
            var state = ProgramState.Empty.SetOperationValue(operation, SymbolicValue.Empty);

            var sut = new SymbolicContext(operation, state);
            var result = sut.SetOperationConstraint(DummyConstraint.Dummy);
            result.Should().NotBe(state, "new ProgramState instance should be created");
            result[operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue();
        }

        [TestMethod]
        public void SetOperationConstraint_WithNewValue()
        {
            var operation = CreateOperation();
            var state = ProgramState.Empty;

            var sut = new SymbolicContext(operation, state);
            var result = sut.SetOperationConstraint(DummyConstraint.Dummy);
            result.Should().NotBe(state, "new ProgramState instance should be created");
            result[operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue();
        }

        [TestMethod]
        public void SetSymbolConstraint_WithExistingValue()
        {
            var operation = CreateOperation();
            var symbol = operation.Children.First().TrackedSymbol();
            var state = ProgramState.Empty.SetSymbolValue(symbol, SymbolicValue.Empty);

            var sut = new SymbolicContext(operation, state);
            var result = sut.SetSymbolConstraint(symbol, DummyConstraint.Dummy);
            result.Should().NotBe(state, "new ProgramState instance should be created");
            result[symbol].HasConstraint(DummyConstraint.Dummy).Should().BeTrue();
        }

        [TestMethod]
        public void SetSymbolConstraint_WithNewValue()
        {
            var operation = CreateOperation();
            var symbol = operation.Children.First().TrackedSymbol();
            var state = ProgramState.Empty;

            var sut = new SymbolicContext(operation, state);
            var result = sut.SetSymbolConstraint(symbol, DummyConstraint.Dummy);
            result.Should().NotBe(state, "new ProgramState instance should be created");
            result[symbol].HasConstraint(DummyConstraint.Dummy).Should().BeTrue();
        }

        [TestMethod]
        public void WithState_SameState_ReturnsThis()
        {
            var state = ProgramState.Empty;
            var sut = new SymbolicContext(null, state);
            sut.WithState(state).Should().Be(sut);
        }

        [TestMethod]
        public void WithState_DifferentState_ReturnsNew()
        {
            var state = ProgramState.Empty;
            var sut = new SymbolicContext(null, state);
            var newState = state.SetOperationValue(CreateOperation(), SymbolicValue.Empty);
            sut.WithState(newState).Should().NotBe(sut);
        }

        [TestMethod]
        public void ApplyOpposite_RespectsArgument()
        {
            BoolConstraint.True.ApplyOpposite(false).Should().Be(BoolConstraint.True);
            BoolConstraint.True.ApplyOpposite(true).Should().Be(BoolConstraint.False);
            BoolConstraint.False.ApplyOpposite(false).Should().Be(BoolConstraint.False);
            BoolConstraint.False.ApplyOpposite(true).Should().Be(BoolConstraint.True);
            // Special constraint behavior
            ObjectConstraint.Null.ApplyOpposite(true).Should().Be(ObjectConstraint.NotNull);
            ObjectConstraint.NotNull.ApplyOpposite(true).Should().BeNull("NotNull can be Null or any other NotNull");
        }

        private static IOperationWrapperSonar CreateOperation() =>
            new(TestHelper.CompileCfgBodyCS("var value = 42;").Blocks[1].Operations[0]);
    }
}
