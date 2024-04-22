﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

[ApiController]
[Route("Hello")]
public class SomeController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;
    private HttpClient clientField = new HttpClient();                                // Compliant, it can be reused between actions
    private HttpClient ClientProperty { get; set; } = new HttpClient();               // Compliant, it can be reused between actions
    private HttpClient ClientPropertyAccessorArrowClause { get => new HttpClient(); } // Noncompliant
    private HttpClient ClientPropertyAccessorMethodBody { get { var anotherStatement = 1; return new HttpClient(); } } // Noncompliant
    private HttpClient ClientPropertyAccessorArrow => new HttpClient();                                                // Noncompliant

    public SomeController()
    {
        clientField = new HttpClient();                           // Compliant
    }

    [HttpGet("foo")]
    public async Task<string> Foo()
    {
        using var clientA = new HttpClient();                     // Noncompliant
        //                  ^^^^^^^^^^^^^^^^
        await clientA.GetStringAsync("");

        using (var clientB = new HttpClient())                    //Noncompliant
        {
            await clientB.GetStringAsync("");
        }

        var client = new HttpClient();                            // Noncompliant
        client = new();                                           // Noncompliant
        clientField = new HttpClient();                           // Noncompliant
        ClientProperty = new HttpClient();                        // Noncompliant
        var local = new HttpClient();                             // Noncompliant
        local = new System.Net.Http.HttpClient();                 // Noncompliant
        var fromStaticMethod = StaticCreateClient();              // FN - see https://github.com/SonarSource/rspec/pull/3847#discussion_r1559510167
        var fromMethod = CreateClient();                          // FN - see https://github.com/SonarSource/rspec/pull/3847#discussion_r1559510167

        local = ClientPropertyAccessorArrow;                      // Compliant
        local = ClientPropertyAccessorArrow;                      // Compliant
        clientField ??= new HttpClient();                         // Compliant
        using var pooledClient = _clientFactory.CreateClient();   // Compliant

        // Lambda
        _ = new Lazy<HttpClient>(() => new HttpClient());         // Noncompliant FP

        // Conditional code
        if (true)
            _ = new HttpClient();                                 // Compliant
        switch (true)
        {
            case true:
                _ = new HttpClient();                             // Compliant
                break;
        }
        _ = true switch { true => new HttpClient() };             // Compliant
        _ = true ? new HttpClient() : null;                       // Compliant

        return "bar";
    }

    private static HttpClient StaticCreateClient()
    {
        return new HttpClient();                                  // Compliant, we raise only in actions
    }

    private HttpClient CreateClient()
    {
        return new HttpClient();                                  // Compliant, we raise only in actions
    }
}
