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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void Finally_Simple()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
}
finally
{
    Tag(""InFinally"");
}
Tag(""AfterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry",
                "InFinally",
                "AfterFinally");
        }

        [TestMethod]
        public void Finally_Nested_ExitingTwoFinallyOnSameBranch()
        {
            const string code = @"
Tag(""BeforeOuterTry"");
try
{
    Tag(""InOuterTry"");
    try
    {
        Tag(""InInnerTry"");
    }
    finally
    {
        true.ToString();    // Put some operations in the way
        Tag(""InInnerFinally"");
    }
}
finally
{
    true.ToString();    // Put some operations in the way
    true.ToString();
    Tag(""InOuterFinally"");
}
Tag(""AfterOuterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeOuterTry",
                "InOuterTry",
                "InInnerTry",
                "InInnerFinally",
                "InOuterFinally",
                "AfterOuterFinally");
        }

        [TestMethod]
        public void Finally_Nested_InstructionAfterFinally()
        {
            const string code = @"
Tag(""BeforeOuterTry"");
try
{
    Tag(""InOuterTry"");
    try
    {
        Tag(""InInnerTry"");
    }
    finally
    {
        true.ToString();    // Put some operations in the way
        Tag(""InInnerFinally"");
    }
    Tag(""AfterInnerFinally"");
}
finally
{
    true.ToString();    // Put some operations in the way
    Tag(""InOuterFinally"");
}
Tag(""AfterOuterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeOuterTry",
                "InOuterTry",
                "InInnerTry",
                "InInnerFinally",
                "AfterInnerFinally",
                "InOuterFinally",
                "AfterOuterFinally");
        }

        [TestMethod]
        public void Finally_BranchInNested()
        {
            const string code = @"
Tag(""BeforeOuterTry"");
try
{
    Tag(""InOuterTry"");
    try
    {
        Tag(""InInnerTry"");
        if (Condition)
        {
            Tag(""1"");
        }
        else
        {
            Tag(""2"");
        }
    }
    finally
    {
        Tag(""InInnerFinally"");
    }
}
finally
{
    Tag(""InOuterFinally"");
}
Tag(""AfterOuterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeOuterTry",
                "InOuterTry",
                "InInnerTry",
                "1",
                "2",
                "InInnerFinally",
                "InOuterFinally",
                "AfterOuterFinally");
        }

        [TestMethod]
        public void Finally_BranchAfterFinally()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
}
finally
{
    true.ToString();    // Put some operations in the way
    Tag(""InFinally"");
}
if (boolParameter)  // No operation between the finally and this. This will create a single follow up block with BranchValue
{
    Tag(""1"");
}
else
{
    Tag(""2"");
}";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry",
                "InFinally",
                "1",
                "2");
        }

        [TestMethod]
        public void Finally_BranchInFinally()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
}
finally
{
    Tag(""InFinallyBeforeCondition"");
    if (Condition)
    {
        Tag(""1"");
    }
    else
    {
        Tag(""2"");
    }
    Tag(""InFinallyAfterCondition"");
}
Tag(""AfterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry",
                "InFinallyBeforeCondition",
                "1",
                "2",
                "InFinallyAfterCondition",
                "AfterFinally");
        }

        [TestMethod]
        public void Finally_WrappedInLocalLifetimeRegion()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
}
finally
{
    var local = true;   // This creates LocalLifeTime region
    Tag(""InFinally"");
    // Here is Block#4 outside the LocalLifeTime region
}
Tag(""AfterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry",
                "InFinally",
                "AfterFinally");
        }

        [TestMethod]
        public void Finally_ThrowInTry()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
    throw new System.Exception();
    Tag(""UnreachableInTry"");
}
finally
{
    Tag(""InFinally"");
}
Tag(""UnreachableAfterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry");   // ToDo: MMF-2393 There should be also InFinally
        }

        [TestMethod]
        public void Finally_ThrowInFinally()
        {
            const string code = @"
Tag(""BeforeTry"");
try
{
    Tag(""InTry"");
}
finally
{
    Tag(""InFinally"");
    throw new System.Exception();
    Tag(""UnreachableInFinally"");
}
Tag(""UnreachableAfterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeTry",
                "InTry",
                "InFinally");
        }

        [TestMethod]
        public void Finally_NestedFinally()
        {
            const string code = @"
Tag(""BeforeOuterTry"");
try
{
    Tag(""InOuterTry"");
}
finally
{
    Tag(""InOuterFinally"");
    try
    {
        Tag(""InInnerTry"");
    }
    finally
    {
        Tag(""InInnerFinally"");
    }
    Tag(""AfterInnerFinally"");
}
Tag(""AfterOuterFinally"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "BeforeOuterTry",
                "InOuterTry",
                "InOuterFinally",
                "InInnerTry",
                "InInnerFinally",
                "AfterInnerFinally",
                "AfterOuterFinally");
        }
    }
}
