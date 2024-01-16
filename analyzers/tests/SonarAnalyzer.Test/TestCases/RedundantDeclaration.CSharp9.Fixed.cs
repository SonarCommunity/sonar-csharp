﻿using System;

Record r1 = new Record(); // Compliant - FN
Record r2 = null;
r2 = new Record(); // Compliant - FN

LocalStatic(new Record()); // Compliant - FN
Action<int, int> a = static (_, _) => { };

Action<int, int, int> b = static (_, p2, _) => { Console.WriteLine(p2); };

Action<int> c = static (parameter1) => { Console.WriteLine(parameter1); }; // FN - Can be replaced with method group `Action<int> b = Console.WriteLine;`

Action<int> d = static parameter1 => { Console.WriteLine(parameter1); }; // FN - Can be replaced with method group `Action<int> c = Console.WriteLine;`

Record Local() => new Record(); // Compliant - FN
static Record LocalStatic(Record r) => r;

int? xx = ((new int?(5))); // Fixed

EventHandler myEvent = new ((_, _) => { });

record Record
{
    object field;

    object Property
    {
        get { return new object(); } // Compliant - FN
        init { field = new object(); } // Compliant - FN
    }
}

public class RedundantDeclaration
{
    public RedundantDeclaration()
    {
        MyEvent += new((_, _) => { });

        object o = new() { }; // Fixed

        _ = new Point[] // Fixed
            {
                new(1, 2)
            };
    }

    private event EventHandler MyEvent;

    public record Point(int x, int y);
}
