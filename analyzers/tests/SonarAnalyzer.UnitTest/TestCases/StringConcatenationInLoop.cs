﻿using System.Collections.Generic;
using System.IO;

namespace Tests.Diagnostics
{
    public class StringConcatenationInLoop
    {
        public StringConcatenationInLoop()
        {
            string s = "";
            for (int i = 0; i < 50; i++)
            {
                var sLoop = "";

                s = s + "a" + "b";  // Noncompliant
//              ^^^^^^^^^^^^^^^^^
                s += "a";     // Noncompliant {{Use a StringBuilder instead.}}
                sLoop += "a"; // Compliant

                i += 5;
            }
            s += "a";

            while (true)
            {
                // See https://github.com/SonarSource/sonar-dotnet/issues/1138
                s = s ?? "b";
            }
        }

        void MarkDisabled(IList<MyObject> objects)
        {
            foreach (var obj in objects)
            {
                obj.Name += " - DISABLED"; // Noncompliant, FP See: https://github.com/SonarSource/sonar-dotnet/issues/5521
            }
        }
    }

    class MyObject
    {
        public string Name { get; set; }
    }
}
