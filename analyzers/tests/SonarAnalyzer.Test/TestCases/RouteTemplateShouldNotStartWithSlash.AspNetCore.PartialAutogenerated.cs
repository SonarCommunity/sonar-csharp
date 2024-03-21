﻿// <auto-generated/>
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

[Route("[controller]")]
public partial class NoncompliantPartialAutogeneratedController : Controller // This is non-compliant but primary issues are not reported on auto-generated classes
{
    [HttpGet("/[action]")]                      // Secondary
    public IActionResult Index3() => View();

    [HttpGet("/SubPath/Index4_1")]              // Secondary
    public IActionResult Index4() => View();
}

[Route("[controller]")]
public partial class CompliantPartialAutogeneratedController : Controller // This class makes its partial partial part in RouteTemplateShouldNotStartWithSlash.AspNet4x.cs compliant
{
    [HttpGet("[action]")]
    public IActionResult Index3() => View();
}
