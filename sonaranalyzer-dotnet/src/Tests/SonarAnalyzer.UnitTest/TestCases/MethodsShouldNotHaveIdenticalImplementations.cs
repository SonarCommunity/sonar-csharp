using System;

namespace Tests.Diagnostics
{
    class Program
    {
        void Foo1()
//           ^^^^ Secondary
//           ^^^^ Secondary@-1
//           ^^^^ Secondary@-2
        {
            string s = "test";
            Console.WriteLine("Result: {0}", s);
        }

        void Foo2() // Noncompliant {{Update this method so that its implementation is not identical to 'Foo1'.}}
//           ^^^^
        {
            string s = "test";
            Console.WriteLine("Result: {0}", s);
        }

        void Foo3() // Noncompliant {{Update this method so that its implementation is not identical to 'Foo1'.}}
        {
            string s = "test";
            Console.WriteLine("Result: {0}", s);
        }

        void Foo4() // Noncompliant {{Update this method so that its implementation is not identical to 'Foo1'.}}
        {
            string s = "test"; // Comment are excluded from comparison
            Console.WriteLine("Result: {0}", s);
        }

        void Foo5()
        {
            string s = "test";
            Console.WriteLine("Result: {0}", s);
            Console.WriteLine("different");
        }



        void Bar1()
        {
            throw new NotImplementedException();
        }

        void Bar2()
        {
            throw new NotImplementedException();
        }



        void FooBar1()
        {
            throw new NotSupportedException();
        }

        void FooBar2()
        {
            throw new NotSupportedException();
        }



        int Baz1(int a) => a;

        int Baz2(int a) => a; // Compliant we ignore expression body



        string Qux1(int val)
        {
            return val.ToString();
        }

        string Qux2(int val)
        {
            return val.ToString(); // Compliant because we ignore one liner
        }

        string Qux3(int val) => val.ToString(); // Compliant we ignore expression body
    }
}