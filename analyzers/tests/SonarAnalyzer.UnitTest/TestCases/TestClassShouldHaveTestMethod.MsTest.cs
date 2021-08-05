﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Diagnostics
{
    [TestClass]
    class ClassTest2 // Noncompliant
//        ^^^^^^^^^^
    {
    }

    [TestClass]
    class ClassTest4 // Noncompliant
    {
        public void Foo() { }
    }

    class ClassTest5
    {
        public void Foo() { }
    }

    class ClassTest6
    {
        public void Foo() { }
    }

    [TestClass]
    class ClassTest9
    {
        [TestMethod]
        public void Foo() { }
    }

    [TestClass]
    class ClassTest12
    {
        [DataTestMethod]
        [DataRow(1)]
        public void Foo(int i) { }
    }

    [TestClass]
    public abstract class MyCommonCode1
    {
    }

    [TestClass]
    public class MySubCommonCode1 : MyCommonCode1 // Noncompliant
    {
    }

    public class MySubCommonCode11 : MyCommonCode1 // Compliant
    {
    }

    [TestClass]
    public abstract class TestFooBase
    {
        [TestMethod]
        public void Foo_WhenFoo_ExpectsFoo() { }
    }

    // See https://github.com/SonarSource/sonar-dotnet/issues/1196
    [TestClass]
    public class TestSubFoo : TestFooBase // Compliant - base abstract and at least 1 test in base
    {

    }

    [TestClass]
    class // Error [CS1001]
    {
    }
}

namespace TestSetupAndCleanupAttributes
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    // Regression tests for Bug 1486: https://github.com/SonarSource/sonar-dotnet/issues/1486
    // Don't raise for classes that test setup or cleanup attributes

    [TestClass]
    public class SetupAttributes1
    {
        [AssemblyInitialize]
        public static void BeforeTests(TestContext context)
        {
        }
    }

    [TestClass]
    public static class SetupAttributes2
    {
        [AssemblyCleanup]
        public static void AfterTests()
        {
        }
    }


    [TestClass]
    public static class SetupAttributes3
    {
        [AssemblyInitialize]
        public static void BeforeTests(TestContext context)
        {
        }

        [AssemblyCleanup]
        public static void AfterTests()
        {
        }
    }
}

namespace DerivedAttributes
{

    public class DerivedTestClassAttribute: TestClassAttribute
    {

    }

    public class DerivedTestMethodAttribute: TestMethodAttribute
    {

    }

    public class DerivedDataTestMethodAttribute : DataTestMethodAttribute
    {

    }

    [DerivedTestClass]
    class DerivedClassAttribute // Noncompliant
//        ^^^^^^^^^^^^^^^^^^^^^
    {
    }

    [DerivedTestClassAttribute]
    class DerivedMethodAttribute1
    {
        [DerivedTestMethod]
        public void Foo() { }
    }

    [DerivedTestClass]
    class DerivedMethodAttribute2
    {
        [DerivedTestMethod]
        [DataRow(1)]
        public void Foo() { }
    }
}
