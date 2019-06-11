﻿using System;

namespace Tests.Diagnostics
{
    public delegate void EventHandler1(object s);
    public delegate int EventHandler2(object s, EventArgs e);
    public delegate void EventHandler3(int sender, EventArgs e);
    public delegate void EventHandler4(object sender, int e);
    public delegate void EventHandler5(object sender, EventArgs args);

    public delegate void potentiallyCorrectEventHandler<T>(object sender, T e);

    public delegate void CorrectEventHandler1(object sender, EventArgs e);
    public delegate void CorrectEventHandler2<T>(object sender, T e) where T : EventArgs;

    public class Foo<TEventArgs> where TEventArgs : EventArgs
    {
        public event EventHandler1 Event1; // Noncompliant {{Change the signature of that event handler to match the specified signature.}}
//                   ^^^^^^^^^^^^^
        public event EventHandler2 Event2; // Noncompliant
        public event EventHandler3 Event3; // Noncompliant
        public event EventHandler4 Event4; // Noncompliant
        public event EventHandler5 Event5; // Noncompliant
        public event potentiallyCorrectEventHandler<Object> Event6; // Noncompliant

        public event EventHandler1 Event1AsProperty // Noncompliant {{Change the signature of that event handler to match the specified signature.}}
//                   ^^^^^^^^^^^^^
        {
            add { }
            remove { }
        }

        public event CorrectEventHandler1 CorrectEvent;
        public event CorrectEventHandler2<EventArgs> CorrectEvent2;
        public event CorrectEventHandler2<TEventArgs> CorrectEvent3;
        public event potentiallyCorrectEventHandler<EventArgs> CorrectEvent4;
        public event potentiallyCorrectEventHandler<TEventArgs> CorrectEvent5;
    }

    public class Bar<TEventArgs1, TEventArgs2, TParamWithoutConstraint>
        where TEventArgs1 : EventArgs
        where TEventArgs2 : TEventArgs1
    {
        public event EventHandler<TEventArgs1> CorrectEvent1;
        public event EventHandler<TEventArgs2> CorrectEvent2;

        public event EventHandler<TParamWithoutConstraint> IncorrectEvent; // Noncompliant
    }
}
