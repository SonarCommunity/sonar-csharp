﻿namespace Tests.TestCases
{
    class ConditionalsWithSameCondition
    {
        public void Foo()
        {
            string c = null;
            if (c is null)
            {
                doTheThing(c);
            }
            if (c is null) // Noncompliant {{This condition was just checked on line 8.}}
            {
                doTheThing(c);
            }
            if (c is not null) // Compliant
            {
                doTheThing(c);
            }

            if (c is "banana") // Compliant, c might be updated in the previous if
            {
                c += "a";
            }
            if (c is "banana") // Compliant, c might be updated in the previous if
            {
                c = "";
            }
            if (c is "banana") // Compliant, c might be updated in the previous if
            {
                c = "";
            }

            int i = 0;
            if (i is > 0 and < 100)
            {
                doTheThing(i);
            }
            if (i is > 0 and < 100) // Noncompliant {{This condition was just checked on line 35.}}
            {
                doTheThing(i);
            }

            Fruit f = null;
            bool cond = false;
            if (f is Apple)
            {
                f = cond ? new Apple() : new Orange();
            }
            if (f is Apple) // Compliant as f may change
            {
                f = cond ? new Apple() : new Orange();
            }

            if (f is not null) { }
            if (f != null) { } // FN, same as above

            if (f is null) { }
            if (f == null) { } // FN, same as above
        }

        void doTheThing(object o) { }
    }

    abstract class Fruit { }
    class Apple : Fruit { }
    class Orange : Fruit { }
}
