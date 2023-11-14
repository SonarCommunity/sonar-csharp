﻿using Microsoft.AspNetCore.Components;
using System;

[Route("/query-parameters")]
class SupplyParameterFromQueryNeedsRoutableComponent_Compliant : ComponentBase
{
    [Parameter]
    [SupplyParameterFromQuery]
    public TimeSpan TimeSpan { get; set; } // Compliant

    [Parameter, SupplyParameterFromQuery]
    public int MyInt { get; set; } // Compliant

    [Parameter]
    public string SupplyParameterFromQueryAttributeMissing { get; set; } // Compliant: missing [SupplyParameterFromQuery]

    [SupplyParameterFromQuery]
    public string ParameterAttributeMissing { get; set; } // Compliant: missing [Parameter]
}
