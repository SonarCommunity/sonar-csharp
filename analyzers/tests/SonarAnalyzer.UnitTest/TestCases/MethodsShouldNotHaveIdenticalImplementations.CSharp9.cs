﻿using System;

void Method1()// Secondary [0]
              // Secondary@-1 [1]
{
    string s = "test";
    Console.WriteLine("Result: {0}", s);
}

void Method2() // Noncompliant [0] {{Update this method so that its implementation is not identical to 'Method1'.}}
{
    string s = "test";
    Console.WriteLine("Result: {0}", s);
}

void Method3() // Noncompliant [1] {{Update this method so that its implementation is not identical to 'Method1'.}}
{
    string s = "test";
    Console.WriteLine("Result: {0}", s);
}

void Method4()
{
    Console.WriteLine("Result: 0");
}

public record Sample
{
    public void Method1() // Secondary [2]
                          // Secondary@-1 [3]
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method2() // Noncompliant [2] {{Update this method so that its implementation is not identical to 'Method1'.}}
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method3() // Noncompliant [3] {{Update this method so that its implementation is not identical to 'Method1'.}}
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method4()
    {
        Console.WriteLine("Result: 0");
    }

    public string Method5()
    {
        return "foo";
    }

    public string Method6() =>
        "foo";
}

public record SamplePositional(string Value)
{
    public void Method1() // Secondary [4]
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method2() // Noncompliant [4] {{Update this method so that its implementation is not identical to 'Method1'.}}
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }
}

interface SomeInterface
{
    void Foo1() // Secondary
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    void Foo2() // Noncompliant
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }
}

public static class TypeConstraints
{
    public static int Use<T>(T? value) where T : struct => 1;

    public static int Use<T>(T? value) where T : class => 2;

    public static void First<T>(T? value) where T : struct // Secondary
    {
        var x = Use(value);
        Console.WriteLine(x);
    }

    public static void Second<T>(T? value) where T : class  // Noncompliant - FP, method looks the same but different overloads are called due to the type constraints used. See: https://github.com/SonarSource/sonar-dotnet/issues/7068
    {
        var x = Use(value);
        Console.WriteLine(x);
    }
}
