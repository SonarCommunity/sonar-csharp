﻿using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TestSamples;

public partial class Sample
{
    [Parameter]
    public Dictionary<string, object> Attributes { get; set; } = new (); // Compliant

    public Dictionary<string, object> Items { get; set; } = new (); // Noncompliant
}
