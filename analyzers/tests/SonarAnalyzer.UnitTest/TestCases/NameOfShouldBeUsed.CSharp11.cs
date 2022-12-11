﻿using System;

namespace Tests.Diagnostics
{
    class Program
    {
        public void Method_With_RawStringLiterals(int arg1)
        {
            if (arg1 < 0)
                throw new Exception("""arg1"""); // Noncompliant
            else if (arg1 < 100)
                throw new ArgumentException("""Bad parameter name""", """arg1"""); // Noncompliant
            else if (arg1 < 1000)
                throw new ArgumentOutOfRangeException("""
                    arg1
                    """); // Noncompliant@-2
        }


        public void Method_With_NewLinesInStringInterpolation(int arg1)
        {
            string argName = "arg1";
            if (arg1 < 0)
            {
                throw new Exception($"{
                    arg1 switch
                    {
                        < 0 => "arg1",  // Noncompliant
                        _ => "Can't touch this",
                    }}");
            }
            else
            {
                throw new Exception($$"""arg1"""); // Noncompliant
            }
        }
    }
}
