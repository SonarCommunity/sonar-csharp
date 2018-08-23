﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    class Foo
    {
        private string field;

        string Method(string s) =>
            s != null
            ? null
            : s.ToLower(); // Noncompliant

        string Prop =>
            field != null
            ? null
            : field.ToLower(); // Noncompliant

        string PropGet
        {
            get =>
                field != null
                ? null
                : field.ToLower(); // Noncompliant
        }

        void ConstantPattern(object o)
        {
            if (o is null)
            {
                o.ToString(); // Noncompliant
            }
            else
            {
                o.ToString(); // Compliant
            }
        }

        void VariableDesignationPattern_Variable(object o)
        {
            if (o is string s)
            {
                if (s == null)
                {
                    // This is unreachable, s has NotNull constraint from the outer if condition
                    s.ToString(); // Compliant
                }

                // s still has NotNull constraint from the outer if statement
                s.ToString(); // Compliant
            }
        }

        void VariableDesignationPattern_Source(object o)
        {
            // We can set NotNull constraint only for one of the variables in the if condition
            // and we choose the declared variable because it is more likely to have usages of
            // it inside the statement body.
            if (o is string s)
            {
                if (o == null)
                {
                    o.ToString(); // Noncompliant, False Positive
                }
            }
        }

        void Patterns_In_Loops(object o, object[] items)
        {
            while (o is string s)
            {
                if (s == null)
                {
                    // This is unreachable, s has NotNull constraint from the while condition
                    s.ToString(); // Compliant
                }
            }

            do
            {
                // The condition is evaluated after the first execution, so we cannot test s
            }
            while (o is string s);

            for (int i = 0; i < length && items[i] is string s; i++)
            {
                if (s == null)
                {
                    // This is unreachable, s has NotNull constraint from the for condition
                    s.ToString();
                }
            }
        }

        void Switch_Pattern_Source(object o)
        {
            switch (o)
            {
                case string s:
                    // We don't set constraints on the switch expression
                    if (o == null)
                    {
                        o.ToString(); // Noncompliant, False Positive
                    }
                    break;

                default:
                    break;
            }
        }

        void Switch_Pattern(object o)
        {
            switch (o)
            {
                case string s:
                    if (s == null)
                    {
                        // This is unreachable, s has NotNull constraint from the outer if condition
                        s.ToString(); // Compliant
                    }
                    // s still has NotNull constraint from the outer if statement
                    s.ToString(); // Compliant
                    break;

                case Foo f when f == null:
                    if (f == null)
                    {
                        f.ToString(); // Compliant, this code is not reachable
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
