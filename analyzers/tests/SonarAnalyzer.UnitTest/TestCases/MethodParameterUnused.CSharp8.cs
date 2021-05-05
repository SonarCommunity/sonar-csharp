﻿using System.Collections.Generic;
using System.Linq;

namespace Tests.TestCases
{
    public interface IWithDefaultImplementation
    {
        decimal Count { get; set; }
        decimal Price { get; set; }

        void Reset(int a);     //Compliant

        //Default interface methods
        decimal Total()
        {
            return Count * Price;
        }

        decimal Total(decimal Discount)
        {
            return Count * Price * (1 - Discount);
        }

        decimal Total(string unused)    // Compliant, because it's interface member
        {
            return Count * Price;
        }
    }

    public class StaticLocalFunctions
    {
        public int DoSomething(int a)   // Compliant
        {
            static int LocalFunction(int x, int seed) => x + seed;
            static int BadIncA(int x, int seed) => x + 1;   //Noncompliant
            static int BadIncB(int x, int seed)             //Noncompliant
            {
                seed = 1;
                return x + seed;
            }
            static int BadIncRecursive(int x, int seed)     //Noncompliant
            {
                seed = 1;
                if (x > 1)
                {
                    return BadIncRecursive(x - 1, seed);
                }
                return x + seed;
            }

            return LocalFunction(a, 42) + BadIncA(a, 42) + BadIncB(a, 42) + BadIncRecursive(a, 42);
        }

        // https://github.com/SonarSource/sonar-dotnet/issues/4377
        private static bool Foo(IEnumerable<int> a, int b) // Noncompliant FP
        {
            bool InsideFoo(int x) => x.Equals(b);
            bool CallInsideFoo(IEnumerable<int> numbers) => numbers.Any(x => false | InsideFoo(x));

            return CallInsideFoo(a);
        }

        public void Method()
        {
            void WithMultipleParameters(int a,
                                        int b, // Noncompliant
                                        int c,
                                        int d) // Noncompliant
            {
                var result = a + c;
            }

            static void WithMultipleParametersStatic(int a,
                                                     int b, // Noncompliant
                                                     int c,
                                                     int d) // Noncompliant
            {
                var result = a + c;
            }
        }

        // See: https://github.com/SonarSource/sonar-dotnet/issues/3803
        private void AddValue(uint id1, uint id2, string value)
        {
            var x = new Dictionary<(uint, uint), string>();
            x[(id1, id2)] = value;
        }
    }

    public class SwitchExpressions
    {
        public int DoSomething(int a, bool b)
        {
            return b switch
            {
                true => a,
                _ => 0
            };
        }
    }

    public class UsageInRange
    {
        public void DoSomething(int a, int b)
        {
            var list = new string[] { "a", "b", "c" };
            var sublist = list[a..];
            System.Range r = ..^b;
            sublist = list[r];
        }
    }

    // https://github.com/SonarSource/sonar-dotnet/issues/3255
    public class Repro_3255
    {
        private string UsedInTuple(string value)
        {
            var x = (value, 7);
            return x.value;
        }
    }
}
