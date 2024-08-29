using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

class Compliant
{
    void Ctor()
    {
        var defaultOrder = new Regex("single space"); // Compliant

        var namedArgs = new Regex(
            pattern: "single space");

        var noRegex = new NoRegex("single space"); // Compliant

        var singleOption = new Regex("ignore  pattern  white space", RegexOptions.IgnorePatternWhitespace); // Compliant
        var mixedOptions = new Regex("ignore  pattern  white space", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled); // Compliant
    }

    void Static()
    {
        var isMatch = Regex.IsMatch("some input", "single space"); // Compliant
        var noRegex = NoRegex.IsMatch("some input", "multiple  white  spaces"); // Compliant
    }

    bool ConcatanationMultiline(string input)
    {
        return Regex.IsMatch(input, "single space"
            + "|b"
            + "|c"
            + "|d]"); // Compliant
    }

    bool ConcatanationSingleIne(string input)
    {
        return Regex.IsMatch(input, "a" + "|b" + "|c" + "|single white space"); // Compliant
    }

    [RegularExpression("single space")] // Compliant
    public string Attribute { get; set; }

    bool WhiteSpaceVariations(string input)
    {
        return Regex.IsMatch(input, " with multple single spaces ")
            || Regex.IsMatch(input, "without_spaces")
            || Regex.IsMatch(input, "with\ttab")
            || Regex.IsMatch(input, "")
            || Regex.IsMatch(input, "\x09 character tabulation")
            || Regex.IsMatch(input, "\x0A line feed")
            || Regex.IsMatch(input, "\x0B line tabulation")
            || Regex.IsMatch(input, "\x0C form feed")
            || Regex.IsMatch(input, "\x0D carriage feed")
            || Regex.IsMatch(input, "\x85 next line")
            || Regex.IsMatch(input, "\xA0 non-break space")
            || Regex.IsMatch(input, "ignore  pattern  white space", RegexOptions.IgnorePatternWhitespace);
    }
}

class Noncompliant
{
    private const string Prefix = ".*";

    void Ctor()
    {
        var patternOnly = new Regex("multiple  white          spaces"); // Noncompliant {{Regular expressions should not contain multiple spaces.}}
        //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        var withConst = new Regex(Prefix + "multiple  white  spaces"); // Noncompliant
    }

    void Static()
    {
        var isMatch = Regex.IsMatch("some input", "multiple  white  spaces");    // Noncompliant
        //                                        ^^^^^^^^^^^^^^^^^^^^^^^^^
        var match = Regex.Match("some input", "multiple  white  spaces");        // Noncompliant
        var matches = Regex.Matches("some input", "multiple  white  spaces");    // Noncompliant
        var split = Regex.Split("some input", "multiple  white  spaces");        // Noncompliant

        var replace = Regex.Replace("some input", "multiple  white  spaces", "some replacement"); // Noncompliant
    }

    bool Multiline(string input)
    {
        return Regex.IsMatch(input,
            @"|b
              |c
              |multiple  white  spaces");  // Noncompliant @-2
    }

    bool ConcatanationMultiline(string input)
    {
        return Regex.IsMatch(input, "a" // Noncompliant
            + "|b"
            + "|c"
            + "|multiple  white  spaces");
    }

    bool ConcatanationSingleIne(string input)
    {
        return Regex.IsMatch(input, "a" + "|b" + "|c" + "|multiple  white  spaces"); // Noncompliant
    }

    [RegularExpression("multiple  white  spaces")] // Noncompliant
    public string Attribute { get; set; }

    [System.ComponentModel.DataAnnotations.RegularExpression("multiple  white  spaces")] // Noncompliant
    public string AttributeFullySpecified { get; set; }

    [global::System.ComponentModel.DataAnnotations.RegularExpression("multiple  white  spaces")] // Noncompliant
    public string AttributeGloballySpecified { get; set; }
}

class Invalid
{
    void FalseNegative(string unknown)
    {
        var regex = new NoRegex(unknown + "multiple  white  spaces"); // FN
    }

    void FalsePositive()
    {
        var withComment = new Regex("(?# comment  with  spaces)"); // Noncompliant, FP
    }
}

class DoesNotCrash
{
    bool UnknownVariable(string input)
    {
        return Regex.IsMatch(input, "a" + undefined); // Error [CS0103] The name 'undefined' does not exist in the current context
    }
}

public class NoRegex
{
    public NoRegex(string pattern) { }

    public static bool IsMatch(string input, string pattern) { return true; }
}
