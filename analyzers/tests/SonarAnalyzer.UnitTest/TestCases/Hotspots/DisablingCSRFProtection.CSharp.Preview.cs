﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Net6Poc.DisablingCSRFProtection
{
    internal class TestCases
    {
        public void Bar(IEnumerable<int> collection)
        {
            [IgnoreAntiforgeryToken] int Get() => 1; // Noncompliant

            _ = collection.Select([IgnoreAntiforgeryToken] (x) => x + 1); // Noncompliant

            Action a =[IgnoreAntiforgeryToken] () => { }; // Noncompliant

            Action x = true
                           ? ([IgnoreAntiforgeryToken]() => { }) // Noncompliant
                           :[GenericAttribute<int>]() => { };

            Call([IgnoreAntiforgeryToken] (x) => { }); // Noncompliant
            Call([GenericIgnoreAntiforgeryToken<int>] (x) => { }); // FN
        }

        [GenericIgnoreAntiforgeryToken<int>] // FN
        public void M() { }

        private void Call(Action<int> action) => action(1);
    }
    public class NonGenericAttribute : Attribute { }

    public class GenericAttribute<T> : Attribute { }

    public class GenericIgnoreAntiforgeryToken<T> : IgnoreAntiforgeryTokenAttribute { }
}
