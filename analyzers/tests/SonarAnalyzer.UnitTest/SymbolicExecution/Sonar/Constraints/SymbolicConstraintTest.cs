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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.SymbolicExecution.Sonar.Constraints;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Sonar.Constraints
{
    [TestClass]
    public class SymbolicConstraintTest
    {
        [TestMethod]
        public void BoolConstraint_ToString()
        {
            BoolConstraint.True.ToString().Should().Be("True");
            BoolConstraint.False.ToString().Should().Be("False");
        }

        [TestMethod]
        public void ByteArraySymbolicValueConstraint_ToString()
        {
            ByteArraySymbolicValueConstraint.Constant.ToString().Should().Be("Constant");
            ByteArraySymbolicValueConstraint.Modified.ToString().Should().Be("Modified");
        }

        [TestMethod]
        public void CollectionCapacityConstraint_ToString()
        {
            CollectionCapacityConstraint.Empty.ToString().Should().Be("Empty");
            CollectionCapacityConstraint.NotEmpty.ToString().Should().Be("NotEmpty");
        }

        [TestMethod]
        public void CryptographyIVSymbolicValueConstraint_ToString()
        {
            CryptographyIVSymbolicValueConstraint.Initialized.ToString().Should().Be("Initialized");
            CryptographyIVSymbolicValueConstraint.NotInitialized.ToString().Should().Be("NotInitialized");
        }

        [TestMethod]
        public void DisposableConstraint_ToString()
        {
            DisposableConstraint.Disposed.ToString().Should().Be("Disposed");
            DisposableConstraint.NotDisposed.ToString().Should().Be("NotDisposed");
        }

        [TestMethod]
        public void NullableValueConstraint_ToString()
        {
            NullableValueConstraint.HasValue.ToString().Should().Be("HasValue");
            NullableValueConstraint.NoValue.ToString().Should().Be("NoValue");
        }

        [TestMethod]
        public void ObjectConstraint_ToString()
        {
            ObjectConstraint.Null.ToString().Should().Be("Null");
            ObjectConstraint.NotNull.ToString().Should().Be("NotNull");
        }

        [TestMethod]
        public void SaltSizeSymbolicValueConstraint_ToString()
        {
            SaltSizeSymbolicValueConstraint.Safe.ToString().Should().Be("Safe");
            SaltSizeSymbolicValueConstraint.Short.ToString().Should().Be("Short");
        }

        [TestMethod]
        public void SerializationConstraint_ToString()
        {
            SerializationConstraint.Safe.ToString().Should().Be("Safe");
            SerializationConstraint.Unsafe.ToString().Should().Be("Unsafe");
        }

        [TestMethod]
        public void StringConstraint_ToString()
        {
            StringConstraint.EmptyString.ToString().Should().Be("EmptyString");
            StringConstraint.FullString.ToString().Should().Be("FullString");
            StringConstraint.FullOrNullString.ToString().Should().Be("FullOrNullString");
            StringConstraint.WhiteSpaceString.ToString().Should().Be("WhiteSpaceString");
            StringConstraint.NotWhiteSpaceString.ToString().Should().Be("NotWhiteSpaceString");
            StringConstraint.FullNotWhiteSpaceString.ToString().Should().Be("FullNotWhiteSpaceString");
        }
    }
}
