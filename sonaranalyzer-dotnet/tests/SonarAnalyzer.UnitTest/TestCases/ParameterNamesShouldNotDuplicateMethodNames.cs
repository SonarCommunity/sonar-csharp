﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    class Program
    {
        public void Method1(string method1, string METHOD1, string argument)
//                  ^^^^^^^ Secondary [0,1]
//                                 ^^^^^^^ Noncompliant@-1 [0] {{Rename the parameter 'method1' so that it does not duplicate the method name.}}
//                                                 ^^^^^^^ Noncompliant@-2 [1] {{Rename the parameter 'METHOD1' so that it does not duplicate the method name.}}
        {
            // Do something
        }
    }

    class WithLocalFunctions
    {
        public void Method()
        {
            void Method1(string Method1) // Compliant - FN
            {
            }

            static void Method2(string Method2) // Compliant - FN
            {
            }
        }
    }
}
