﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

var fs0 = new FileStream(@"c:\foo.txt", FileMode.Open); // Noncompliant
FileStream fs1 = new(@"c:\foo.txt", FileMode.Open);     // Noncompliant

var fs2 = new FileStream(@"c:\foo.txt", FileMode.Open); // Compliant, passed to a method
NoOperation(fs2);

FileStream fs3; // Compliant - not instantiated
using var fs5 = new FileStream(@"c:\foo.txt", FileMode.Open); // Compliant

using FileStream fs6 = new(@"c:\foo.txt", FileMode.Open); // Compliant

void NoOperation(object x) { }

class Foo
{
    public void Bar(object cond)
    {
        var fs = new FileStream("", FileMode.Open); // FN, not disposed on all paths
        if (cond is 5)
        {
            fs.Dispose();
        }
        else if (cond is not 599)
        {
            fs.Dispose();
        }
    }

    public void Lambdas()
    {
        Action<int, int> a = static (int v, int w) => {
            var fs = new FileStream("", FileMode.Open); // Noncompliant
        };
        Action<int, int> b = (_, _) => {
            var fs = new FileStream("", FileMode.Open);
            fs.Dispose();
        };
        Action<int, int> с = static (int v, int w) => {
            FileStream fs = new("", FileMode.Open); // Noncompliant
        };
        Action<int, int> в = (_, _) => {
            FileStream fs = new("", FileMode.Open);
            fs.Dispose();
        };
    }
}

record MyRecord
{
    private FileStream field_fs1; // Compliant - not instantiated
    public FileStream field_fs2 = new FileStream(@"c:\foo.txt", FileMode.Open); // Compliant - public
    private FileStream field_fs3 = new FileStream(@"c:\foo.txt", FileMode.Open); // Noncompliant {{Dispose 'field_fs3' when it is no longer needed.}}
    private FileStream field_fs4 = new FileStream(@"c:\foo.txt", FileMode.Open); // Compliant - disposed
    private FileStream field_fs5 = new(@"c:\foo.txt", FileMode.Open); // Noncompliant {{Dispose 'field_fs5' when it is no longer needed.}}

    private FileStream backing_field1;
    public FileStream Prop1
    {
        init
        {
            backing_field1 = new FileStream("", FileMode.Open); // Noncompliant
        }
    }

    private FileStream backing_field2;
    public FileStream Prop2
    {
        init
        {
            backing_field2 = new ("", FileMode.Open); // Noncompliant
        }
    }

    public void Foo()
    {
        field_fs4.Dispose();

        FileStream fs5; // Compliant - used properly
        using (fs5 = new FileStream(@"c:\foo.txt", FileMode.Open))
        {
            // do nothing but dispose
        }

        using (fs5 = new(@"c:\foo.txt", FileMode.Open))
        {
            // do nothing but dispose
        }

        FileStream fs1 = new(@"c:\foo.txt", FileMode.Open);        // Noncompliant
        var fs2 = File.Open(@"c:\foo.txt", FileMode.Open);         // Noncompliant - instantiated with factory method
        var s = new WebClient();                                   // Noncompliant - another tracked type
    }
}
