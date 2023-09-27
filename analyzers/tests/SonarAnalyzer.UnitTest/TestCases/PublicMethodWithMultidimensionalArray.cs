﻿public interface IFace
{
    void Method5(int[,] a); //Noncompliant
//       ^^^^^^^
}

public abstract class Base
{
    public abstract void Method4(int[,] a); //Noncompliant {{Make this method private or simplify its parameters to not use multidimensional/jagged arrays.}}
}

public class PublicMethodWithMultidimensionalArray : Base, IFace
{
    public void Method(int a)
    {
    }
    public void MethodX(int[] a)
    {
    }
    public void Method1(int[][] a) //Noncompliant
    {
    }
    void Method2(int[][] a) //Compliant
    {
    }
    public void Method3(int[,] a) //Noncompliant
    {
    }

    public override void Method4(int[,] a) //Compliant, overrides
    {
    }

    public void Method5(int[,] a) //Compliant, implements interface
    {
    }

    void Method6(params int[][,] a) // Compliant
    {
    }

    public void Method7(params int[][] a) // Compliant
    {
    }

    public void Method8(params int[][][] a) // Noncompliant
    {
    }

    public void Method9(int[][] a, params int[][] b) // Noncompliant
    {
    }

    public void Method10(int[,][] a) // Noncompliant
    {
    }
}

internal class Other
{
    public void Method1(int[][] a) // Compliant, class is internal
    {
    }
}

// https://github.com/SonarSource/sonar-dotnet/issues/8083
namespace Repro_8083
{
    public class Constructors
    {
        public Constructors(int[][] a) { }          // FN, the ctor is publicly exposed
        public Constructors(int[,] a) { }           // FN
        public Constructors(params int[] a) { }     // Compliant, params of int
        public Constructors(params int[][][] a) { } // FN, params of int[][]
        public Constructors(int i) { }              // Compliant, not a multi-dimensional array
    }
}
