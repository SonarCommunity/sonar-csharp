﻿using System;

namespace Tests.Diagnostics
{
    class UseValueParameter
    {
        private int count;
        public int Count
        {
            get { return count; }
            set { count = 3; } // Noncompliant {{Use the 'value' parameter in this property set accessor declaration.}}
//          ^^^
        }
        public int Count2
        {
            get { return count; }
            set { count = value; }
        }
        public int Count3
        {
            //get { return count; }
            set // Noncompliant
            {
                var value = 5;
                count = value;
            }
        }
        public int Count5
        {
            set
            {
                throw new Exception();
            }
        }

        public int Count4 => count;

        public int this[int i]
        {
            get
            {
                return 0;
            }
            set // Noncompliant
            {
                var x = 1;
            }
        }

        event EventHandler PreDrawEvent;

        event EventHandler IDrawingObject.OnDraw
        {
            add // Noncompliant {{Use the 'value' parameter in this event accessor declaration.}}
            {
                lock (PreDrawEvent)
                {
                }
            }
            remove
            {
                lock (PreDrawEvent)
                {
                    PreDrawEvent -= value;
                }
            }
        }
    }

    interface IFoo
    {
        int Foo { get; set; }

        event EventHandler Bar;
    }

    public class Foo : IFoo
    {
        public virtual int Foo
        {
            get { return 42; }
            set { } // Compliant because interface implementation
        }

        public virtual float Bar
        {
            get { return 42; }
            set { } // Noncompliant
        }

        public virtual event EventHandler Bar
        {
            add { } // Compliant because interface implementation
            remove { } // Compliant because interface implementation
        }
    }

    public class Bar : Foo
    {
        public override int Foo
        {
            get { return 42; }
            set { } // Noncompliant
        }

        public override float Bar
        {
            get { return 42; }
            set { } // Noncompliant
        }

        public override event EventHandler Bar
        {
            add { } // Noncompliant
            remove { } // Noncompliant
        }
    }

    public class NewSyntax
    {
        public int ReadonlyGetter { get; }

        public int ArrowedProperty => 1;

        private int field;
        public int ArrowedAccessors
        {
            get => field;
            set => field = value;
        }

        private string id;

        public string Id
        {
            get => null;
            set => id = 10; // Noncompliant
        }

        public int A
        {
            set => throw new Exception();
        }

        public event EventHandler E
        {
            add => throw new Exception();
            remove => throw new Exception();
        }
    }
}
