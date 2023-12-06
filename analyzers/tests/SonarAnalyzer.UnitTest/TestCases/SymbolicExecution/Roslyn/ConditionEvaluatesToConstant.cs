﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Tests.Diagnostics
{
    public class ConditionEvaluatesToConstant
    {
        private const bool t = true;
        private const bool f = false;

        public void LoopsWithBreak(object o1, object o2, object o3)
        {
            bool c1, c2, c3;
            c1 = c2 = c3 = true;

            while (c1) // Noncompliant
            {
                if (o1 != null)
                    break;
            }

            do
            {
                if (o2 != null)
                    break;
            } while (c2); // Noncompliant

            for (int i = 0; c3; i++) // Noncompliant
            {
                if (o3 != null)
                    break;
            }
        }

        public void DoesNotRaiseForConst()
        {
            if (t) // Compliant - no issue is raised for const fields.
            {
                Console.WriteLine("Do stuff");
            }
        }

        public void NotExecutedLoops(object o1, object o2, object o3)
        {
            bool c1, c2, c3;
            c1 = c2 = c3 = false;

            while (c1)                  // Noncompliant {{Change this condition so that it does not always evaluate to 'False'. Some code paths are unreachable.}}
            //     ^^
            {
                if (o1 != null)         // Secondary
                    break;
            }

            do
            {
                if (o2 != null)
                    break;
            } while (c2);               // Noncompliant {{Change this condition so that it does not always evaluate to 'False'.}}
            //       ^^

            for (int i = 0; c3; i++)    // Noncompliant {{Change this condition so that it does not always evaluate to 'False'. Some code paths are unreachable.}}
                                        // Secondary@-1 ^33#239
            {
                if (o3 != null)
                                        // secondary location starts at incrementor and ends at the end of the above line
                    break;
            }
        }

        public void BreakInLoop(object o)
        {
            bool c = true;
            while (c)   // Noncompliant
            {
                if (o != null)
                    break;
            }
        }

        public void ReturnInLoop(object o)
        {
            bool c = true;
            while (c)   // Noncompliant
            {
                if (o != null)
                    return;
            }
        }

        public void ThrowInLoop(object o)
        {
            bool c = true;
            while (c)   // Noncompliant
            {
                if (o != null)
                    throw new Exception();
            }
        }

        public void ConstField(bool a, bool b)
        {
            var x = t || a || b; // Compliant t is const

            if (t == true) // Noncompliant
            {
                Console.WriteLine("");
            }
        }

        public void Foo1(bool a, bool b)
        {
            var l = true;
            var x = l || a || b;
            //      ^                       Noncompliant
            //           ^^^^^^             Secondary@-1
        }

        public void Foo2(bool a, bool b)
        {
            var l = true;
            var x = ((l)) || a || b;
            //        ^                     Noncompliant
            //               ^^^^^^         Secondary@-1

        }

        public void Foo3(bool a, bool b)
        {
            var l = true;
            var x = ((l || a)) || b;
            //        ^                     Noncompliant
            //             ^^^^^^^^         Secondary@-1
        }

        public void Foo4(bool a, bool b)
        {
            var l = true;
            var m = false;
            var x = ((m || l)) || a || b;
            //        ^                     Noncompliant
            //             ^                Noncompliant@-1
            //                    ^^^^^^    Secondary@-2

        }

        public void Foo5(bool a, bool b)
        {
            var l = true;
            var m = false;
            var x = ((m && l)) || a || b;
            //        ^                     Noncompliant
            //             ^                Secondary@-1
        }

        public void Foo6(bool a, bool b)
        {
            var l = true;
            var x = l || a ? a : b;
            //      ^                       Noncompliant
            //           ^                  Secondary@-1
            //                   ^          Secondary@-2
        }

        public void Foo7(bool a, bool b)
        {
            var l = true;
            if ((l || a ? a : b) || b)
            //   ^                          Noncompliant
            //        ^                     Secondary@-1
            //                ^             Secondary@-2
            {
            }
        }

        public void Foo8(bool a, bool b)
        {
            a = true;
            _ = a && b;
            //  ^       Noncompliant
        }

        void Pointer(int* a) // Error [CS0214]
        {
            if (a != null)  // Error [CS0214]
                            // Compliant
            {
            }
        }

        void Nameof(string s)
        {
            if (null == nameof(Method1)) // Noncompliant
            {
            }
        }

        public void Method1()
        {
            var b = true;
            if (b)                      // Noncompliant
            //  ^
            {
                Console.WriteLine();
            }
            else
            {
                // secondary location covers all unreachable code blocks:
                Console.WriteLine(1);    // Secondary ^17#131
                while (b)
                {
                    Console.WriteLine(2);
                }   // the secondary location ends at the end of the above line
            }

            Console.WriteLine();
        }

        public void Method2()
        {
            var b = true;
            if (b)                      // Noncompliant
            {
                Console.WriteLine();
            }

            if (!b)                     // Noncompliant
            {
                Console.WriteLine();    // Secondary
            }

            Console.WriteLine();
        }

        public void Method2Literals()
        {
            if (true)   // Compliant
            {
                Console.WriteLine();
            }

            if (false)  // Compliant
            {
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        public void Method3()
        {
            bool b;
            TryGet(out b);
            if (b) { }
        }

        private void TryGet(out bool b) { b = false; }

        public void Method5(bool cond)
        {
            while (cond)
            {
                Console.WriteLine();
            }

            var b = true;
            while (b)               // Noncompliant
            {
                Console.WriteLine();
            }

            Console.WriteLine();    // Secondary
        }

        public void Method6(bool cond)
        {
            var i = 10;
            while (i < 20)
            {
                i = i + 1;
            }

            var b = true;
            while (b)               // Noncompliant
            {
                Console.WriteLine();
            }

            Console.WriteLine();    // Secondary
        }

        public void Method7(bool cond)
        {
            while (true)            // Compliant
            {
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        public void Method8(bool cond)
        {
            foreach (var item in new int[][] { new int[] { 1, 2, 3 } })
            {
                foreach (var i in item)
                {
                    Console.WriteLine();
                }
            }
        }

        public void Method9_For(bool cond)
        {
            for (;;) // Not reporting on this
            {

            }
        }

        public void FixedCountLoop(bool arg)
        {
            var b = false;
            for (var i = 0; i < 10; i++)
            {
                if (i > 5)
                    b = arg;
            }
            if (b)
            {
                Console.WriteLine();
            }
        }

        const int Limit = 10;

        public void FixedCountLoopWithConstLimit()
        {
            if (Limit == 10)            // Noncompliant
                Console.WriteLine();
            for (var i = 0; i < Limit; i++)
                Console.WriteLine();
            if (Limit == 10)            // Noncompliant
                Console.WriteLine();
        }

        public void Method_Switch()
        {
            int i = 10;
            bool b = true;
            switch (i)
            {
                case 1:         // Noncompliant
                default:
                case 2:         // Noncompliant
                    b = false;
                    break;
                case 3:         // Noncompliant
                    b = false;  // Secondary
                    break;
            }

            if (b)              // Noncompliant
            {
            }
            else
            { }
        }

        public void Method_Switch_NoDefault()
        {
            int i = 10;
            bool b = true;
            switch (i)
            {
                case 1:         // Noncompliant
                case 2:         // Noncompliant
                    b = false;  // Secondary
                    break;
            }

            if (b)              // Noncompliant
            {
            }
            else
            {
            }
        }

        public void Method_Switch_Learn(bool cond)
        {
            switch (cond)
            {
                case true:
                    if (cond) // Noncompliant
                    {
                        Console.WriteLine();
                    }
                    break;
            }
        }

        public bool Property1
        {
            get
            {
                var a = new Action(() =>
                {
                    var b = true;
                    if (b)                      // Noncompliant
                    {
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine();    // Secondary
                    }
                });
                return true;
            }
            set
            {
                value = true;
                if (value)                  // Noncompliant
//                  ^^^^^
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine();    // Secondary
                }
            }
        }

        public static bool T
        {
            get
            {
                return t;
            }
        }

        public void Method_Complex()
        {
            bool guard1 = true;
            bool guard2 = true;
            bool guard3 = true;

            while (GetCondition())
            {
                if (guard1)
                {
                    guard1 = false;
                }
                else
                {
                    if (guard2)         // Noncompliant FP: loop is only analyzed twice
                    {
                        guard2 = false;
                    }
                    else
                    {
                        guard3 = false; // Secondary FP
                    }
                }
            }

            if (guard3)                 // Noncompliant FP: loop is only analyzed twice
            {
                Console.WriteLine();
            }
        }

        public void Method_Complex_2()
        {
            var x = false;
            var y = false;

            while (GetCondition())
            {
                while (GetCondition())
                {
                    if (x)
                    {
                        if (y)
                        {
                        }
                    }
                    y = true;
                }
                x = true;
            }
        }
        static object GetObject() { return null; }
        public void M()
        {
            var o1 = GetObject();
            object o2 = null;
            if (o1 != null)
            {
                if (o1.ToString() != null)
                {
                    o2 = new object();
                }
            }
            if (o2 == null)
            {

            }
        }

        public void NullableStructs()
        {
            int? i = null;

            if (i == null)              // Noncompliant, always true
            {
                Console.WriteLine(i);
            }

            i = new Nullable<int>();
            if (i == null)              // Noncompliant
            { }

            int ii = 4;
            if (ii == null)             // Noncompliant, always false
            {
                Console.WriteLine(ii);  // Secondary
            }
        }

        private static bool GetCondition()
        {
            return true;
        }

        public void Lambda(bool condition)
        {
            var fail = false;
            Action a = new Action(() => { fail = condition; });
            a();
            if (fail) // Noncompliant FP
            {
            }
        }

        public void Constraint(bool cond)
        {
            var a = cond;
            var b = a;
            if (a)
            {
                if (b) // FN: requires relation support
                {

                }
            }
        }

        public void Stack(bool cond)
        {
            var a = cond;
            var b = a;
            if (!a)
            {
                if (b) // FN: requires relation support
                {
                }
            }

            var fail = false;
            Action ac = new Action(() => { fail = cond; });
            ac();
            if (!fail) // Noncompliant FP
            {
            }
        }

        public void BooleanBinary(bool a, bool b)
        {
            if (a & !b)
            {
                if (a) { }          // FN: engine doesn't learn BoolConstraints from binary operators
                if (b) { }          // FN: engine doesn't learn BoolConstraints from binary operators

            }

            if (!(a | b))
            {
                if (a) { }          // FN: engine doesn't learn BoolConstraints from binary operators

            }

            if (a ^ b)
            {
                if (!a ^ !b) { }    // FN: engine doesn't learn BoolConstraints from binary operators
            }

            a = false;
            if (a & b) { }          // Noncompliant

            a = a & true;
            if (a)                  // Noncompliant
            { }

            a = a | true;
            if (a)                  // Noncompliant
            { }

            a = a^ true;
            if (a)                  // Noncompliant
            { }
        }

        public void BooleanBinary_CompoundAssignments(bool a, bool b)
        {
            a = false;

            a &= true;
            if (a)
            { }              // FN: engine doesn't learn BoolConstraints from binary operators

            a |= true;
            if (a)
            { }              // FN: engine doesn't learn BoolConstraints from binary operators

            a ^= true;
            if (a)
            { }              // FN: engine doesn't learn BoolConstraints from binary operators
        }

        public void IsAsExpression(object o)
        {
            if (o is string) { }
            object oo = o as string;
            if (oo == null) { }

            o = null;
            if (o is object) { } // Noncompliant
            oo = o as object;
            if (oo == null) { }  // Noncompliant
        }

        public void Equals(bool b)
        {
            var a = true;
            if (a == b)
            {
                if (b) { }  // Noncompliant
            }
            else
            {
                if (b) { }  // Noncompliant
            }

            if (!(a == b))
            {
                if (b) { }  // Noncompliant
            }
            else
            {
                if (b) { }  // Noncompliant
            }
        }

        public void NotEquals(bool b)
        {
            var a = true;
            if (a != b)
            {
                if (b) { }  // Noncompliant
            }
            else
            {
                if (b) { }  // Noncompliant
            }

            if (!(a != b))
            {
                if (b) { }  // Noncompliant
            }
            else
            {
                if (b) { }  // Noncompliant
            }
        }

        public void EqRelations(bool a, bool b)
        {
            if (a == b)
            {
                if (b == a) { }     // FN: requires relation support
                if (b == !a) { }    // FN: requires relation support
                if (!b == !!a) { }  // FN: requires relation support
                if (!(a == b)) { }  // FN: requires relation support
            }
            else
            {
                if (b != a) { }     // FN: requires relation support
                if (b != !a) { }    // FN: requires relation support
                if (!b != !!a) { }  // FN: requires relation support

            }

            if (a != b)
            {
                if (b == a) { }     // FN: requires relation support
            }
            else
            {
                if (b != a) { }     // FN: requires relation support
            }
        }

        public void RelationshipWithConstraint(bool a, bool b)
        {
            if (a == b && a) { if (b) { } } // FN: requires relation support
        //                         ~
            if (a != b && a)
            {
                if (b) { }                  // FN: requires relation support
            }

            if (a && b)
            {
                if (a == b) { }             // Noncompliant
            }

            if (a && b && a == b) { }       // Noncompliant
//                        ^^^^^^

            a = true;
            b = false;
            if (a &&                        // Noncompliant
                b)                          // Noncompliant
            {
            }
        }

        private static void BackPropagation(object a, object b)
        {
            if (a == b && b == null)
            {
                a.ToString();
            }
        }

        public void RefEqualsImpliesValueEquals(object a, object b)
        {
            if (object.ReferenceEquals(a, b))
            {
                if (object.Equals(a, b)) { }    // FN
                if (Equals(a, b)) { }           // FN
                if (a.Equals(b)) { }            // FN
            }

            if (this == a)
            {
                if (this.Equals(a)) { } // FN
                if (Equals(a)) { }      // FN
            }
        }

        public void ValueEqualsDoesNotImplyRefEquals(object a, object b)
        {
            if (object.Equals(a, b)) // 'a' could override Equals, so this is not a ref equality check
            {
                if (a == b) { } // Compliant
            }
        }

        public void ReferenceEqualsMethodCalls(object a, object b)
        {
            if (object.ReferenceEquals(a, b))
            {
                if (a == b) { } // FN
            }

            if (a == b)
            {
                if (object.ReferenceEquals(a, b)) { } // FN
            }
        }

        public void ReferenceEqualsMethodCallWithOpOverload(ConditionEvaluatesToConstant a, ConditionEvaluatesToConstant b)
        {
            if (object.ReferenceEquals(a, b))
            {
                if (a == b) { } // FN
            }

            if (a == b)
            {
                if (object.ReferenceEquals(a, b)) { } // Compliant, == is doing a value comparison above.
            }
        }

        public void ReferenceEquals(object a, object b)
        {
            if (object.ReferenceEquals(a, b)) { }

            if (object.ReferenceEquals(a, a)) { } // FN

            a = null;
            if (object.ReferenceEquals(null, a)) { }    // Noncompliant
            if (object.ReferenceEquals(a, a)) { }       // Noncompliant

            if (object.ReferenceEquals(null, new object())) { } // Noncompliant

            // Because of boxing:
            int i = 10;
            if (object.ReferenceEquals(i, i)) { }   // FN

            int? ii = null;
            int? jj = null;
            if (object.ReferenceEquals(ii, jj)) { } // Noncompliant

            jj = 10;
            if (object.ReferenceEquals(ii, jj)) { } // Noncompliant
        }

        public void ReferenceEqualsBool()
        {
            bool a, b;
            a = b = true;
            if (object.ReferenceEquals(a, b)) { }   // FN

            if (object.Equals(a, b)) { }   // Noncompliant {{Change this condition so that it does not always evaluate to 'True'.}}
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public static bool operator ==(ConditionEvaluatesToConstant a, ConditionEvaluatesToConstant b)
        {
            return false;
        }

        public static bool operator !=(ConditionEvaluatesToConstant a, ConditionEvaluatesToConstant b)
        {
            return false;
        }

        public void StringEmpty(string s1)
        {
            string s = null;
            if (string.IsNullOrEmpty(s)) { } // Noncompliant
            if (string.IsNullOrWhiteSpace(s)) { } // Noncompliant
            if (string.IsInterned(s) != null) { }
            s = "";
            if (string.IsNullOrEmpty(s)) { } // FN

            if (string.IsNullOrWhiteSpace(s)) { } // FN

            if (string.IsNullOrEmpty(s1)) { } // Compliant, we don't know anything about the argument

            if (string.IsNullOrWhiteSpace(s1)) { } // Compliant

            if (string.IsNullOrEmpty(s1))
            {
                if (string.IsNullOrEmpty(s1)) { } // FN
            }
        }

        public void Comparisons(int i, int j)
        {
            if (i < j)
            {
                if (j < i) { }  // FN
                if (j <= i) { } // FN
                if (j == i) { } // FN
                if (j != i) { } // FN
            }

            if (i <= j)
            {
                if (j < i) { }  // FN
                if (j <= i)
                {
                    if (j == i) { } // FN
                    if (j != i) { } // FN
                }
                if (j == i)
                {
                    if (i >= j) { } // FN
                }
                if (j != i)
                {
                    if (i >= j) { } // FN
                }
            }
        }

        void ValueEquals(int i, int j)
        {
            if (i == j)
            {
                if (Equals(i, j)) { } // FN
                if (i.Equals(j)) { }  // FN
            }
        }

        void DefaultExpression(object o)
        {
            if (default(object) == null) { } // Noncompliant
            int? nullableInt = null;
            if (nullableInt == null) { } // Noncompliant
            if (default(int?) == null) { } // Noncompliant

            if (default(System.IO.FileAccess) != null) { } // Noncompliant
            if (default(float) != null) { } // Noncompliant
        }

        void DefaultGenericClassExpression<TClass>(TClass arg)
            where TClass : class
        {
            if (default(TClass) == null) { } // Noncompliant
        }

        void DefaultGenericStructExpression<TStruct>(TStruct arg)
            where TStruct : struct
        {
            if (default(TStruct) != null) { } // Noncompliant
                                              // Error@-1 [CS0019]
        }

        void DefaultUnconstrainedGenericExpression<T>(T arg)
        {
            if (default(T) == null) { } // We know nothing about T
        }

        void ConditionalAccessNullPropagation(object o)
        {
            if (o == null)
            {
                if (o?.ToString() == null) { }
                //  ^                               Noncompliant
                //    ^^^^^^^^^^^                   Secondary@-1
                //  ^^^^^^^^^^^^^^^^^^^^^           Noncompliant@-2
                if (o?.GetHashCode() == null) { }
                //  ^                               Noncompliant
                //    ^^^^^^^^^^^^^^                Secondary@-1
                //  ^^^^^^^^^^^^^^^^^^^^^^^^        Noncompliant@-2
            }
        }

        void Cast()
        {
            var i = 5;
            var o = (object)i;
            if (o == null) { } // Noncompliant

            var x = (ConditionEvaluatesToConstant)o; // This would throw and invalid cast exception
            if (x == null) { } // Noncompliant

        }

        public async Task NotNullAfterAccess(object o, int[,] arr, IEnumerable<int> coll, Task task)
        {
            Console.WriteLine(o.ToString());
            if (o == null) { } // Noncompliant


            Console.WriteLine(arr[42, 42]);
            if (arr == null) { } // Noncompliant


            foreach (var item in coll)
            {
            }
            if (coll == null) { } // Noncompliant


            await task;
            if (task == null) { } // FN

        }

        public void EnumMemberAccess()
        {
            var m = new MyClass();
            Console.WriteLine(m.myEnum);
            m = null;
            if (m?.myEnum == MyEnum.One)
            //  ^                               Noncompliant
            //    ^^^^^^^                       Secondary@-1
            //  ^^^^^^^^^^^^^^^^^^^^^^^         Noncompliant@-2
            {
            }
        }

        int field;
        int GetValue() { return 42; }
        public void NullabiltyTest()
        {
            if (field == null)  // Noncompliant
            {
            }

            int i = GetValue();
            if (i == null)      // Noncompliant
            {
            }
        }

        public void EqualsTest32(object o)
        {
            var o2 = o;
            if (o == o2) { }                        // FN
            if (object.ReferenceEquals(o, o2)) { }  // FN
            if (o.Equals(o2)) { }                   // FN
            if (object.Equals(o2, o)) { }           // FN


            int i = 1;
            int j = i;
            if (i == j)                             // Noncompliant
            {
            }

            if (i.Equals(j)) { }                    // Noncompliant {{Change this condition so that it does not always evaluate to 'True'.}}
            if (object.Equals(i, j)) { }            // Noncompliant {{Change this condition so that it does not always evaluate to 'True'.}}
        }

        async Task Test_Await_Constraint(Task<string> t)
        {
            if (t != null)
            {
                var o = await t;
                if (o == null) { } // Compliant, might be null
            }
        }

        enum MyEnum
        {
            One, Two
        }

        class MyClass
        {
            public MyEnum myEnum;
        }

        public void Assert(bool condition, object o1)
        {
            Debug.Assert(condition);

            if (condition) // Noncompliant
            {
            }

            Trace.Assert(condition); // Compliant

            if (o1 != null)
            {
                Debug.Assert(o1 == null, "Some message", "More details", 1, 2, 3); // Compliant
            }
        }

        public void Assert(object o1)
        {
            System.Diagnostics.Debug.Assert(o1 != null);
            System.Diagnostics.Debug.Assert(o1 == null);
        }

        void ComparisonTransitivity(int a, int b, int c)
        {
            if (a == b && b < c)
            {
                if (a >= c) { }  // FN

            }
            if (a == b && b <= c)
            {
                if (a > c) { }  // FN

            }
            if (a > b && b > c)
            {
                if (a <= c) { } // FN

            }
            if (a > b && b >= c)
            {
                if (a <= c) { } // FN

            }
            if (a >= b && b >= c)
            {
                if (a < c) { }  // FN

            }
            if (a >= b && c <= b)
            {
                if (a < c) { }  // FN

            }
            if (a >= b && c >= b)
            {
                if (a < c) { }
            }
        }

        void RefEqTransitivity(Comp a, Comp b, Comp c)
        {
            if (a == b && b == c)
            {
                if (a != c) { }         // FN

            }
            if (a.Equals(b) && b == c)
            {
                if (a != c) { }
                if (a == c) { }
                if (a.Equals(c)) { }    // FN
                if (!a.Equals(c)) { }   // FN

            }
            if (a > b && b == c)
            {
                if (a <= c) { }         // FN

            }
        }

        void ValueEqTransitivity(Comp a, Comp b, Comp c)
        {
            if (a == b && b.Equals(c))
            {
                if (a.Equals(c)) { }    // FN
            }
            if (a.Equals(b) && b.Equals(c))
            {
                if (a != c) { }
                if (a == c) { }
                if (a.Equals(c)) { }    // FN
                if (!a.Equals(c)) { }   // FN

            }
            if (a > b && b.Equals(c))
            {
                if (a > c) { }          // FN
                if (a <= c) { }         // FN

            }
            if (!a.Equals(b) && b.Equals(c))
            {
                if (a.Equals(c)) { }    // FN

                if (a == c) { }         // FN

            }
            if (a != b && b.Equals(c))
            {
                if (a.Equals(c)) { }
                if (a == c) { }
            }
        }

        void NeqEqTransitivity(object a, object b, object c)
        {
            if (a == c && a != b)
            {
                if (b == c) { }         // FN

                if (b.Equals(c)) { }
            }

            if (a == c && !a.Equals(b))
            {
                if (b == c) { }         // FN

                if (b.Equals(c)) { }    // FN

            }
        }

        public void LiftedOperator()
        {
            int? i = null;
            int? j = 5;

            if (i < j) // Noncompliant
            {
            }

            if (i <= j) // Noncompliant
            {
            }

            if (i > j) // Noncompliant
            {
            }

            if (i >= j) // Noncompliant
            {
            }

            if (i > 0) // Noncompliant
            {
            }

            if (i >= 0) // Noncompliant
            {
            }

            if (i < 0) // Noncompliant
            {
            }

            if (i <= 0) // Noncompliant
            {
            }

            if (j > null) // Noncompliant
            {
            }

            if (j >= null) // Noncompliant
            {
            }

            if (j < null) // Noncompliant
            {
            }

            if (j <= null) // Noncompliant
            {
            }
        }

        unsafe void Pointers(int* a, int* b)
        {
            if (a < b)
            {
            }
        }

        class Singleton
        {
            private static object syncRoot = new object();

            private static Singleton instance;

            public static Singleton Instance
            {
                get
                {
                    if (instance == null)
                    {
                        lock (syncRoot)
                        {
                            if (instance == null) // We don't check conditions that are in lock statements.
                            {
                                instance = new Singleton();
                            }
                        }
                    }
                    return instance;
                }
            }
        }

        class Comp
        {
            public static bool operator <(Comp a, Comp b) { return true; }
            public static bool operator >(Comp a, Comp b) { return true; }
            public static bool operator >=(Comp a, Comp b) { return true; }
            public static bool operator <=(Comp a, Comp b) { return true; }
        }

        struct MyStructWithNoOperator
        {
            public static void M(MyStructWithNoOperator a)
            {
                if (a == null) // Noncompliant, also a compiler error
                               // Error@-1 [CS0019]
                {
                }
            }
        }

        struct MyStructWithOperator
        {
            public bool Unknown;

            public static bool operator ==(MyStructWithOperator? a, MyStructWithOperator? b)
            {
                return a.Value.Unknown;
            }

            public static bool operator !=(MyStructWithOperator? a, MyStructWithOperator? b)
            {
                return a.Value.Unknown;
            }

            public static void M(MyStructWithOperator a)
            {
                if (a == null) // Noncompliant FP: custom operator
                {
                }
            }
        }

        public class NullableCases
        {
            void Case1()
            {
                bool? b1 = true;
                if (b1 == true) // Noncompliant
                {

                }
            }

            void Case2(bool? i)
            {
                if (i == null)
                {

                }
                if (i == true)
                {

                }
                if (i == false)
                {

                }

                i = null;
                if (i == null) // Noncompliant
                {

                }
                if (i == true) // Noncompliant
                {

                }
                if (i == false) // Noncompliant
                {

                }

                i = true;
                if (i == null) // Noncompliant
                {

                }
                if (i == true) // Noncompliant
                {

                }
                if (i == false) // Noncompliant
                {

                }

                i = false;
                if (i == null) // Noncompliant
                {

                }
                if (i == true) // Noncompliant
                {

                }
                if (i == false) // Noncompliant
                {

                }

                bool? b2 = true;
                if (b2 == false) // Noncompliant
                {
                }

                bool? b3 = true;
                if (b3 == null) // Noncompliant
                {
                }

                bool? b4 = null;
                if (b4 == true) // Noncompliant
                {
                }

                bool? b5 = null;
                if (b5 == false) // Noncompliant
                {
                }


                bool? b6 = null;
                if (b6 == null) // Noncompliant
                {
                }
                bool? b7 = true;
                if (b7 == true) // Noncompliant
                {
                }
                bool? b8 = false;
                if (b8 == false) // Noncompliant
                {
                }

            }

            void Case3(bool? b)
            {
                if (b == null)
                {
                    if (null == b) // Noncompliant
                    {
                        b.ToString();
                    }
                }
                else
                {
                    if (b != null) // Noncompliant
                    {
                        b.ToString();
                    }
                }
            }

            void Case4(bool? b)
            {
                if (b == true)
                {
                    if (true == b) // Noncompliant
                    {
                        b.ToString();
                    }
                }
            }

            void Case5(bool? b)
            {
                if (b == true)
                {
                }
                else if (b == false)
                {
                }
                else
                {
                }
            }

            void Case6(bool? b)
            {
                if (b == null)
                {
                }
                else if (b == true)
                {
                }
                else
                {
                }
            }

            void Case7(bool? b)
            {
                if (b == null)
                {
                    if (b ?? false)
                    //  ^           Noncompliant
                    {

                    }
                }
            }

            void Case8(bool? b)
            {
                if (b != null)
                {
                    if (b.HasValue) // Noncompliant
                    {
                    }
                }
            }

            void Case9(bool? b)
            {
                if (b == true)
                {
                    var x = b.Value;
                    if (x == true) // Noncompliant
                    {
                    }
                }
            }

            void Case10(int? i)
            {
                if (i == null)
                {
                    if (i.HasValue) // Noncompliant
                    {
                    }
                }
            }

            // https://github.com/SonarSource/sonar-dotnet/issues/4755
            public void IfElseIfElseFlow_FromCast(object value)
            {
                var b = (bool?)value;
                if (b == true)
                {
                    Console.WriteLine("true");
                }
                else if (b == false)            // Compliant
                {
                    Console.WriteLine("false");
                }
                else
                {
                    Console.WriteLine("null");
                }
            }

            public void IfElseIfElseFlow_DirectValue(bool? b)
            {
                if (b == true)
                {
                    Console.WriteLine("true");
                }
                else if (b == false)
                {
                    Console.WriteLine("false");
                }
                else
                {
                    Console.WriteLine("null");
                }
            }

            public void IfElseIfElseFlow_KnownNull()
            {
                bool? b = null;
                if (b == true)                  // Noncompliant
                {
                    Console.WriteLine("true");  // Secondary
                }
                else if (b == false)            // Noncompliant
                {
                    Console.WriteLine("false"); // Secondary
                }
                else
                {
                    Console.WriteLine("null");
                }
            }
        }

        public class ConstantExpressionsAreExcluded
        {
            const bool T = true;
            const bool F = false;

            void LocalConstants()
            {
                const bool t = true;
                if (t)                                      // Noncompliant
                {
                    Console.WriteLine();
                }
                const bool f = false;
                if (f)                                      // Noncompliant
                {
                    Console.WriteLine();                    // Secondary
                }
            }
            void Constants()
            {
                if (ConstantExpressionsAreExcluded.T)       // Compliant it's a constant
                {
                    Console.WriteLine();
                }
                if (ConstantExpressionsAreExcluded.F)       // Compliant it's a constant
                {
                    Console.WriteLine();
                }
            }
            void WhileTrue()
            {
                while (ConstantExpressionsAreExcluded.T)    // Compliant it's a constant
                {
                    Console.WriteLine();
                }
            }
            void WhileFalse()
            {
                do
                {
                    Console.WriteLine();
                }
                while (ConstantExpressionsAreExcluded.F);           // Compliant it's a constant
            }
            void Condition()
            {
                var x = ConstantExpressionsAreExcluded.T ? 1 : 2;   // Compliant, T is constant
            }
        }
    }

    public class GuardedTests
    {
        public void Guarded(string s1)
        {
            Guard1(s1);

            if (s1 == null)  // Noncompliant, always flse
            { // this branch is never executed
            }
            else
            {
            }
        }

        public void Guard1<T>([ValidatedNotNull]T value) where T : class { }

        [AttributeUsage(AttributeTargets.Parameter)]
        public sealed class ValidatedNotNullAttribute : Attribute { }
    }

    public class CatchFinally
    {
        public void ObjectsShouldNotBeDisposedMoreThanOnce()
        {
            Stream stream = null;
            try
            {
                stream = File.Open("file", FileMode.Open);
                using (var reader = new StreamReader(stream))
                {
                    // read the file here

                    // StreamReader will dispose the stream automatically; set stream to null
                    // to prevent it from disposing twice (the recommended pattern for S3966)
                    stream = null;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public void Throw(bool condition)
        {
            if (condition)      // Compliant
                throw new Exception();
        }

        public void Rethrow(bool condition)
        {
            try
            {
                Console.WriteLine("may throw");
            }
            catch
            {
                if (condition)  // Compliant
                    throw;
            }
        }

        public void FalseNegatives()
        {
            object o = null;
            try
            {
                Console.WriteLine("Could throw");
            }
            catch
            {
                if (o != null) { } // Noncompliant
                if (o == null) { } // Noncompliant
            }
            finally
            {
                if (o != null) { } // Noncompliant
                if (o == null) { } // Noncompliant
            }
        }
    }

    class UsingStatement
    {
        public void Method()
        {
            var isTrue = true;
            if (isTrue) { }     // Noncompliant
            using (var reader = new StreamReader(""))
            {
                if (isTrue) { } // Noncompliant
            }
            if (isTrue) { }     // Noncompliant
        }
    }

    class AsyncAwait
    {
        object _foo1;
        async Task Foo(Task t)
        {
            object o = null;
            _foo1 = o;
            await t; // awaiting clears the constraints
            if (_foo1 != null) { } // FN
            if (_foo1 == null) { } // FN
            if (o != null) { } // Noncompliant
            if (o == null) { } // Noncompliant
        }
    }

    public class StaticMethods
    {
        object _foo1;
        // https://github.com/SonarSource/sonar-dotnet/issues/947
        void CallToMonitorWaitShouldResetFieldConstraints()
        {
            object o = null;
            _foo1 = o;
            System.Threading.Monitor.Wait(o); // This is a multi-threaded application, the fields could change
            if (_foo1 != null) { } // Noncompliant FP
            if (_foo1 == null) { } // Noncompliant FP
            if (o != null) { } // Noncompliant
            if (o == null) { } // Noncompliant
        }

        void CallToStaticMethodsShouldResetFieldConstraints()
        {
            object o = null;
            _foo1 = o;
            Console.WriteLine(); // This particular method has no side effects
            if (_foo1 != null) { } // Noncompliant
            if (_foo1 == null) { } // Noncompliant
            if (o != null) { } // Noncompliant
            if (o == null) { } // Noncompliant
        }
    }

    class FooContainer
    {
        public bool Foo { get; set; }
    }

    class TestNullConditional
    {
        void First(FooContainer fooContainer, bool bar)
        {
            if (fooContainer?.Foo == false || bar)
            Console.WriteLine(bar ? "1" : "2");
            else
            Console.WriteLine(fooContainer != null
                ?
                "3"
                :
                "4");
        }

        void Second(FooContainer fooContainer)
        {
            if (fooContainer?.Foo != true)
            {
            Console.WriteLine("3");
            if (fooContainer != null)
            {
                Console.WriteLine("4");
            }
            }
        }

        public class Result
        {
            public bool Succeed { get; set; }

            public static Result Test(bool cond)
            {
            if (cond)
            {
                return new Result();
            }
            return null;
            }
        }

        public static void Compliant1(bool cond)
        {
            var result = Result.Test(cond);

            if (result == null || !result.Succeed)
            {
            Console.WriteLine("shorted");
            if (result != null)
            {
                Console.WriteLine("other");
            }
            }

            if (result?.Succeed != true)
            {
            Console.WriteLine("shorted");
            if (result != null)
            {
                Console.WriteLine("other");
            }
            }
        }

        public static void NonCompliant1()
        {
            Result result = null;
            if (result?.Succeed != null)
            //  ^^^^^^                             Noncompliant
            //         ^^^^^^^^                    Secondary@-1
            //  ^^^^^^^^^^^^^^^^^^^^^^^            Noncompliant@-2
            {
                Console.WriteLine("shorted");   // Secondary
                if (result != null)
                {
                    Console.WriteLine("other");
                }
            }
        }

        public static void NonCompliant2()
        {
            Result result = new Result();
            if (result?.Succeed != null)        // Noncompliant
                                                // Noncompliant@-1
            {
                Console.WriteLine("shorted");
                while (result != null)          // Noncompliant
                {
                    Console.WriteLine("other");
                }
            }
        }

        public class A
        {
            public bool booleanVal { get; set; }
        }

        public static void Compliant2()
        {
            A aObj = null;
            if (aObj?.booleanVal ?? false)
            //  ^^^^                            Noncompliant
            //       ^^^^^^^^^^^                Secondary@-1
            //  ^^^^^^^^^^^^^^^^                Noncompliant@-2
            {
                Console.WriteLine("a");
            }
        }

        public static void NonCompliant3()
        {
            A aObj = null;
            if (aObj?.booleanVal == null)
            //  ^^^^                            Noncompliant
            //       ^^^^^^^^^^^                Secondary@-1
            //  ^^^^^^^^^^^^^^^^^^^^^^^^        Noncompliant@-2
            {
                Console.WriteLine("a");
            }

            if (aObj?.booleanVal != null)   // Noncompliant
                                            // Secondary@-1
                                            // Noncompliant@-2
            {
                Console.WriteLine("a");     // Secondary
            }
        }

        public static void Compliant3(A a)
        {

            if (a?.booleanVal == true)
            {
                Console.WriteLine("Compliant");
                return;
            }

            if (a != null)  // Compliant
            {
            }
        }

        public static void NonCompliant4(A a)
        {

            if (a?.booleanVal == null)
            {
                Console.WriteLine("Compliant");
                return;
            }

            if (a != null) // Noncompliant
            {
            }
        }

        public static void Compliant4(A a)
        {
            if (a?.booleanVal == null)
            {
                Console.WriteLine("Compliant");
            }

            if (a != null) // Compliant
            {
            }
        }

        public static void Compliant5(A a)
        {
            while (a?.booleanVal == null ? true : false)    // Compliant
            {
                Console.WriteLine("Compliant");
            }
        }

        public static void NonCompliant5()
        {
            A a = null;
            while (a?.booleanVal == null ? true : false)
            //     ^                                        Noncompliant
            //       ^^^^^^^^^^^                            Secondary@-1
            //     ^^^^^^^^^^^^^^^^^^^^^                    Noncompliant@-2
            //                                    ^^^^^     Secondary@-3
            {
                Console.WriteLine("Compliant");
            }
        }

        public class S
        {
            public string str=null;
        }

        public static void Compliant6(S sObj)
        {
            if (sObj?.str?.Length > 2)
            {
                Console.WriteLine("a");
            }
        }

        public static void NonCompliant6()
        {
            S sObj = null;
            if (sObj?.str?.Length > 2)
            //  ^^^^                                        Noncompliant
            //       ^^^^^^^^^^^^                           Secondary@-1
            //  ^^^^^^^^^^^^^^^^^^^^^                       Noncompliant@-2
            {
                Console.WriteLine("a"); // Secondary
            }
        }
    }

    public class TestNullCoalescing
    {
        public void CompliantMethod(bool? input)
        {
            if (input ?? false)                     // Compliant
            {
                Console.WriteLine("input is true");
            }
            else
            {
                Console.WriteLine("input is false");
            }
        }

        public void CompliantMethod1(bool? input)
        {
            while (input ?? false)                  // Compliant
            {
                Console.WriteLine("input is true");
            }
        }

        public void CompliantMethod2(bool? input, bool input1)
        {
            while ((input ?? false) && input1)      // Compliant
            {
                Console.WriteLine("input is true");
            }
        }

        public void CompliantMethod3(bool? input, bool input1)
        {

            if (input ?? false ? input1 : false)    // Compliant
            {
                Console.WriteLine("input is true");
            }
        }

        public void NonCompliantMethod()
        {
            bool? input = true;
            if (input ?? false)                         // Noncompliant
            {
                Console.WriteLine("input is true");
            }
            else
            {
                Console.WriteLine("input is false");    // Secondary
            }
        }

        public void NonCompliantMethod1()
        {
            bool? input = true;
            while (input ?? false)  // Noncompliant
                                    // FN false is unreachable: Due to the shape of the CFG the issue raised twice, first without then with a secondary location. The second one is ignored by the analyzer.
            {
                Console.WriteLine("input is true");
            }
        }

        public void NonCompliantMethod2(bool? input)
        {
            while ((input ?? false) || true)        // Compliant
            {
                Console.WriteLine("input is true");
            }
        }

        public void NonCompliantMethod3(bool? input, bool input1)
        {
            if ((input ?? false) ? false : false)   // Compliant
            {
                Console.WriteLine("input is true");
            }
        }
    }

    class TryCast
    {
        public void UpCast(string s)
        {
            if (s != null)
            {
                var o = s as object;
                if (o != null)              // Noncompliant: Upcast never fails => o is never null
                    Console.WriteLine(s);
            }
        }

        public void DownCast(object o)
        {
            if (o != null)
            {
                var s = o as string;
                if (s != null)              // Compliant
                    Console.WriteLine(s);
            }
        }

        public void InterfaceCast(IEnumerable<int> e)
        {
            if (e != null)
            {
                var c = e as IComparable<int>;
                if (c != null)              // Compliant
                    Console.WriteLine(c);
            }
        }

        public void NullableUpCast(int i)
        {
            var ni = i as int?;
            if (ni != null)                 // Noncompliant: Upcast never fails => ni is never null
                Console.WriteLine(ni);
        }

        public void NullableDownCast(object o)
        {
            if (o != null)
            {
                var ni = o as int?;
                if (ni != null)             // Compliant
                    Console.WriteLine(ni);
            }
        }
    }

    class Program
    {
        public static string CompliantMethod4(string parameter)
        {
            var useParameter = parameter ?? "non-empty";
            if (useParameter == null || useParameter=="") // Noncompliant
            return "non-empty"; // we don't know if this going to be excuted or not

            return "empty";
        }

        public static string Method1347(string parameter)
        {
            var useParameter = parameter ?? "non-empty";
            if (!string.IsNullOrEmpty(useParameter))
            {
                return "non-empty";
            }
            else
            {
            }
            return "empty";
        }

        static void CompliantMethod5(string[] args)
        {
            var obj = args.Length > 0 ? new Program() : null;

            if (obj?.Cond ?? false) // Compliant
            {
                Console.WriteLine("Foo");
                Console.WriteLine("Bar");
            }
        }

        private bool Cond = new Random().Next() % 2 == 1;
    }

    class Repro2442
    {
        public void Method(bool unknown)
        {
            bool f = false;
            bool t = true;
            if (t) // Noncompliant
            {

            }
            else
            {

            }

            if (f) // Noncompliant
            {

            }
            else
            {

            }

            if (unknown || t) // Noncompliant
            {

            }
            else
            {

            }

            if (unknown && f) // Noncompliant
            {

            }
            else
            {

            }

            if (unknown && t) // Noncompliant
            {

            }
            else
            {

            }

            if (unknown || f) // Noncompliant
            {

            }
            else
            {

            }

            if (unknown && (t)) // Noncompliant
            {

            }
            else
            {

            }
        }

        /// <summary>
        /// A certain combination of condition wrongly considers the else code as dead.
        /// </summary>
        private static void FalsePositive()
        {
            FalsePositive2_Sub(true, false, false);
            FalsePositive2_Sub(true, false, true);

            FalsePositive2_Sub(true, true, true);
            FalsePositive2_Sub(true, true, false);

            FalsePositive2_Sub(false, false, false);
            FalsePositive2_Sub(false, false, true);

            FalsePositive2_Sub(false, true, true);
            FalsePositive2_Sub(false, true, false);

            // Outcome.
            Console.WriteLine((_test1 && _test2 && _test3 && _test4) ? "Went through each test condition" : "Missed at least one test condition");
        }

        private static bool _test1 = false;
        private static bool _test2 = false;
        private static bool _test3 = false;
        private static bool _test4 = false;

        private static void FalsePositive2_Sub(bool testCondition1, bool testCondition2, bool testCondition3)
        {
            bool condition1 = testCondition1;
            bool condition2 = testCondition2;
            bool condition3 = condition2 && testCondition3;

            if (condition2 && condition3 && condition1)
            {
                _test1 = true;
            }
            else if (!condition2 && !condition1)
            {
                _test2 = true;
            }
            else if (condition2
                && condition1
                && !condition3) // Noncompliant
            {
                _test3 = true;
            }
            else
            {
                _test4 = true;
            }
        }

        public void FP_Increment(List<int> list)
        {
            int MaxStepCount = 200;
            var steps = 0;
            while (list.Any())
            {
                if (steps++ > MaxStepCount) // Noncompliant FP
                {
                    return;
                }
            }
        }

        public void FP_Increment_2(List<int> list)
        {
            int MaxStepCount = 200;
            int steps = 0;
            while (list.Any())
            {
                steps = steps + 1;
                if (steps > MaxStepCount) // Noncompliant FP
                {
                    return;
                }
            }
        }
    }

    public class RefArgTest
    {
        public void Method(ref string s, int x) { }
        public void Method1(string infixes)
        {
            if (infixes != null)
            {
                Method(ref infixes, infixes.Length);
                if (infixes == null)    // Noncompliant FP: ref
                {
                    return;
                }
            }
        }

        public void Method2(string infixes)
        {
            if (infixes != null)
            {
                Method(ref infixes, infixes.Length);
                if (infixes != null)    // Noncompliant FP: ref
                {
                    return;
                }
            }
        }

        public void Method3(string infixes)
        {
            if (infixes == null)
            {
                Method(ref infixes, infixes.Length);
                if (infixes == null)    // Noncompliant FP: ref
                {
                    return;
                }
            }
        }

        public void Method4(string infixes)
        {
            if (infixes == null)
            {
                Method(ref infixes, infixes.Length);
                if (infixes != null)    // Noncompliant FP: ref
                {
                    return;
                }
            }
        }

    }

    class ReproForEachFP
    {
        private static bool Repro2348(List<int> list)
        {
            bool containspositive = false;
            bool containsnegative = false;

            foreach (int value in list)
            {
                if (value > 0)
                {
                    containspositive = true;
                }
                else if (value < 0)
                {
                    containsnegative = true;
                }
            }

            return containspositive && !containsnegative; // Compliant
        }

        private static void Repro1187_1()
        {
            bool do1 = false;
            bool do2 = false;
            var items = new int[] { 1, 2, 3 };
            foreach (var item in items)
            {
                switch (item)
                {
                    case 1:
                        do1 = true;
                        break;
                    case 2:
                        do2 = true;
                        break;
                }
            }

            if (do1 && do2)                             // FN, this repro is written badly. This used to be a TP due to the known content of items.
                                                        //     The original intention of the reported issues can be found below.
            {
                throw new InvalidOperationException();
            }
        }

        private static void Repro1187_1_Fixed(int[] items)
        {
            bool do1 = false;
            bool do2 = false;
            foreach (var item in items)
            {
                switch (item)
                {
                    case 1:
                        do1 = true;
                        break;
                    case 2:
                        do2 = true;
                        break;
                }
            }

            if (do1 && do2)                             // Compliant
            {
                throw new InvalidOperationException();
            }
        }

        private static void Repro1187_2(List<int> elementGroup)
        {
            bool startDoingSomething = false;
            int a = 0;

            foreach (var element in elementGroup)
            {
                if (!startDoingSomething) // Compliant
                {
                    if (element > 3)
                    {
                        startDoingSomething = true;
                    }
                }
                else
                {
                    a++;
                }
            }
        }

        private static bool Repro1160(string[] files)
        {
            bool anyPathRooted = false;
            bool allPathsRooted = true;
            foreach (var file in files)
            {
                if (Path.IsPathRooted(file))
                {
                    anyPathRooted = true;
                }
                else
                {
                    allPathsRooted = false;
                }
            }
            if (anyPathRooted && !allPathsRooted)   // Compliant
            {
                throw new InvalidOperationException("Paths must be all rooted or all unrooted");
            }
            return allPathsRooted;
        }

        private static bool ForEachLoop(int[] items)
        {
            bool bool1 = false;
            bool bool2 = false;

            foreach (var item in items)
            {
                if (item > 0)
                {
                    bool1 = true;
                }
                else if (item < 0)
                {
                    bool2 = true;
                }
            }

            if (bool1 && bool2)                         // Compliant
            {
                throw new InvalidOperationException();
            }

            return bool1 && !bool2;                     // Compliant
        }

        private static bool ForEachLoop2(int[] items)
        {
            bool bool1 = false;
            bool bool2 = false;

        BeforeLoop:

            foreach (var item1 in items)
            {
                foreach (var item2 in items)
                {
                    foreach (var item3 in items)
                    {
                        foreach (var item4 in items)
                        {
                            if (item1 > 0)
                            {
                                bool1 = true;
                            }
                            else if (item2 < 0)
                            {
                                bool2 = true;
                            }
                        }
                    }
                }
            }

            if (bool1 && bool2) // Compliant
            {
                goto BeforeLoop;
            }
            return false;
        }

        private static bool LoopIterationLimitation(int[] items)
        {
            bool bool1 = false;
            bool bool2 = false;
            bool bool3 = false;

            foreach (var item1 in items)
            {
                if (bool2)          // Noncompliant - FP because symbolic execution stops at the third iteration of loop
                {
                    bool3 = true;   // Secondary FP
                }
                if (bool1)
                {
                    bool2 = true;
                }
                if (item1 > 1) {
                    bool1 = true;
                }

            }
            return bool3;
        }
    }

    public class GeneratorFunctions
    {
        private bool generate;

        public void Stop()
        {
            generate = false;
        }

        public IEnumerable<int> Repro_1295()
        {
            generate = true;
            while (generate) // Noncompliant FP: 'generate' field can potentially be changed inside the loop where this generator is used
            {
                yield return 0;
            }
        }

        public IEnumerable<int> FalseNegative()
        {
            var myVariable = true;
            while (myVariable) // Noncompliant: myVariable will never change after initialization
            {
                yield return 0;
            }
        }
    }

    public class StringComparision
    {
        public void Method(string parameterString)
        {
            string emptyString1 = "";
            string emptyString2 = "";
            string nullString1 = null;
            string nullString2 = null;
            string fullStringa1 = "a";
            string fullStringa2 = "a";
            string fullStringb = "b";
            string whiteSpaceString1 = " ";
            string whiteSpaceString2 = " ";
            string doubleWhiteSpaceString1 = "  ";
            string doubleWhiteSpaceString2 = "   ";

            if (emptyString1 == emptyString2)                       // FN
            {

            }
            if (nullString1 == nullString2)                         // Noncompliant
            {

            }

            if (fullStringa1 == fullStringa2)
            {

            }

            if (fullStringa1 == fullStringb)
            {

            }

            if (parameterString == emptyString1)
            {

            }

            if (parameterString == nullString1)
            {

            }

            if (emptyString1 == nullString1)                        // Noncompliant
            {

            }

            if (emptyString1 == fullStringa1)                       // FN
            {

            }

            if (nullString1 == fullStringa1)                        // Noncompliant
            {

            }

            if (fullStringa1 == "")                                 // FN
            {

            }

            if (fullStringa1 == null)                               // Noncompliant
            {

            }

            if (whiteSpaceString1 == whiteSpaceString2)             // FN
            {

            }

            if (whiteSpaceString1 == " ")                           // FN
            {

            }

            if (whiteSpaceString1 == emptyString1)                  // FN
            {

            }

            if (whiteSpaceString1 == nullString1)                   // Noncompliant
            {

            }

            if (whiteSpaceString1 == fullStringa1)                  // FN
            {

            }

            if (whiteSpaceString1 == parameterString)
            {

            }

            if (doubleWhiteSpaceString1 == doubleWhiteSpaceString2) // FN
            {

            }

            if (doubleWhiteSpaceString1 == emptyString1)            // FN
            {

            }

            if (doubleWhiteSpaceString1 == nullString1)             // Noncompliant
            {

            }

            if (doubleWhiteSpaceString1 == fullStringa1)            // FN
            {

            }

            if (doubleWhiteSpaceString1 == parameterString)
            {

            }

        }
    }

    public class NullOrEmpty
    {
        public void Method1(string s)
        {
            string s1 = null;
            string s2 = "";
            string s3 = "a";
            if (string.IsNullOrEmpty(s1)) // Noncompliant
            {
            }
            if (string.IsNullOrEmpty(s2)) // FN
            {
            }

            if (string.IsNullOrEmpty(s))
            {
            }

            if (string.IsNullOrEmpty(s3)) // FN
            {
            }
        }

        public void Method2(string s)
        {

            if (string.IsNullOrEmpty(s))
            {
            }

            s = "";
            if (string.IsNullOrEmpty(s)) // FN
            {
            }

            s = null;
            if (string.IsNullOrEmpty(s)) // Noncompliant
            {
            }

            s = "a";
            if (string.IsNullOrEmpty(s)) // FN
            {
            }
        }

        public void Method3(string s1)
        {
            if (string.IsNullOrEmpty(s1))
            {
                s1.ToString();
            }
            else
            {
                if (s1 == null)     // Noncompliant
                {
                    s1.ToString();  // Secondary
                }
            }

        }

        public void Method4(string s1)
        {
            if (!string.IsNullOrEmpty(s1))
            {
                if (s1 == null)     // Noncompliant
                {
                    s1.ToString();  // Secondary
                }

            }
            else
            {
                s1.ToString();
            }

        }

        public void Method5(string s1)
        {
            if (!(string.IsNullOrEmpty(s1)))
            {
                if (s1 == null)     // Noncompliant
                {
                    s1.ToString();  // Secondary
                }

            }
            else
            {
                s1.ToString();
            }

        }

        public void Method6(string s1)
        {
            if (string.IsNullOrEmpty(s1) || s1 == "a")
            {
                s1.ToString();
            }
            else
            {
                if (s1 == null)     // Noncompliant
                {
                    s1.ToString();  // Secondary
                }
            }

        }

        public void Method7(string s1)
        {
            if (string.IsNullOrEmpty(s1) && s1 == "a") // FN
            {
                s1.ToString();
            }
            else
            {
                if (s1 == null)
                {
                    s1.ToString();
                }
            }

        }


        public string Method8(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException($"{nameof(message)} shouldn't be null!");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException($"{nameof(message)} shouldn't be empty!");
            }

            return String.Empty;
        }

        void NullCoalesce_Useless(string a, string b, string c, string d)
        {
            string isNull = null;
            string notNull = "";
            string notEmpty = "value";
            string ret;

            ret = b ?? a;
            ret = b ?? notNull;
            ret = c ?? notEmpty;
            ret = d ?? "N/A";

            //Left operand: Values notNull and notEmpty are known to be not-null
            ret = notNull ?? a;                         // Noncompliant
                                                        // Secondary@-1
            ret = ((notNull)) ?? a;                     // Noncompliant
                                                        // Secondary@-1
            ret = "Lorem " + (notNull ?? a) + " ipsum"; // Noncompliant
                                                        // Secondary@-1
            ret = notNull ?? "N/A";                     // Noncompliant
                                                        // Secondary@-1
            ret = notEmpty ?? "N/A";                    // Noncompliant
                                                        // Secondary@-1

            //Left operand: isNull is known to be null
            ret = null ?? a;                            // Noncompliant
            ret = isNull ?? a;                          // Noncompliant
            ret = ((isNull)) ?? a;                      // Noncompliant
            ret = "Lorem " + (isNull ?? a) + " ipsum";  // Noncompliant

            //Right operand: isNull is known to be null, therefore ?? is useless
            ret = a ?? null;                            // FN: NOOP
            ret = a ?? isNull;                          // FN: NOOP
            //         ~~~~~~

            //Combo/Fatality
            ret = notNull ?? isNull;
            //    ^^^^^^^                                  Noncompliant {{Change this expression which always evaluates to the same result. Some code paths are unreachable.}}
            //               ^^^^^^                        Secondary@-1
            ret = isNull ?? null;                       // Noncompliant {{Change this expression which always evaluates to the same result.}}
            //    ^^^^^^
            ret = "Value" ?? a;
            //    ^^^^^^^                                  Noncompliant {{Change this expression which always evaluates to the same result. Some code paths are unreachable.}}
            //               ^                             Secondary@-1
        }

        int CoalesceCount<T>(IList<T> arg)
        {
            arg = arg ?? new List<T>();
            return arg.Count;
        }

        public class CoalesceProperty
        {
            private object message;

            public object Message
            {
                get { return message = message ?? new object(); }
            }
        }
    }

    public class NullOrWhiteSpace
    {
        public void Method1(string s)
        {
            string s1 = null;
            string s2 = "";
            string s3 = s ?? "";
            string s4 = " ";

            if (string.IsNullOrWhiteSpace(s1))  // Noncompliant
            {
            }


            if (string.IsNullOrWhiteSpace(s2))  // FN
            {
                if (s2 == "a")                  // FN
                {

                }
            }

            if (string.IsNullOrWhiteSpace(s3))
            {

            }

            if (string.IsNullOrWhiteSpace(s4))  // FN
            {

            }

            if (!string.IsNullOrWhiteSpace(s4)) // FN
            {

            }

            if (!string.IsNullOrWhiteSpace(s))
            {
                if (s == "")                    // FN
                {

                }

                if (s == " ")                   // FN
                {

                }
            }

        }
    }

}

// https://github.com/SonarSource/sonar-dotnet/issues/3565
namespace Repro_3565
{
    using Microsoft.Extensions.Primitives;

    public class Repro_3565
    {
        public void DoWork(StringSegment segment)
        {
            if (segment == null)    // Compliant: StringSegment has custom equality operator that can return true if the StringSegment contains a null string
            {
                throw new ArgumentException("May not point to a null string", nameof(segment));
            }
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/3701 for S2589
// https://github.com/SonarSource/sonar-dotnet/issues/3161 for S2583
namespace Repro_LocalFunction
{
    public class Repro
    {
        public void DoWork(bool condition)
        {
            string value = null;
            LocalFunction();
            if (value != null)                          // Noncompliant FP: value is assigned in local function
            {
                throw new InvalidOperationException();  // Secondary FP
            }
            if (value == null)                          // Noncompliant: value is always null
            {
                throw new InvalidOperationException();
            }

            void LocalFunction()
            {
                if (condition)
                {
                    value = "Not null";
                }
            }
        }

        public void DoWork2(bool condition)
        {
            string value = null;
            string alwaysNull = null;
            LocalFunction();

            if (alwaysNull != null)                     // Noncompliant
            {
                throw new InvalidOperationException();  // Secondary
            }
            if (alwaysNull == null)                     // Noncompliant
            {
                throw new InvalidOperationException();
            }

            void LocalFunction()
            {
                if (condition)
                {
                    value = "Not null";
                }
            }
        }

        public void DoWork3(bool condition, string s)
        {
            string value = null;
            LocalFunction();

            if (value != null)                          // Noncompliant FP (set in local function)
            {
                throw new InvalidOperationException();  // Secondary FP
            }
            if (value == null)                          // Noncompliant FP (set in local function)
            {
                throw new InvalidOperationException();
            }

            void LocalFunction()
            {
                if (condition)
                {
                    value = s;
                }
            }
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/3353 for S2583
namespace Repro_RefParam
{
    public class Repro
    {
        private static object gate = new object();

        public void TestExample(ref bool stop)
        {
            while (!stop)
            {
                while (true)    // Compliant
                {
                    if (stop)   // Noncompliant FP: In a multithreaded context it makes sense to check as the value can be changed on another thread.
                    {
                        break;
                    }
                }
            }
        }

        public void InitWithLock(ref object field)
        {
            if (field == null)
            {
                lock (gate)
                {
                    if (field == null) // We don't check conditions in lock statements.
                                       // In multithreading context it makes sense to check for null twice
                    {
                        field = new object();
                    }
                }
            }
        }

        public void Init(ref object field)
        {
            if (field == null)
            {
                if (field == null)   // Noncompliant, we already checked for null
                {
                    field = new object();
                }
            }
        }

        public void Repro739()
        {
            var x = 5.5;
            var y = (int)x;
            if (x == y)            // Compliant
            {
                Console.WriteLine("Test");
            }
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/4257
public class Tuples
{
    public void Go()
    {
        MemoryStream memoryStream = null;
        string str = null;

        (memoryStream, str) = GetData();

        if (memoryStream != null) // Noncompliant FP: memoryStream was reassigned as a tuple
        {
            // some code
        }
    }

    public (MemoryStream, string) GetData() => (null, null);
}

// https://github.com/SonarSource/sonar-dotnet/issues/7057
public class Repro_7057
{
    private (string, int) SomeTuple() => ("hello", 1);
    private string SomeString() => "hello";

    public void WithTuple()
    {
        string current = null;
        string last;
        do
        {
            last = current;
            (current, _) = SomeTuple();
        }
        while (last == null);   // Noncompliant FP
    }

    public void WithString()
    {
        string current = null;
        string last;

        do
        {
            last = current;
            current = SomeString();
        }
        while (last == null);
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/5221
public class ReproWithNullableValueTypes
{
    protected void Test(decimal? value1, decimal? value2)
    {
        if (value1 == null || value2 == null || value1 != value2)   // Compliant
        {
            Console.WriteLine("test");
        }
    }
}

// Inspired by https://github.com/SonarSource/sonar-dotnet/issues/4784
public class Repro_4784
{
    public void ReportOnConditional(object[] arg)
    {
        var list = arg.ToList();    // NotNull
        var ret = list?.Count;      // Noncompliant: list is never null
        if (list?.Count == 0)       // Noncompliant: list is never null
        {
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/7489
public class Repro_7489
{
    public void Method()
    {
        int? value;
        var hasValue = true;
        while (hasValue)    //Compliant: when the tuple deconstruction is in the body
        {
            value = null;
            hasValue = value.HasValue;

            foreach (var (a, b) in new List<(int, int)>())
            { } // It only reproduces when this is present
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/5601
public class Repro_5601
{
    public static bool Run()
    {
        int pos = 0;
        bool foundA = false;
        bool readA = false;
        string test = "ab";

        while (pos < test.Length)
        {
            if (test[pos] == 'a')
            {
                foundA = true;
                readA = true;
            }
            else if (readA)
            {
                readA = false;
            }
            pos++;
        }

        if (readA)
        {
            return false;
        }

        if (foundA)
        {
            return true;
        }
        return false;
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/2411
public class Repro_2411
{
    public void Method(Guid guid)
    {
        if (guid == null || guid == Guid.Empty) // Noncompliant {{Change this condition so that it does not always evaluate to 'False'.}}
        //  ^^^^^^^^^^^^
            guid = Guid.NewGuid();
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/8080
public class Repro_8080
{
    protected const int Limit = 10;

    protected void Repro()
    {
        for (int i = 0; i < Limit; i++)
        {
            var test = i > 2 ? 0 : 42;  // Compliant
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/8264
public class Repro_8264
{
    enum HttpStatusCode { OK }

    void Repro()
    {
        HttpStatusCode? statusCode = null;
        var contentLength = 0;

        while (true)
        {
            var response = "5";
            if (statusCode is null)
            {
                statusCode = HttpStatusCode.OK;
                continue; // first iteration
            }

            if (response == "5" && contentLength == 0)  // Noncompliant {{Change this condition so that it does not always evaluate to 'True'.}} FP (contentLength is changed in the second iteration)
            {
                contentLength = 7;
                continue;
            }

            if (response.Length > 0)
            {
                if (contentLength > 0)                  // Noncompliant {{Change this condition so that it does not always evaluate to 'False'. Some code paths are unreachable.}} FP (consequential error)
                {
                    Console.WriteLine("Unreachable??"); // Secondary
                }
                break;
            }
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/8285
public class Repro_8285
{
    void Repro(int number)
    {
        if (number < 0)
        {
            if (number % 2 == 0) // Compliant
            {
                Console.WriteLine("Something");
            }
            else
            {
                Console.WriteLine("Something else");
            }
        }
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/8262
public class Repro_8262
{
    void Repro(int rounds) // upper bound of the loop must be unknown. If it is known, no FP is reported
    {
        for (var i = 1; i <= rounds; i++)
        {
            switch (i)
            {
                case 1:
                case 4:   // Noncompliant {{Change this condition so that it does not always evaluate to 'False'.}} FP
                case 7:   // Noncompliant FP
                case 10:  // Noncompliant FP
                case 13:  // Noncompliant FP
                case 16:  // Noncompliant FP
                    Console.WriteLine(i);
                    break;
                case 2:   // Noncompliant FP
                case 5:   // Secondary
                case 8:
                case 11:
                case 14:
                    Console.WriteLine(i);
                    break;
                case 3:   // Secondary
                case 6:
                case 9:
                case 12:
                case 15:
                    Console.WriteLine(i);
                    break;
                default:
                    break;
            }
        }
    }
}

// Reproducer for https://github.com/SonarSource/sonar-dotnet/issues/8368
public class Repro_8368
{
    public void Method()
    {
        Exception lastException = null;

        try
        {
            DoSomeWork();
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
        }

        if (lastException != null) // Noncompliant - FP
        {
            LogError(lastException);
        }
    }

    void DoSomeWork() { }
    void LogError(Exception exception) { }
}


// Reproducer for https://github.com/SonarSource/sonar-dotnet/issues/8378
public class Repro_8378
{
    public void Method()
    {
        var success = true;
        try
        {
            var result = 100 / DateTime.Now.Second;
        }
        catch (Exception ex)
        {
            success = false;
        }
        if (success)    // Noncompliant FP
        {
            Console.WriteLine("Success!");
        }
    }
}

// Reproducer for https://github.com/SonarSource/sonar-dotnet/issues/8428
public class Repro_8428
{
    void Test()
    {
        var ids = new int[1000];
        for (var i = 0; i < ids.Length; i++)
        {
            if (i % 100 != 0 || i <= 0)   // Noncompliant FP
            {
                System.Diagnostics.Debug.WriteLine(i);
            }
        }
    }
}

// Reproducer for https://github.com/SonarSource/sonar-dotnet/issues/8445
public class Repro_8445
{
    private readonly int readonlyInt = 42;
    private readonly string readonlyString = "42";
    private static readonly int readonlyStaticInt = 42;
    private static readonly string readonlyStaticString = "42";
    private void Test()
    {
        if (readonlyInt == 42)                                  // FN
            Console.WriteLine();
        if (readonlyString == "42")                             // FN
            Console.WriteLine();
        if (readonlyStaticInt == 42)                            // FN
            Console.WriteLine();
        if (readonlyStaticString == "42")                       // FN
            Console.WriteLine();
        var other = new Repro_8445_OtherClass();
        if (other.readonlyInt == 42)                            // FN
            Console.WriteLine();
        if (other.readonlyString == "42")                       // FN
            Console.WriteLine();
        if (Repro_8445_OtherClass.readonlyStaticInt == 42)      // FN
            Console.WriteLine();
        if (Repro_8445_OtherClass.readonlyStaticString == "42") // FN
            Console.WriteLine();
    }
}
public class Repro_8445_OtherClass
{
    public static readonly int readonlyStaticInt = 42;
    public static readonly string readonlyStaticString = "42";
    public readonly int readonlyInt = 42;
    public readonly string readonlyString = "42";
}
