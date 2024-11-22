﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

using System.Reflection;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.Test.Rules;

#pragma warning disable T0008 // Internal Styling Rule T0008
#pragma warning disable T0009 // Internal Styling Rule T0009

[TestClass]
public class BackslashShouldBeAvoidedInAspNetRoutesTest
{
    private const string AttributePlaceholder = "<attributeName>";

    private const string SlashAndBackslashConstants = """
        private const string ASlash = "/";
        private const string ABackSlash = @"\";
        private const string AConstStringIncludingABackslash = $"A{ABackSlash}";
        private const string AConstStringNotIncludingABackslash = $"A{ASlash}";
        """;

    private static readonly object[][] AttributesWithAllTypesOfStrings =
    [
        [$"[{AttributePlaceholder}(AConstStringIncludingABackslash)]", false, "ConstStringIncludingABackslash"],
        [$"[{AttributePlaceholder}(AConstStringNotIncludingABackslash)]", true, "ConstStringNotIncludingABackslash"],
        [$"""[{AttributePlaceholder}("\u002f[action]")]""", true, "EscapeCodeOfSlash"],
        [$"""[{AttributePlaceholder}("\u005c[action]")]""", false, "EscapeCodeOfBackslash"],
        [$$"""[{{AttributePlaceholder}}($"A{ASlash}[action]")]""", true, "InterpolatedString"],
        [$$"""[{{AttributePlaceholder}}($@"A{ABackSlash}[action]")]""", false, "InterpolatedVerbatimString"],
        [$""""[{AttributePlaceholder}("""\[action]""")]"""", false, "RawStringLiteralsTriple"],
        [$"""""[{AttributePlaceholder}(""""\[action]"""")]""""", false, "RawStringLiteralsQuadruple"],
        [$$$""""[{{{AttributePlaceholder}}}($$"""{{ABackSlash}}/[action]""")]"""", false, "InterpolatedRawStringLiteralsIncludingABackslash"],
        [$$$""""[{{{AttributePlaceholder}}}($$"""{{ASlash}}/[action]""")]"""", true, "InterpolatedRawStringLiteralsNotIncludingABackslash"],
    ];

    private readonly VerifierBuilder builderCS = new VerifierBuilder<CS.BackslashShouldBeAvoidedInAspNetRoutes>().WithBasePath("AspNet");
    private readonly VerifierBuilder builderVB = new VerifierBuilder<VB.BackslashShouldBeAvoidedInAspNetRoutes>().WithBasePath("AspNet");

    private static IEnumerable<object[]> RouteAttributesWithAllTypesOfStrings =>
        AttributesWithAllTypesOfStrings.Select(x => new object[] { ((string)x[0]).Replace(AttributePlaceholder, "Route"), x[1], x[2] });

    public static string AttributesWithAllTypesOfStringsDisplayNameProvider(MethodInfo methodInfo, object[] values) =>
        $"{methodInfo.Name}_{(string)values[2]}";

#if NETFRAMEWORK
    // ASP.NET 4x MVC 3 and 4 don't support attribute routing, nor MapControllerRoute and similar
    public static IEnumerable<object[]> AspNet4xMvcVersionsUnderTest =>
        [["5.2.7"] /* Most used */, [Constants.NuGetLatestVersion]];

    private static IEnumerable<MetadataReference> AspNet4xReferences(string aspNetMvcVersion) =>
        MetadataReferenceFacade.SystemWeb                                          // For HttpAttributeMethod and derived attributes
            .Concat(NuGetMetadataReference.MicrosoftAspNetMvc(aspNetMvcVersion));  // For Controller

    [TestMethod]
    [DynamicData(nameof(AspNet4xMvcVersionsUnderTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNet4x_CS(string aspNetMvcVersion) =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNet4x.cs")
            .AddReferences(AspNet4xReferences(aspNetMvcVersion))
            .Verify();

    [TestMethod]
    [DynamicData(
        nameof(RouteAttributesWithAllTypesOfStrings),
        DynamicDataDisplayName = nameof(AttributesWithAllTypesOfStringsDisplayNameProvider),
        DynamicDataDisplayNameDeclaringType = typeof(BackslashShouldBeAvoidedInAspNetRoutesTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNet4x_CS_Latest(string actionDeclaration, bool compliant, string displayName)
    {
        actionDeclaration = actionDeclaration.Replace(AttributePlaceholder, "Route");
        var builder = builderCS.AddReferences(AspNet4xReferences("5.2.7")).WithOptions(ParseOptionsHelper.CSharpLatest).AddSnippet($$"""
            using System.Web.Mvc;

            public class WithAllTypesOfStringsController : Controller
            {
                {{SlashAndBackslashConstants}}

                {{(compliant ? actionDeclaration : actionDeclaration + " // Noncompliant")}}
                public ActionResult Index() => View();
            }
            """);

        if (compliant)
        {
            builder.VerifyNoIssues();
        }
        else
        {
            builder.Verify();
        }
    }

    [TestMethod]
    [DynamicData(nameof(AspNet4xMvcVersionsUnderTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNet4x_VB(string aspNetMvcVersion) =>
        builderVB
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNet4x.vb")
            .AddReferences(AspNet4xReferences(aspNetMvcVersion))
            .Verify();
#endif

#if NET
    private static IEnumerable<object[]> HttpMethodAttributesWithAllTypesOfStrings =>
        AttributesWithAllTypesOfStrings.Zip(
            ["HttpGet", "HttpPost", "HttpPatch", "HttpHead", "HttpDelete", "HttpOptions", "HttpGet", "HttpPost", "HttpPatch", "HttpHead"],
            (attribute, httpMethod) => new object[] { ((string)attribute[0]).Replace(AttributePlaceholder, httpMethod), attribute[1], attribute[2] });

    public static IEnumerable<object[]> AspNetCore2xVersionsUnderTest =>
        [["2.0.4"] /* Latest 2.0.x */, ["2.2.0"] /* 2nd most used */, [Constants.NuGetLatestVersion]];

    private static IEnumerable<MetadataReference> AspNetCore2xReferences(string aspNetCoreVersion) =>
        NuGetMetadataReference.MicrosoftAspNetCoreMvcCore(aspNetCoreVersion)                       // For Controller
            .Concat(NuGetMetadataReference.MicrosoftAspNetCoreMvcViewFeatures(aspNetCoreVersion))  // For View
            .Concat(NuGetMetadataReference.MicrosoftAspNetCoreMvcAbstractions(aspNetCoreVersion)); // For IActionResult

    private static IEnumerable<MetadataReference> AspNetCore3AndAboveReferences =>
        [
            AspNetCoreMetadataReference.MicrosoftAspNetCore,                    // For WebApplication
            AspNetCoreMetadataReference.MicrosoftExtensionsHostingAbstractions, // For IHost
            AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpAbstractions,    // For HttpContext, RouteValueDictionary
            AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpFeatures,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcAbstractions,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcCore,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcRazorPages,       // For RazorPagesEndpointRouteBuilderExtensions.MapFallbackToPage
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcViewFeatures,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreRouting,             // For IEndpointRouteBuilder
        ];

    [TestMethod]
    [DynamicData(nameof(AspNetCore2xVersionsUnderTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore2x_CS(string aspNetCoreVersion) =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore2AndAbove.cs")
            .AddReferences(AspNetCore2xReferences(aspNetCoreVersion))
            .Verify();

    [TestMethod]
    [DynamicData(
        nameof(RouteAttributesWithAllTypesOfStrings),
        DynamicDataDisplayName = nameof(AttributesWithAllTypesOfStringsDisplayNameProvider),
        DynamicDataDisplayNameDeclaringType = typeof(BackslashShouldBeAvoidedInAspNetRoutesTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore2x_Route_CS_Latest(string actionDeclaration, bool compliant, string displayName) =>
        TestAspNetCoreAttributeDeclaration(
            builderCS.AddReferences(AspNetCore2xReferences("2.2.0")).WithOptions(ParseOptionsHelper.CSharpLatest),
            actionDeclaration,
            compliant);

    [TestMethod]
    [DynamicData(
        nameof(HttpMethodAttributesWithAllTypesOfStrings),
        DynamicDataDisplayName = nameof(AttributesWithAllTypesOfStringsDisplayNameProvider),
        DynamicDataDisplayNameDeclaringType = typeof(BackslashShouldBeAvoidedInAspNetRoutesTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore2x_HttpMethods_CS_Latest(string actionDeclaration, bool compliant, string displayName) =>
        TestAspNetCoreAttributeDeclaration(
            builderCS.AddReferences(AspNetCore2xReferences("2.2.0")).WithOptions(ParseOptionsHelper.CSharpLatest),
            actionDeclaration,
            compliant);

    [TestMethod]
    [DynamicData(
        nameof(RouteAttributesWithAllTypesOfStrings),
        DynamicDataDisplayName = nameof(AttributesWithAllTypesOfStringsDisplayNameProvider),
        DynamicDataDisplayNameDeclaringType = typeof(BackslashShouldBeAvoidedInAspNetRoutesTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore3AndAbove_Route_CS_Latest(string actionDeclaration, bool compliant, string displayName) =>
        TestAspNetCoreAttributeDeclaration(
            builderCS.AddReferences(AspNetCore3AndAboveReferences).WithOptions(ParseOptionsHelper.CSharpLatest),
            actionDeclaration,
            compliant);

    [TestMethod]
    [DynamicData(
        nameof(HttpMethodAttributesWithAllTypesOfStrings),
        DynamicDataDisplayName = nameof(AttributesWithAllTypesOfStringsDisplayNameProvider),
        DynamicDataDisplayNameDeclaringType = typeof(BackslashShouldBeAvoidedInAspNetRoutesTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore3AndAbove_HttpMethods_CS_Latest(string actionDeclaration, bool compliant, string displayName) =>
        TestAspNetCoreAttributeDeclaration(
            builderCS.AddReferences(AspNetCore3AndAboveReferences).WithOptions(ParseOptionsHelper.CSharpLatest),
            actionDeclaration,
            compliant);

    [TestMethod]
    [DynamicData(nameof(AspNetCore2xVersionsUnderTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore2x_VB(string aspNetCoreVersion) =>
        builderVB
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore2x.vb")
            .AddReferences(AspNetCore2xReferences(aspNetCoreVersion))
            .Verify();

    [TestMethod]
    [DynamicData(nameof(AspNetCore2xVersionsUnderTest))]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore2x_CS_Latest(string aspNetCoreVersion) =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore2x.Latest.cs")
            .AddReferences(AspNetCore2xReferences(aspNetCoreVersion))
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();

    [TestMethod]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore3AndAbove_CS() =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore2AndAbove.cs")
            .AddReferences(AspNetCore3AndAboveReferences)
            .Verify();

    [TestMethod]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore3AndAbove_CS_Latest() =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore3AndAbove.Latest.cs")
            .AddReferences(AspNetCore3AndAboveReferences)
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();

    [TestMethod]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore3AndAbove_VB() =>
        builderVB
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore3AndAbove.vb")
            .AddReferences(AspNetCore3AndAboveReferences)
            .Verify();

    [TestMethod]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore8AndAbove_CS_Latest() =>
        builderCS
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore8AndAbove.Latest.cs")
            .AddReferences(AspNetCore3AndAboveReferences)
            .WithOptions(ParseOptionsHelper.CSharpLatest)
            .Verify();

    [TestMethod]
    public void BackslashShouldBeAvoidedInAspNetRoutes_AspNetCore8AndAbove_VB() =>
        builderVB
            .AddPaths("BackslashShouldBeAvoidedInAspNetRoutes.AspNetCore8AndAbove.vb")
            .AddReferences(AspNetCore3AndAboveReferences)
            .Verify();

    private static void TestAspNetCoreAttributeDeclaration(VerifierBuilder builder, string attributeDeclaration, bool compliant)
    {
        builder = builder.AddSnippet($$"""
            using Microsoft.AspNetCore.Mvc;

            public class WithAllTypesOfStringsController : Controller
            {
                {{SlashAndBackslashConstants}}

                {{(compliant ? attributeDeclaration : attributeDeclaration + " // Noncompliant")}}
                public IActionResult Index() => View();
            }
            """);

        if (compliant)
        {
            builder.VerifyNoIssues();
        }
        else
        {
            builder.Verify();
        }
    }
#endif
}
