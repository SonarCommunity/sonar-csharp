﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Diagnostics
{
    public class Program
    {
        public void Foo()
        {
            var result = new Guid(); // Noncompliant {{Use 'Guid.NewGuid()' or 'Guid.Empty' or add arguments to this Guid instantiation.}}
//                       ^^^^^^^^^^

            result = Guid.Empty;
            result = Guid.NewGuid();
            result = new Guid(new byte[0]);

            var testytest = default(Guid); // Noncompliant
//                          ^^^^^^^^^^^^^
            var testytestQualifiedName = default(System.Guid); // Noncompliant

            var anotherTest = new StringBuilder(); // Checking that the rule raises issue only for the Guid new Object instatiation.
            var anotherTestDefault = default(string);
            var anotherTestDefaultQualifiedName = default(System.String);
        }
    }
}
