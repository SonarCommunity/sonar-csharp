﻿using System;

namespace Tests.Diagnostics
{
    class MyException : NotImplementedException { }

    class Program
    {
        void Foo()
        {
            throw new NotImplementedException(); // Noncompliant {{Implement this method or throw 'NotSupportedException' instead.}}
//          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        }

        void Bar()
        {
            throw new MyException(); // Compliant - we don't check inheritance
        }

        void FooBar()
        {
            var ex = new NotImplementedException(); // Compliant - not thrown
        }

        void FooBar2()
        {
            var ex = new NotImplementedException();
            throw ex; // Noncompliant
//          ^^^^^^^^^
        }

        void NotImplemented() =>
            throw new NotImplementedException(); // FN
    }

    interface IInterface
    {
        void FooBar() // Compliant - Default interface methods can be used to extend an already existing API while keeping backwards compatibility.
        {
        }
    }

    public class WithLocalFunctions
    {
        public void Method()
        {
            void Foo()
            {
                throw new NotImplementedException(); // Noncompliant
//              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            }

            static void Bar()
            {
                throw new NotImplementedException(); // Noncompliant
            }
        }
    }
}
