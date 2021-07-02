﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    class GetTypeWithIsAssignableFrom
    {
        void Test(bool b)
        {
            var expr1 = new GetTypeWithIsAssignableFrom();
            var expr2 = new GetTypeWithIsAssignableFrom();

            if (expr1.GetType()/*abcd*/.IsAssignableFrom(expr2.GetType() /*efgh*/)) //Noncompliant {{Use the 'IsInstanceOfType()' method instead.}}
//              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            { }
            if (expr1.GetType().IsInstanceOfType(expr2)) //Compliant
            { }

            if (!typeof(GetTypeWithIsAssignableFrom).IsAssignableFrom(expr1.GetType())) //Noncompliant {{Use the 'is' operator instead.}}
            { }
            var x = typeof(GetTypeWithIsAssignableFrom).IsAssignableFrom(expr1.GetType()); //Noncompliant
            if (expr1 is GetTypeWithIsAssignableFrom) // Noncompliant  {{Use a 'null' check instead.}}
//              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            { }

            if (typeof(GetTypeWithIsAssignableFrom).IsAssignableFrom(typeof(GetTypeWithIsAssignableFrom))) //Compliant
            { }

            var t1 = expr1.GetType();
            var t2 = expr2.GetType();
            if (t1.IsAssignableFrom(t2)) //Compliant
            { }
            if (t1.IsAssignableFrom(c: expr2.GetType())) //Noncompliant
            { }

            if (t1.IsAssignableFrom(typeof(GetTypeWithIsAssignableFrom))) //Compliant
            { }

            Test(t1.IsAssignableFrom(c: expr2.GetType())); //Noncompliant

            if (expr1 is object) // Compliant - "is object" is a commonly used pattern for non-null check
            { }

            if (expr1 is System.Object) // Compliant - "is object" is a commonly used pattern for non-null check
            { }
        }
    }
    class Fruit { }
    sealed class Apple : Fruit { }
    class NonsealedBerry : Fruit { }

    class Program
    {
        static void Main()
        {
            var apple = new Apple();
            var berry = new NonsealedBerry();
            var b = apple.GetType() == typeof(Apple);       // Noncompliant
            b = berry.GetType() == typeof(NonsealedBerry);  // Compliant, nonsealed class
            b = typeof(Apple).IsInstanceOfType(apple);      // Noncompliant
            b = typeof(Apple).IsAssignableFrom(apple.GetType()); // Noncompliant
            b = typeof(Apple).IsInstanceOfType(apple);      // Noncompliant
            var appleType = typeof(Apple);
            b = appleType.IsAssignableFrom(apple.GetType());    // Noncompliant

            b = apple.GetType() == typeof(int?);    // Compliant

            Fruit f = apple;
            b = true && (((f as Apple)) != null);   // Noncompliant
            b = f as Apple == null;                 // Noncompliant
            b = f as Apple == new Apple();

            b = true && ((apple)) is Apple; // Noncompliant
            b = !(apple is Apple);          // Noncompliant
            b = f is Apple;

            var num = 5;
            b = num is int?;
            b = num is float;
        }
    }

    // https://github.com/SonarSource/sonar-dotnet/issues/3605
    public class Repro_3605
    {
        public string StringProperty { get; set; }
        public const string Example = "Lorem Ipsum";

        public void Go(Repro_3605 value)
        {
            bool result = value.StringProperty is Example; // Noncompliant FP for pattern matching
        }
    }

    public class Coverage
    {
        public void Foo()
        {
            var b = typeof(Apple).IsEquivalentTo(null);
            this.IsInstanceOfType("x");
            this.IsAssignableFrom("x");
            this.GetType(null);
            var c = this.GetType() == typeof(Apple);
            var d = GetType() == null;
        }

        public bool IsInstanceOfType(string x) => true;
        public bool IsAssignableFrom(string x) => true;
        public bool GetType(object x) => true;
        public Type GetType() => null;
    }

    public class CoverageWithErrors
    {
        public void Go(CoverageWithErrors arg)
        {
            bool b;
            this.IsInstanceOfType("x");                 // Error [CS1061]: 'Coverage2' does not contain a definition for 'IsInstanceOfType'
            b = arg is UndefinedType;                   // Error [CS0246]: The type or namespace name 'UndefinedType' could not be found
            b = undefined is CoverageWithErrors;        // Error [CS0103]: The name 'undefined' does not exist in the current context
            b = arg.GetType() == typeof(UndefinedType); // Error [CS0246]: The type or namespace name 'UndefinedType' could not be found
            b = arg.GetType() == typeof();              // Error [CS1031]: Type expected
            b = arg.GetType() == typeof;                // Error [CS1031]: Type expected
                                                        // Error@-1 [CS1003]: Syntax error, '(' expected
                                                        // Error@-2 [CS1026]: ) expected
        }
    }
}
