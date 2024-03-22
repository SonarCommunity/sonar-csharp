﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class RouteTemplateShouldNotStartWithSlashTest
{
    private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.RouteTemplateShouldNotStartWithSlash>();
    private readonly VerifierBuilder builderVB = new VerifierBuilder<VB.RouteTemplateShouldNotStartWithSlash>();

#if NET
    private static IEnumerable<MetadataReference> AspNetCoreReferences =>
        [
            AspNetCoreMetadataReference.MicrosoftAspNetCore,                    // For WebApplication
            AspNetCoreMetadataReference.MicrosoftExtensionsHostingAbstractions, // For IHost
            AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpAbstractions,    // For HttpContext, RouteValueDictionary
            AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpFeatures,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcAbstractions,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcCore,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcViewFeatures,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreRouting,             // For IEndpointRouteBuilder
            AspNetCoreMetadataReference.MicrosoftAspNetCoreRazorPages,          // For RazorPagesEndpointRouteBuilderExtensions.MapFallbackToPage
        ];

    [TestMethod]
    public void RouteTemplateShouldNotStartWithSlash_CS() =>
        builderCS
            .AddPaths("RouteTemplateShouldNotStartWithSlash.AspNetCore.cs", "RouteTemplateShouldNotStartWithSlash.AspNetCore.PartialAutogenerated.cs")
            .WithConcurrentAnalysis(false)
            .AddReferences(AspNetCoreReferences)
            .Verify();

    [DataRow("""[HttpGet("/Index1")]""", "", false)]
    [DataRow("""[Route("/Index2")]""", "", false)]
    [DataRow("""[HttpGet("\\Index1")]""", "", true)]
    [DataRow("""[Route("\\Index2")]""", "", true)]
    [DataRow("""[HttpGet("Index1/SubPath")]""", "", true)]
    [DataRow("""[Route("Index2/SubPath")]""", "", true)]
    [DataRow("""[Route("/IndexA")]""", """[Route("/IndexB")]""", false)]
    [DataRow("""[Route("/IndexC")]""", """[HttpGet("/IndexD")]""", false)]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_RouteAttributes(string firstAttribute, string secondAttribute, bool compliant)
    {
        var builder = builderCS.AddReferences(AspNetCoreReferences)
            .AddSnippet($$"""
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.Mvc.Routing;

                [Route("[controller]")]
                public class BasicsController : Controller {{(compliant ? string.Empty : " // Noncompliant")}}
                {
                    {{firstAttribute}}{{(compliant ? string.Empty : " // Secondary")}}
                    {{secondAttribute}}{{(compliant || string.IsNullOrEmpty(secondAttribute) ? string.Empty : " // Secondary")}}
                    public ActionResult SomeAction() => View();
                }
                """);

        if (compliant)
        {
            builder.VerifyNoIssueReported();
        }
        else
        {
            builder.Verify();
        }
    }

    [DataRow("""[HttpGet("/IndexGet")]""")]
    [DataRow("""[HttpPost("/IndexPost")]""")]
    [DataRow("""[HttpPut("/IndexPut")]""")]
    [DataRow("""[HttpDelete("/IndexDelete")]""")]
    [DataRow("""[HttpPatch("/IndexPatch")]""")]
    [DataRow("""[HttpHead("/IndexHead")]""")]
    [DataRow("""[HttpOptions("/IndexOptions")]""")]
    public void RouteTemplateShouldNotStartWithSlash_HttpAttributes(string attribute)
    {
        var builder = builderCS.AddReferences(AspNetCoreReferences)
            .AddSnippet($$"""
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.Mvc.Routing;

                public class BasicsController : Controller  // Noncompliant
                {
                    {{attribute}} // Secondary
                    public ActionResult SomeAction() => View();
                }
                """);
        builder.Verify();
    }

    [DataRow("""[Route(template: @"/[action]", Name = "a", Order = 42)]""")]
    [DataRow("""[RouteAttribute(@"/[action]")]""")]
    [DataRow("""[Microsoft.AspNetCore.Mvc.RouteAttribute(@"/[action]")]""")]
    [DataRow("""[method:Route(@"/[action]")]""")]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_AttributeSyntaxVariations(string attribute)
    {
        var builder = builderCS.AddReferences(AspNetCoreReferences)
            .AddSnippet($$"""
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.Mvc.Routing;

                public class BasicsController : Controller  // Noncompliant
                {
                    {{attribute}} // Secondary
                    public ActionResult SomeAction() => View();
                }
                """);
        builder.Verify();
    }

    [DataRow("""(@"/[action]")""", false)]
    [DataRow("""("/[action]")""", false)]
    [DataRow("""("\u002f[action]")""", false)]
    [DataRow("""($"/{ConstA}/[action]")""", false)]
    [DataRow(""""("""/[action]""")"""", false)]
    [DataRow(""""""(""""/[action]"""")"""""", false)]
    [DataRow(""""($$"""/{{ConstA}}/[action]""")"""", false)]
    [DataRow("""(@"/[action]", Name = "a", Order = 42)""", false)]
    [DataRow("""($"{ConstA}/[action]")""", true)]
    [DataRow("""($"{ConstSlash}[action]")""", false)]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_WithAllTypesOfStrings(string attributeParameter, bool compliant)
    {
        var builder = builderCS
            .AddReferences(AspNetCoreReferences)
            .WithOptions(ParseOptionsHelper.FromCSharp11)
            .AddSnippet($$"""
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.Mvc.Routing;

                public class BasicsController : Controller {{(compliant ? string.Empty : " // Noncompliant")}}
                {
                    private const string ConstA = "A";
                    private const string ConstSlash = "/";

                    [Route{{attributeParameter}}] {{(compliant ? string.Empty : " // Secondary")}}
                    public ActionResult SomeAction() => View();
                }
                """);

        if (compliant)
        {
            builder.VerifyNoIssueReported();
        }
        else
        {
            builder.Verify();
        }
    }

    [TestMethod]
    public void RouteTemplateShouldNotStartWithSlash_VB() =>
        builderVB
            .AddPaths("RouteTemplateShouldNotStartWithSlash.AspNetCore.vb")
            .AddReferences(AspNetCoreReferences)
            .Verify();

#endif

#if NETFRAMEWORK
    // ASP.NET 4x MVC 3 and 4 don't support attribute routing, nor MapControllerRoute and similar
    public static IEnumerable<object[]> AspNet4xMvcVersionsUnderTest => [["5.2.7"] /* Most used */, [Constants.NuGetLatestVersion]];

    private static IEnumerable<MetadataReference> AspNet4xReferences(string aspNetMvcVersion) =>
        MetadataReferenceFacade.SystemWeb
            .Concat(NuGetMetadataReference.MicrosoftAspNetMvc(aspNetMvcVersion));

    [TestMethod]
    [DynamicData(nameof(AspNet4xMvcVersionsUnderTest))]
    public void RouteTemplateShouldNotStartWithSlash_CS(string aspNetMvcVersion) =>
        builderCS
            .AddPaths("RouteTemplateShouldNotStartWithSlash.AspNet4x.cs", "RouteTemplateShouldNotStartWithSlash.AspNet4x.PartialAutogenerated.cs")
            .WithConcurrentAnalysis(false)
            .AddReferences(AspNet4xReferences(aspNetMvcVersion))
            .Verify();

    [TestMethod]
    [DynamicData(nameof(AspNet4xMvcVersionsUnderTest))]
    public void RouteTemplateShouldNotStartWithSlash_VB(string aspNetMvcVersion) =>
        builderVB
            .AddPaths("RouteTemplateShouldNotStartWithSlash.AspNet4x.vb")
            .AddReferences(AspNet4xReferences(aspNetMvcVersion))
            .Verify();

    [DataRow("/Index2", false)]
    [DataRow("\\Index2", true)]
    [DataRow("Index1/SubPath", true)]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_WithHttpAttribute(string attributeParameter, bool compliant)
    {
        var builder = builderCS.AddReferences(AspNet4xReferences("5.2.7")).AddSnippet($$"""
            using System.Web.Mvc;

            [Route("[controller]")]
            public class BasicsController : Controller {{(compliant ? string.Empty : " // Noncompliant")}}
            {
                [HttpGet]
                [Route("{{attributeParameter}}")] {{(compliant ? string.Empty : " // Secondary")}}
                public ActionResult SomeAction() => View();
            }
            """);

        if (compliant)
        {
            builder.VerifyNoIssueReported();
        }
        else
        {
            builder.Verify();
        }
    }

    [DataRow("""[Route(template: @"/[action]", Name = "a", Order = 42)]""", false)]
    [DataRow("""[Route(template: @"[action]", Name = "/a", Order = 42)]""", true)]
    [DataRow("""[RouteAttribute(@"/[action]")]""", false)]
    [DataRow("""[RouteAttribute(@"/[action]")]""", false)]
    [DataRow("""[System.Web.Mvc.RouteAttribute(@"/[action]")]""", false)]
    [DataRow("""[method:Route(@"/[action]")]""", false)]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_WithAttributeSyntaxVariations(string attribute, bool compliant) =>
        builderCS.AddReferences(AspNet4xReferences("5.2.7"))
            .WithOptions(ParseOptionsHelper.FromCSharp11)
            .AddSnippet($$"""
                using System.Web.Mvc;

                [Route("[controller]")]
                public class BasicsController : Controller {{(compliant ? string.Empty : " // Noncompliant")}}
                {
                    {{attribute}} {{(compliant ? string.Empty : " // Secondary")}}
                    public ActionResult SomeAction() => View();
                }
                """)
            .Verify();

    [DataRow("""(@"/[action]")""", false)]
    [DataRow("""("/[action]")""", false)]
    [DataRow("""("\u002f[action]")""", false)]
    [DataRow("""($"/{ConstA}/[action]")""", false)]
    [DataRow(""""("""/[action]""")"""", false)]
    [DataRow(""""""(""""/[action]"""")"""""", false)]
    [DataRow(""""($$"""/{{ConstA}}/[action]""")"""", false)]
    [DataRow("""(@"/[action]", Name = "a", Order = 42)""", false)]
    [DataRow("""($"{ConstA}/[action]")""", true)]
    [DataRow("""($"{ConstSlash}[action]")""", false)]
    [DataTestMethod]
    public void RouteTemplateShouldNotStartWithSlash_WithAllTypesOfStrings(string attributeParameter, bool compliant)
    {
        var builder = builderCS
            .AddReferences(AspNet4xReferences("5.2.7"))
            .WithOptions(ParseOptionsHelper.FromCSharp11)
            .AddSnippet($$"""
                using System.Web.Mvc;

                [Route("[controller]")]
                public class BasicsController : Controller {{(compliant ? string.Empty : " // Noncompliant")}}
                {
                    private const string ConstA = "A";
                    private const string ConstSlash = "/";

                    [Route{{attributeParameter}}] {{(compliant ? string.Empty : " // Secondary")}}
                    public ActionResult SomeAction() => View();
                }
                """);

        if (compliant)
        {
            builder.VerifyNoIssueReported();
        }
        else
        {
            builder.Verify();
        }
    }
#endif

}
