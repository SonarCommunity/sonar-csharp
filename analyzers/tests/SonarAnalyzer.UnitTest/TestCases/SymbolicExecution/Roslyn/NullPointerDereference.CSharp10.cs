﻿using System;
using System.Text;

public class Sample
{
    private string field;

    public void Examples()
    {
        StringBuilder sb = new();

        (sb, int a) = (null, 42);
        sb.ToString(); // FN
    }

    public void Unassigned()
    {
        StringBuilder isNull, hasValue;
        (isNull, hasValue) = (null, new StringBuilder());
        isNull.ToString();      // FN
        hasValue.ToString();
    }
}
