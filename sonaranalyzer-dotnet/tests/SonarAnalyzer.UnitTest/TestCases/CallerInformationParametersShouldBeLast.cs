﻿using System;
using System.Runtime.CompilerServices;
namespace Tests.Diagnostics
{
    class Program
    {
        public void Method1(string callerFilePath) { }
        public void Method2([CallerFilePath]string callerFilePath) { }
        public void Method3(string other, [CallerFilePath]string callerFilePath) { }
        public void Method4([CallerFilePath]string callerFilePath, string other) { } // Noncompliant {{Move 'callerFilePath' to the end of the parameter list.}}
//                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        public void Method5([CallerFilePath]string callerFilePath, string other, [CallerLineNumber]int callerLineNumber) { }
//                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        public void Method6(string first, [CallerFilePath]string callerFilePath, [CallerLineNumber]int callerLineNumber, string other) { }
//                                                                               ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                                        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ @-1

        public void Method7([CallerFilePath]string callerFilePath,
//                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            [CallerLineNumber]int callerLineNumber,
//          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            [CallerMemberName]string callerMemberName, string other) { }
//          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        public void Method8([CallerMemberName]string callerMemberName = null,
            [CallerFilePath]string callerFilePath = null, [CallerLineNumber]int callerLineNumber = 0) { }

        public Program([CallerFilePath]string callerFilePath, string other) { } // Noncompliant
        public Program(string other, [CallerFilePath]string callerFilePath) { }
    }

    class BaseClass
    {
        public virtual void Method1(string callerFilePath, string other) { }
    }

    interface MyInterface
    {
        void Method2(string callerFilePath, string other) { }
    }

    class DerivedClass : BaseClass, MyInterface
    {
        public override void Method1([CallerFilePath]string callerFilePath, string other) // Compliant, method overriden
        {
        }

        public void Method2([CallerFilePath]string callerFilePath, string other) // Compliant, method from interface
        {
        }
    }

    class InvalidSyntax
    {
        public void Method0() {}
        public void () {}
        public void Method1( { }
        public void Method2) { }
        public void Method3([CallerFilePath]) { }
        public void Method4([CallerFilePath],string other) { }
        public void Method5([CallerFilePath] string, string other) { }
        public void Method6([CallerFilePathAttribute string parameter) { }
        public void Method6([CallerLineNumber][CallerFilePath]string callerFilePath, string other) { } // Noncompliant
    }
}
