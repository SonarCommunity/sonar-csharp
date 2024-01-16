﻿using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Tests.Diagnostics
{
    public record Record
    {
        private int a; // Noncompliant {{Remove the unused private field 'a'.}}

        private int b;
        public int B() => b;

        private nint Value { get; init; }

        private nint UnusedValue { get; init; } // Noncompliant

        public Record Create() => new() {Value = 1};

        private interface IFoo // Noncompliant
        {
            public void Bar() { }
        }

        private record Nested(string Name, int CategoryId);

        public void UseNested()
        {
            Nested d = new("name", 2);
        }

        private record Nested2(string Name, int CategoryId);

        public void UseNested2()
        {
            _ = new Nested2("name", 2);
        }

        private record UnusedNested1(string Name, int CategoryId); // Noncompliant
//                     ^^^^^^^^^^^^^

        internal record UnusedNested2(string Name, int CategoryId); // Noncompliant
        public record UnusedNested3(string Name, int CategoryId);

        private int usedInPatternMatching = 1;

        public int UseInPatternMatching(int val) =>
            val switch
            {
                < 0 => usedInPatternMatching,
                >= 0 => 1
            };

        private class LocalFunctionAttribute : Attribute { }
        private class LocalFunctionAttribute2 : Attribute { }

        public void Foo()
        {
            [LocalFunction]
            static void Bar() { }

            [Obsolete]
            [NotExisting] // Error [CS0246]
                          // Error@-1 [CS0246]
            [LocalFunctionAttribute2]
            [LocalFunction]
            static void Quix() { }

            [Obsolete]
            static void ForCoverage() { }

            static void NoAttribute() { }
        }
    }

    public record PositionalRecord(string Value)
    {
        private int a; // Noncompliant
        private int b;
        public int B() => b;

        private record UnusedNested(string Name, int CategoryId) { } // Noncompliant
    }

    public class TargetTypedNew
    {
        private TargetTypedNew(int arg)
        {
            var x = arg;
        }

        private TargetTypedNew(string arg)                           // Noncompliant
        {
            var x = arg;
        }

        public static TargetTypedNew Create()
        {
            return new(42);
        }

        public static void Foo()
        {
            PositionalRecord @record = new PositionalRecord("");
        }
    }

    public partial class PartialMethods
    {
    }

    // https://github.com/SonarSource/sonar-dotnet/issues/2752
    public class ReproIssue2752
    {
        private record PrivateRecordRef
        {
            public uint part1; // Noncompliant FP. Type is communicated an external call.
        }

        [DllImport("user32.dll")]
        private static extern bool ExternalMethod(ref PrivateRecordRef reference);
    }

    // https://github.com/SonarSource/sonar-dotnet/issues/7904
    public sealed record Repro_7904
    {
        // Used by the runtime to create a string representation of the record.
        // See also https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#printmembers-formatting-in-derived-records
        private bool PrintMembers(StringBuilder builder) // Noncompliant FP
        {
            return true;
        }
    }

    public class Repro_7904_2
    {
        private bool PrintMembers(StringBuilder builder) // Noncompliant
        {
            return true;
        }
    }
}
