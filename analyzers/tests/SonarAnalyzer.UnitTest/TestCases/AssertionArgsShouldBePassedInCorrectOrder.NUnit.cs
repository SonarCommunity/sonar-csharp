﻿using System;
using NUnit.Framework;

namespace Tests.Diagnostics
{
    [TestFixture]
    class Program
    {
        void FakeAssert(object a, object b) { }

        [Test]
        public void Foo()
        {
            var str = "";
            Assert.AreEqual(str, ""); // Noncompliant {{Make sure these 2 arguments are in the correct order: expected value, actual value.}}
//                          ^^^^^^^
            Assert.AreSame(str, ""); // Noncompliant
//                         ^^^^^^^
            Assert.AreNotSame(str, ""); // Noncompliant

            double d = 42;
            Assert.AreEqual(d, 42); // Noncompliant
//                          ^^^^^
            Assert.AreSame(d, 42); // Noncompliant
            Assert.AreEqual(d, 42, 1, "message"); // Noncompliant

            Assert.AreEqual("", str);
            Assert.AreSame("", str);
            Assert.AreEqual(42, d, 1, "message");

            FakeAssert(d, 42);
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/6630
namespace Repro_6630
{
    [TestFixture]
    class Program
    {
        [Test]
        public void Foo()
        {
            var str = "";
            Assert.AreEqual(actual: "", expected: str); // Compliant FN
            Assert.AreEqual(expected: "", actual: str); // Compliant
            Assert.AreEqual(actual: str, expected: ""); // Noncompliant FP
            Assert.AreEqual(expected: str, actual: ""); // Noncompliant

            Assert.AreNotEqual(actual: "", expected: str); // Compliant FN
            Assert.AreSame(actual: "", expected: str); // Compliant FN
            Assert.AreNotSame(actual: "", expected: str); // Compliant FN

            Assert.AreEqual(actual: null, expected: new Program()); // Compliant FN
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/6547
namespace Repro_6547
{
    [TestFixture]
    class Program
    {
        public enum Seasons { Spring, Summer, Autumn, Winter }

        [Test]
        public void TestString()
        {
            string stringToTest = RetrieveString();
            const string constString = "Spring";

            Assert.AreEqual(expected: stringToTest, actual: constString); // FN
            Assert.AreEqual(expected: constString, actual: stringToTest); // Compliant
        }

        [Test]
        public void TestEnum()
        {
            Seasons seasonToTest = RetrieveSeason();

            Assert.AreEqual(expected: seasonToTest, actual: Seasons.Spring); //FN
            Assert.AreEqual(expected: Seasons.Spring, actual: seasonToTest); // Compliant
        }

        public Seasons RetrieveSeason() => Seasons.Spring;
        public string RetrieveString() => "Spring";
    }
}
