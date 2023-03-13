﻿public class NullCoalescenceAssignment
{
    public void NullCoalescenceAssignment_NotNull(string s1)
    {
        s1 ??= "N/A";
        s1.ToString(); // Compliant
    }

    public void NullCoalescenceAssignment_Null(string s1)
    {
        s1 ??= null;
        s1.ToString(); // FN
    }
}

public interface IWithDefaultMembers
{
    decimal Count { get; set; }
    decimal Price { get; set; }

    void Reset(string s)
    {
        s.ToString(); // FIXME Non-compliant
    }
}

public class LocalStaticFunctions
{
    public void Method(object arg)
    {
        string LocalFunction(object o)
        {
            return o.ToString(); // FN: local functions are not supported by the CFG
        }

        static string LocalStaticFunction(object o)
        {
            return o.ToString(); // FN: local functions are not supported by the CFG
        }
    }
}

public class Address
{
    public string Name { get; }

    public string State { get; }

    public void Deconstruct(out string name, out string state) =>
        (name, state) = (Name, State);
}

public class Person
{
    public string Name { get; }

    public Address Address { get; }

    public void Deconstruct(out string name, out Address address) =>
        (name, address) = (Name, Address);
}

public class SwitchExpressions
{
    public void OnlyDiscardBranch_Noncompliant(string s, bool b)
    {
        var result = b switch
        {
            _ => s.ToString() // FIXME Non-compliant
        };
    }

    public void MultipleBranches_Noncompliant(string s, int val)
    {
        var result = val switch
        {
            1 => "a",
            2 => s.ToString(), // FIXME Non-compliant
            _ => "b"
        };
    }

    public void Nested_Noncompliant(string s, int val, bool condition)
    {
        var result = val switch
        {
            1 => "a",
            2 => condition switch
            {
                _ => s.ToString() // FIXME Non-compliant
            },
            _ => "b"
        };
    }

    public void MultipleBranches_HandleNull(string s, int val)
    {
        var result = s switch
        {
            null => s.ToString(),   // FIXME Non-compliant
            _ => s.ToString()       // Compliant as null was already handled
        };
    }

    public void MultipleBranches_Compliant(string s, int val)
    {
        var result = val switch
        {
            1 => "a",
            2 => s == null ? string.Empty : s.ToString(),
            _ => "b"
        };
    }

    public string MultipleBranches_PropertyPattern(Address address, string s)
    {
        return address switch
        {
            { State: "WA" } addr => s.ToString(), // FIXME Non-compliant
            _ => string.Empty
        };
    }

    public string MultipleBranches_PropertyPattern_FP(string s)
    {
        return s switch
        {
            { Length: 5 } => s.ToString(), // FIXME Non-compliant - FP we know that the length is 5 so the string cannot be null
            _ => string.Empty
        };
    }

    public string MultipleBranches_RecursivePattern(Person person, string s)
    {
        return person switch
        {
            { Address: { State: "WA" } } pers => s.ToString(), // FIXME Non-compliant
            _ => string.Empty
        };
    }

    public string MultipleBranches_TuplePattern(Address address, string s)
    {
        return address switch
        {
            var (name, state) => s.ToString(), // FN
            _ => string.Empty
        };
    }

    public string MultipleBranches_WhenClause(Address address, string s)
    {
        return address switch
        {
            Address addr when addr.Name.Length > 0 => s.ToString(),         // FIXME Non-compliant
            Address addr when addr.Name.Length == s.Length => string.Empty, // FIXME Non-compliant
            _ => string.Empty
        };
    }

    public string MultipleBranches_VarDeclaration(Address address, string s)
    {
        return address switch
        {
            Address addr => s.ToString(), // FIXME Non-compliant
            _ => string.Empty
        };
    }

    public string TwoBranches_NoDefault(bool condition, string s)
    {
        return condition switch
        {
            true => s.ToString(), // FIXME Non-compliant
            false => s.ToString() // FIXME Non-compliant
        };
    }
}

public class SwitchStatement
{
    public void Test(string s)
    {
        switch (s)
        {
            case null:
                break;

            default:
                s.ToString(); // Compliant - the null is handled by the case null branch.
                break;
        }
    }
}
