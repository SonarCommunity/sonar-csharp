﻿namespace MicrosoftTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    class TestSuite
    {
        public void Method()
        {
            [TestMethod]
            void NestedTest() { } // Noncompliant {{Make this test method a public method instead of a local function.}}

            [DataTestMethod]
            void NestedDataTest() { } // Noncompliant
        }
    }
}

namespace NUnitTests
{
    using NUnit.Framework;

    [TestFixture]
    class TestSuite
    {
        public void Method()
        {
            [Test]
            void NestedTest() { } // Noncompliant

            [TestCase(42)]
            void NestedTestCase() { } // Noncompliant
        }
    }
}

namespace XUnitTests
{
    using Xunit;

    class TestSuite
    {
        public void Method()
        {
            [Fact]
            void NestedFact() { } // Noncompliant

            [Theory]
            void NestedTheory() { } // Noncompliant
        }
    }
}
