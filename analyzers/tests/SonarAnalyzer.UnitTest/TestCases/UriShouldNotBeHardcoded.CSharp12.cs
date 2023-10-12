﻿using System;
using System.Collections.Generic;

class PrimaryConstructor(string ctorParamUri = "file://blah.txt", // Noncompliant
                         string ctorParam = "file://blah.txt") // Compliant
{
    void Method(string methodParamUri = "file://blah.txt", // Noncompliant
                string methodParam = "file://blah.txt") // Compliant
    {
        var lambda = (string lambdaParamUri = "file://blah.txt") => lambdaParamUri; // Noncompliant
        var lambda2 = (string lambdaParam = "file://blah.txt") => lambdaParam; // Compliant

        var lambdaMultipleNoncompliances = (string paramUri1 = "file://blah.txt", string paramUri2 = "file://blah.txt") => paramUri1;
        //                                                     ^^^^^^^^^^^^^^^^^
        //                                                                                           ^^^^^^^^^^^^^^^^^@-1
    }
}

struct PrimaryConstructorStruct(string ctorParamUri = "file://blah.txt", // Noncompliant
                         string ctorParam = "file://blah.txt") // Compliant
{
}

record class PrimaryConstructorRecordClass(string ctorParamUri = "file://blah.txt", // Noncompliant
                         string ctorParam = "file://blah.txt") // Compliant
{
}

record struct PrimaryConstructorRecordStruct(string ctorParamUri = "file://blah.txt", // Noncompliant
                         string ctorParam = "file://blah.txt") // Compliant
{
}

// https://github.com/SonarSource/sonar-dotnet/pull/8146
class Repro_8146
{
    void Method()
    {
        IList<string> uris1 = new[] { "C:/test.txt" }; // FN
        IList<string> uris2 = ["C:/test.txt"]; // FN
        string[][] urisMatrix1 = [["C:/test.txt"]]; // FN
        IDictionary<string, string> urisDict = new Dictionary<string, string> { ["a"] = "C:/test.txt" }; // FN
    }
}
