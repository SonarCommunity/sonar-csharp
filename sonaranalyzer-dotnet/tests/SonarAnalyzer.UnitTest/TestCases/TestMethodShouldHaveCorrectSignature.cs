﻿using System;
using Xunit;
using NUnit.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Diagnostics
{
    class MsTestTest
    {
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        private void PrivateTestMethod() // Noncompliant {{Make this test method 'public'.}}
//                   ^^^^^^^^^^^^^^^^^
        {
        }

        [TestMethod]
        protected void ProtectedTestMethod() // Noncompliant
        {
        }

        [TestMethod]
        internal void InternalTestMethod() // Noncompliant
        {
        }

        [TestMethod]
        public async void AsyncTestMethod()  // Noncompliant {{Make this test method non-'async' or return 'Task'.}}
        {
        }

        [TestMethod]
        public void GenericTestMethod<T>()  // Noncompliant {{Make this test method non-generic.}}
        {
        }

        [TestMethod]
        private void MultiErrorsMethod1<T>() // Noncompliant {{Make this test method 'public' and non-generic.}}
        {
        }

        [TestMethod]
        private async void MultiErrorsMethod2<T>() // Noncompliant {{Make this test method 'public', non-generic and non-'async' or return 'Task'.}}
        {
        }

        [TestMethod]
        public async Task DoSomethingAsync() // Compliant
        {
        }
    }

    class XUnitTest
    {

        [Xunit.Fact]
        private void PrivateTestMethod() // Compliant
        {
        }

        [Fact]
        protected void ProtectedTestMethod() // Compliant
        {
        }

        [Fact]
        internal void InternalTestMethod() // Compliant
        {
        }

        [Fact]
        public async void AsyncTestMethod()  // Noncompliant
        {
        }

        [Fact]
        public void GenericTestMethod<T>()  // Noncompliant
        {
        }
    }

    class NUnitTest
    {


        [NUnit.Framework.Test]
        private void PrivateTestMethod() // Noncompliant
        {
        }

        [Test]
        protected void ProtectedTestMethod() // Noncompliant
        {
        }

        [Test]
        internal void InternalTestMethod() // Noncompliant
        {
        }

        [Test]
        public async void AsyncTestMethod()  // Noncompliant
        {
        }

        [Test]
        public void GenericTestMethod<T>()  // Noncompliant
        {
        }
    }
}
