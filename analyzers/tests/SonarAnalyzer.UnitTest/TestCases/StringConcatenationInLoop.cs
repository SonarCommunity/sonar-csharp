﻿using System.Collections.Generic;
using System.IO;

namespace Tests.Diagnostics
{
    public class StringConcatenationInLoop
    {
        public StringConcatenationInLoop(IList<MyObject> objects)
        {
            string s = "";
            int t = 0;

            for (int i = 0; i < 50; i++)
            {
                var sLoop = "";

                s = s + "a" + "b";  // Noncompliant
//              ^^^^^^^^^^^^^^^^^
                s += "a";     // Noncompliant {{Use a StringBuilder instead.}}
                sLoop += "a"; // Compliant

                i += 1;
                i = i + 1;
                t += 1;
                t = t + 1;
            }

            while (true)
            {
                var sLoop = "";

                s = s + "a"; // Noncompliant
                s += "a"; // Noncompliant
                sLoop = s + "a"; // Compliant
                sLoop += s + "a"; // Compliant

                // See https://github.com/SonarSource/sonar-dotnet/issues/1138
                s = s ?? "b";
            }

            foreach (var o in objects)
            {
                var sLoop = "";

                s = s + "a"; // Noncompliant
                s += "a"; // Noncompliant
                sLoop = s + "a"; // Compliant
                sLoop += s + "a"; // Compliant
            }

            do
            {
                var sLoop = "";

                s = s + "a"; // Noncompliant
                s += "a"; // Noncompliant
                sLoop = s + "a"; // Compliant
                sLoop += s + "a"; // Compliant
            }
            while (true);

            s = s + "a"; // Compliant
            s += "a"; // Compliant
        }

        // https://github.com/SonarSource/sonar-dotnet/issues/5521
        void Repro_5521(IList<MyObject> objects)
        {
            foreach (var obj in objects)
            {
                obj.Name += "a"; // Noncompliant FP
                obj.Name = obj.Name + "a"; // Noncompliant FP
            }
        }
    }

    public class MyObject
    {
        public string Name { get; set; }
    }
}
