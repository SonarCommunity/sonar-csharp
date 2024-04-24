﻿public class AllValid
{
    public static int s1;
    public static int s2 = 42;
    public static int s3, s4 = 42;
    public int i1;
    public int i2 = 42;
    public int i3, i4 = 42;

    internal static int internalS;
    internal int internalI;

    protected static int protectedS;    // Compliant
    protected int protectedI;

    private static int privateS;        // Compliant
    private int privateI;

    public const string C = "C";        // Compliant, while this is FieldDeclarationSyntax, it's not in the scope of this rule.
}

public class SomeValid
{
    public static int s1;
    public int i1;
    public int i2;
    public static int s2, s3;      // Noncompliant {{Move this static field above the public instance ones.}}
    //            ^^^^^^^^^^
    public int i3;

    // Compliant, there're no other private/protected/internal instance fields above
    internal static int internalS;
    protected static int protectedS;
    protected internal static int protectedInternalS;
    protected private static int protectedPrivateS;
    private static int privateS;
}

public class ProtectedInternal
{
    protected internal int protectedI;
    protected static int protectedS;    // Noncompliant {{Move this static field above the protected instance ones.}}
}

public class ProtectedPrivate
{
    protected private int protectedI;
    protected static int protectedS;    // Noncompliant {{Move this static field above the protected instance ones.}}
}

public class AllWrong
{
    public int i1;
    public int i2 = 42;
    public int i3, i4 = 42;
    protected int protectedI;
    private int privateI;
    internal int internalI;

    public static int s1;               // Noncompliant {{Move this static field above the public instance ones.}}
    public static int s2 = 42;          // Noncompliant
    public static int s3, s4 = 42;      // Noncompliant
    internal static int internalS;      // Noncompliant {{Move this static field above the internal instance ones.}}
    private static int privateS;        // Noncompliant {{Move this static field above the private instance ones.}}
    protected static int protectedS;    // Noncompliant {{Move this static field above the protected instance ones.}}
}

public record R
{
    private int i;
    private static int s;   // Noncompliant

}

public record struct RS
{
    private int i;
    private static int s;   // Noncompliant

}

public struct S
{
    private int i;
    private static int s;   // Noncompliant

}
