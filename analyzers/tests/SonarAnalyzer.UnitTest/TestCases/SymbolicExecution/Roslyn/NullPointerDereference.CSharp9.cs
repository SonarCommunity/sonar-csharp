﻿using System;
using System.Text;

object topLevel = "Value";
topLevel.ToString();

topLevel = null;
topLevel.ToString();   // FN

void TopLevelLocalFunction()
{
    object local = "Value";
    local.ToString();

    local = null;
    local.ToString();   // FN
}

public class Sample
{
    private string field;

    public void TargetTypedNew()
    {
        StringBuilder sb;

        sb = new();
        sb.Append("Value");

        sb = new(42);
        sb.Append("Value");

        sb = null;
        sb.ToString(); // Noncompliant
    }

    public void PatternMatching(object arg)
    {
        object o = null;
        if (arg is string)
        {
            arg.ToString();     // Compliant
        }

        if (arg is int and > 0 and > 1)
        {
            arg.ToString();     // Compliant
        }

        if (arg is int or bool or long)
        {
            arg.ToString();     // Compliant
        }

        if (arg is null)
        {
            arg.ToString();     // Noncompliant
        }
        if (arg is int or bool or null)
        {
            arg.ToString();     // FN
        }
        else if (arg is not not null)
        {
            arg.ToString();     // Non-compliant FIXME
        }
        else if (!(arg is not null))
        {
            arg.ToString();     // Non-compliant FIXME
        }
        else
        {
            if (o is not null)
            {
                o.ToString();   // Compliant
            }
            o.ToString();       // Noncompliant
        }

        if (arg is false)
        {
            if ((bool)arg)
            {
                o.ToString();   // Compliant, unreachable
            }
            else
            {
                o.ToString();   // Noncompliant
            }
        }

        if (arg is true)
        {
            if (!(bool)arg)
            {
                o.ToString();   // Compliant, unreachable
            }
            else
            {
                o.ToString();   // Noncompliant
            }
        }
    }

    public void PatternMatchingSwitch(object arg)
    {
        var res = arg switch
        {
            not null => arg,
            _ => ""
        };
        res.ToString();

        res = arg switch
        {
            string and not null => arg,
            _ => ""
        };
        res.ToString();     // Compliant

        res = arg switch
        {
            string x => x,
            not not null => arg,
            _ => ""
        };
        res.ToString();     // Noncompliant

        object o = null;
        o.ToString();       // Noncompliant
    }

    public void StaticLambda()
    {
        Func<string> a = static () =>
        {
            object o = null;
            return o.ToString();    // Noncompliant
        };
        a();
    }

    public int PropertySimple
    {
        get => 42;
        init
        {
            object o = null;
            field = o.ToString();   // Noncompliant
        }
    }

    public object PropertyWithValue
    {
        get => null;
        init
        {
            if (value == null)
            {
                field = value.ToString();   // Noncompliant
            }
        }
    }
}

public record Record
{
    public void Method()
    {
        object o = null;
        o.ToString();   // Noncompliant
    }
}

public partial class Partial
{
    public partial void Method();
}

public partial class Partial
{
    public partial void Method()
    {
        object o = null;
        o.ToString();   // Noncompliant
    }
}

namespace TartetTypedConditional
{
    public interface IAlpha { }
    public interface IBeta { }
    public class AlphaAndBeta : IAlpha, IBeta { }
    public class BetaAndAlpha : IAlpha, IBeta { }

    public class Sample
    {
        public void Go(bool condition)
        {
            AlphaAndBeta ab = new AlphaAndBeta();
            BetaAndAlpha ba = null;
            IAlpha result = condition ? ab : ba;
            result.ToString();  // Noncompliant
        }
    }
}
