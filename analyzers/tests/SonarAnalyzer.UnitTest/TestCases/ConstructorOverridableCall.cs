﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    public abstract class ParentAbstract
    {
        protected ParentAbstract(ParentAbstract other)
        {
            DoSomething();  // Noncompliant {{Remove this call from a constructor to the overridable 'DoSomething' method.}}
//          ^^^^^^^^^^^
            this.DoSomething();  // Noncompliant
            other.DoSomething(); // Compliant

            var a = this;
            a.DoSomething(); // Not recognized

            var action = new Action(() => { DoSomething(); });
        }

        public abstract void DoSomething();
    }

    public class Parent
    {
        public Parent()
        {
            DoSomething();  // Noncompliant
            DoSomething2();

            var action = new Action(() => { DoSomething(); });
        }

        public virtual void DoSomething() // can be overridden
        {

        }
        public void DoSomething2()
        {

        }
    }

    public class Child : Parent
    {
        private string foo;

        public Child(string foo) // leads to call DoSomething() in Parent constructor which triggers a NullReferenceException as foo has not yet been initialized
        {
            this.foo = foo;
        }

        public override void DoSomething()
        {
            Console.WriteLine(this.foo.Length);
        }
    }

    public class Class1
    {
        public virtual void DoSomething() { }
    }

    public class Class2 : Class1
    {
        public Class2()
        {
            DoSomething(); // FN
        }
    }

    public class Class3 : Class2
    {
        public string Name { get; set; }

        public Class3(string name)
        {
            Name = name;
        }

        public override void DoSomething()
        {
            Console.WriteLine($"{Name} is null at this point");
        }
    }
}
