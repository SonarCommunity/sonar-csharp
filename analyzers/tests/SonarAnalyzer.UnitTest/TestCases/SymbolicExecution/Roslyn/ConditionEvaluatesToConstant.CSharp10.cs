﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarAnalyzer.UnitTest.TestCases;

struct MyStruct
{
    public bool x;
    public bool y = false;

    public MyStruct()
    {
        x = false;
    }
}

class AClass
{
    public static void DoSomething()
    {
        MyStruct myStruct = new();
        if (myStruct.x) { } // FN
        else if (myStruct.y) { } // FN
    }
}
