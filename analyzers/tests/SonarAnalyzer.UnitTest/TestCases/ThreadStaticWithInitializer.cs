﻿using System;

namespace Tests.Diagnostics
{
    public class ThreadStaticWithInitializer
    {
        public class Foo
        {
            [ThreadStatic]
            public static object PerThreadObject = new object(); // Noncompliant {{Remove this initialization of 'PerThreadObject' or make it lazy.}}
//                                               ^^^^^^^^^^^^^^

            [ThreadStatic]
            public static object _perThreadObject;

            public static object StaticObject = new object();
        }
    }

    public class ThreadStaticWithInitializerDerivedAttribute
    {
        public class Foo
        {
            [DerivedAttribute]
            public static object PerThreadObject = new object(); // FN for performance reasons we decided not to handle derived classes
        }

        public class DerivedAttribute : ThreadStaticAttribute { }
    }
}
