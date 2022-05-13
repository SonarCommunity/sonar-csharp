﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Azure.WebJobs
{
    public class FunctionNameAttribute : Attribute { public FunctionNameAttribute(string name) { } }  // FIXME: Fake, remove before merging
}

public class AnotherAttribute : Attribute
{
    public AnotherAttribute(string name) { }
}

public static class StaticClass
{
    public static int Property { get; set; }
    public static int Field;

    public static void Update(int value) =>
        Property = value;
}

public class InstanceClass
{
    public int PropertyInstance { get; set; }
    public int FieldInstance;

    public static int PropertyStatic { get; set; }
    public static int FieldStatic;

    public static void UpdateStatic(int value) =>
        PropertyStatic = value;

    public void UpdateInstance(int value) =>
        PropertyInstance = value;
}

namespace Inside.Namespace
{
    public static class Someting
    {
        public static int Field;
    }
}

public static class AzureFunctionsStatic
{
    public static int Property { get; set; }
    public static int Field;
    public static int[] Array;

    [Another("Something")]
    public static void WithAnotherAttribute()   // Compliant
    {
        Property = 42;
    }

    public static void NoAttribute()
    {
        StaticClass.Property = 42;              // Compliant
    }

    [FunctionName("Sample")]
    public static void Write()
    {
        var local = 0;

        Property = 42;          // Noncompliant {{Do not modify a static state from Azure Function.}}
        Field = 42;             // Noncompliant {{Do not modify a static state from Azure Function.}}
        Array[0] = 42;          // FIXME FN Non-compliant {{Do not modify a static state from Azure Function.}}

        Property = local;       // Noncompliant
        Field = local;          // Noncompliant

        Property = Calculate(); // Noncompliant
        Field = Calculate();    // Noncompliant

        StaticClass.Update(42);             // Not tracked, we don't analyze cross-procedure
        StaticClass.Field = 42;             // Noncompliant {{Do not modify a static state from Azure Function.}}
        StaticClass.Property = 42;          // Noncompliant {{Do not modify a static state from Azure Function.}}
//      ^^^^^^^^^^^^^^^^^^^^
        AzureFunctionsStatic.Array[0] = 42; // FIXME FN Non-compliant {{Do not modify a static state from Azure Function.}}
//      ***************************** FIXME

        InstanceClass.UpdateStatic(42);     // Not tracked, we don't analyze cross-procedure
        InstanceClass.PropertyStatic = 42;  // Noncompliant
        InstanceClass.FieldStatic = 42;     // Noncompliant

        Inside.Namespace.Someting.Field = 42;           // Noncompliant
        global::Inside.Namespace.Someting.Field = 42;   // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        var o = new InstanceClass();
        o.UpdateInstance(42);
        o.PropertyInstance = 42;    // Compliant, not static
        o.FieldInstance = 42;
    }

    [FunctionName("Sample")]
    public static void WriteArrow() =>
        Property = 42;      // Noncompliant

    [FunctionName("Sample")]
    public static async Task<string> AsyncTask()
    {
        Property = 42;      // Noncompliant
        return null;
    }

    [FunctionName("Sample")]
    public static void ReadLocal()
    {
        var a = Property;
        var b = Field;
        var c = Array[0];
        if (Property == 0) { }
        if (Field == 0) { }
        if (Array[0] == 0) { }
        WithArg(Property);
        WithArg(Field);
        WithArg(Array[0]);
    }

    [FunctionName("Sample")]
    public static void SideEffects()
    {
        var a = Field = 42;         // Noncompliant
        if ((Field = 42) == 0) { }  // Noncompliant
        if (Field++ == 0) { }       // FIXME FN Non-compliant
        if ((Field += 1) == 0) { }  // Noncompliant
        WithArg(Field++);           // FIXME FN Non-compliant
        WithArg(Field += 1);        // Noncompliant
    }

    [FunctionName("Sample")]
    public static void RefOut()
    {
        var local = 0;
        WithRef(ref local);         // FIXME FN Non-compliant {{Do not modify a static state from Azure Function.}}
        WithOut(out local);         // FIXME FN Non-compliant
        WithOut(value: out local);   // FIXME FN Non-compliant
        WithOut(outOfOrder: local, value: out local);   // FIXME FN Non-compliant
    }

    [FunctionName("Sample")]
    public static void Nested()
    {
        // We don't care if it's used or not. It probably is when it exists.
        Action parenthesized = () => { Property = 42; };    // Noncompliant
        Action<int> b = simple => { Property = 42; };       // Noncompliant

        void LocalFunction()
        {
            Property = 42;      // Noncompliant
        }
    }

    private static int Calculate() =>
        0;

    private static void WithArg(int value) { }

    private static void WithRef(ref int value) =>
        value = 0;

    private static void WithOut(out int value) =>
        value = 0;

    private static void WithOut(out int value, int outOfOrder) =>
        value = 0;

}

public class AzureFunctionsInstance
{
    [FunctionName("Sample")]
    public static void Write()
    {
        StaticClass.Property = 42;  // Noncompliant
    }
}

public static class Operators
{
    private static int Field;
    private static object FieldObj;

    [FunctionName("Sample")]
    public static void Unary()
    {
        Field++;    // FIXME FN Non-compliant
        Field--;    // FIXME FN Non-compliant
        ++Field;    // FIXME FN Non-compliant
        --Field;    // FIXME FN Non-compliant

        for (; Field < 100; Field++) { }    // FIXME FN Non-compliant
    }

    [FunctionName("Sample")]
    public static void Assignment(object arg)
    {
        Field += 42;        // Noncompliant
        Field -= 42;        // Noncompliant
        Field *= 42;        // Noncompliant
        Field /= 42;        // Noncompliant
        Field %= 42;        // Noncompliant
        Field &= 42;        // Noncompliant
        Field |= 42;        // Noncompliant
        Field ^= 42;        // Noncompliant
        Field >>= 42;       // Noncompliant
        Field <<= 42;       // Noncompliant
        FieldObj ??= arg;   // Noncompliant

        var a = FieldObj ?? arg;    // Compliant
    }
}

public static class Collections
{
    private static IList<int> List = new List<int>();
    private static ISet<int> HSet = new HashSet<int>();
    private static IDictionary<int, int> Dict = new Dictionary<int, int>();
    private static int[] Array = { 0, 1, 2 };

    [FunctionName("Sample")]
    public static void Add()
    {
        List.Add(42);       // FN
        HSet.Add(42);        // FN
        Dict.Add(42, 42);   // FN
        Dict[0] = 42;       // FN
    }

    [FunctionName("Sample")]
    public static void Remove()
    {
        List.Remove(42);    // FN
        List.RemoveAt(0);   // FN
        HSet.Remove(42);     // FN
        Dict.Remove(42);    // FN
    }

    [FunctionName("Sample")]
    public static void Update()
    {
        List[0] = 42;       // FN
        Dict[0] = 42;       // FN
        Array[0] = 42;      // FN
    }
}
