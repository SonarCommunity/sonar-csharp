﻿using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class Sample
{
    private string field;

    public void TargetTypedNew(object arg)
    {
        arg.ToString();         // Noncompliant

        StringBuilder sb = new();
    }

    public void PatternMatching(object arg)
    {
        if (arg is string)
        {
            arg.ToString();     // Compliant
        }

        if (arg is int and > 0 and > 1)
        {
            arg.ToString();     // Noncompliant - FP
        }

        if (arg is int or bool or long)
        {
            arg.ToString();     // Noncompliant - FP
        }

        if (arg is null)
        {
            arg.ToString();     // Covered by S2259
        }
        if (arg is int or bool or null)
        {
            arg.ToString();
        }
        else if (arg is not not null)
        {
            arg.ToString();
        }
        else if (!(arg is not null))
        {
            arg.ToString();
        }
    }

    public void StaticLambda()
    {
        Func<object, string> a = static (arg) =>
        {
            return arg.ToString();      // Compliant, it's not a public method argument
        };
        a(null);
    }

    public object PropertySet
    {
        get => null;
        set
        {
            field = value.ToString();   // Noncompliant
        }
    }

    public object PropertyInit
    {
        get => null;
        init
        {
            field = value.ToString();   // Noncompliant
        }
    }
}

public record Record
{
    public void Method(object arg)
    {
        arg.ToString();                 // Noncompliant
    }
}

public partial class Partial
{
    public partial void Method(object arg);
}

public partial class Partial
{
    public partial void Method(object arg)
    {
        arg.ToString();                 // Noncompliant
    }
}

public class UsingFromServicesAttribute
{
    public int GetProduct([FromServices] IService service) =>
         service.GetValue();            // Compliant, it's attributed with FromServices attribute

    public int GetProductMultipleAttr([FromServices][FromRoute] IService service) =>
        service.GetValue();             // Compliant, it's attributed with FromServices attribute

    public int GetPrice(IService service) =>
        service.GetValue();             // Noncompliant

    public interface IService
    {
        int GetValue();
    }
}
