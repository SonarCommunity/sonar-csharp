﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Diagnostics1
{
    class MsTestClass
    {
        [TestMethod]
        [Ignore]
//       ^^^^^^ Noncompliant {{Either remove this 'Ignore' attribute or add an explanation about why this test is ignored.}}
        public void Foo1()
        {
        }

        [TestMethod]
        [Ignore] // Reenable when something
        public void Foo2()
        {
        }

        [TestMethod]
        [Ignore]
        [WorkItem(1234)]
        public void Foo3()
        {
        }
    }
}

namespace Tests.Diagnostics2
{
    using NUnit.Framework;

    class NUnitClass
    {
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
//       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Noncompliant
        public void Foo1()
        {
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // Reenable when something
        public void Foo2()
        {
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        [WorkItem(1234)]
        public void Foo3()
        {
        }

        [TestCase("")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
//       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Noncompliant
        public void Foo4(string s)
        {
        }

        [TestCase("")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // Reenable when something
        public void Foo5(string s)
        {
        }

        [TestCase("")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        [WorkItem(1234)]
        public void Foo6(string s)
        {
        }

        [TestCaseSource("DivideCases")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        // Noncompliant@-1
        public void DivideTest(int n, int d, int q)
        {
            Assert.AreEqual(q, n / d);
        }

        static object[] DivideCases =
        {
            new object[] { 12, 3, 4 },
            new object[] { 12, 2, 6 },
            new object[] { 12, 4, 3 }
        };
    }
}

namespace Tests.Diagnostics3
{
    using Xunit;

    class XUnitClass
    {
        [Fact]
        [Ignore]
//       ^^^^^^ Noncompliant
        public void Foo1()
        {
        }

        [Fact]
        [Ignore] // Reenable when something
        public void Foo2()
        {
        }

        [Fact]
        [Ignore]
        [WorkItem(1234)]
        public void Foo3()
        {
        }

        [Theory]
        [InlineData("")]
        [Ignore]
//       ^^^^^^ Noncompliant
        public void Foo4(string s)
        {
        }

        [Theory]
        [InlineData("")]
        [Ignore] // Reenable when something
        public void Foo5(string s)
        {
        }

        [Theory]
        [InlineData("")]
        [Ignore]
        [WorkItem(1234)]
        public void Foo6(string s)
        {
        }
    }
}
