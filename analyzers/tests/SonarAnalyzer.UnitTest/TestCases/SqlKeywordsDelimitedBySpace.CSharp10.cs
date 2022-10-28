﻿using System;
using System.Data.SqlClient;
using Linq = System.Linq;

namespace Tests.Diagnostics
{
    class Examples
    {
        public void VariousSqlKeywords(string unknownValue)
        {
            const string s1 = "TRUNCATE";
            const string s2 = "TABLE HumanResources.JobCandidate;";
            const string noncompliant1 = $"{s1}{s2}"; // Noncompliant {{Add a space before 'TABLE'.}}
//                                             ^^^^

            const string noncompliant2 = $"{s1}TABLE HumanResources.JobCandidate;"; // Noncompliant
//                                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            const string noncompliant3 = $"TRUNCATE{s2}"; // Noncompliant
//                                                 ^^^^

            const string s3 = "SELECT e.*, f";
            const string s4 = "ORDER BY LastName";
            const string noncompliant4 = $"{s3}FROM DimEmployee AS e{s4}"; // Noncompliant
//Noncompliant@-1

            const string s5 = "TRUNCATE ";
            const string s6 = "TABLE HumanResources.JobCandidate;";
            const string compliant1 = $"{s5}{s6}";

            const string s7 = "TRUNCATE";
            const string s8 = " TABLE HumanResources.JobCandidate;";
            const string compliant2 = $"{s7}{s8}";

            const string compliant3 = $"{s1} {s2}";
            string compliant4 = $"{s1}{42}";

            string compliant5 = $"{s1}{unknownValue}{s2}";

            string a = string.Empty;
            string b = "TABLE HumanResources.JobCandidate;";
            a = "TRUNCATE";

            string noncompliant5 = $"{a}{b}"; // Noncompliant

            const string complexCase = $"{s1}{$"{s1}{s2}"}"; // Noncompliant
// Noncompliant@-1

            const string complexCase2 = $"{s1}{noncompliant1}"; // Noncompliant {{Add a space before 'TRUNCATETABLE'.}}

            int x = 42;

            (x, var y) = (x, "TRUNCATE" + "TABLE HumanResources.JobCandidate;"); // Noncompliant
        }
    }

    // https://github.com/SonarSource/sonar-dotnet/issues/6126
    public class Repro_6249
    {
        string SomeColumn { get; set; }

        public void Method()
        {
            const string sql = "UPDATE [some_table]" +          // FN
                $"SET [some_column] = @{nameof(SomeColumn)}," +
                $" [other_column] = @{nameof(SomeColumn)}";     // Noncompliant FP
        }
    }
}
