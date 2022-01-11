﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MarkAssemblyWithAssemblyVersionAttributeTest
    {
        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttribute_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttribute.cs", new CS.MarkAssemblyWithAssemblyVersionAttribute());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeRazor_CS() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttributeRazor.cs",
                new CS.MarkAssemblyWithAssemblyVersionAttribute(),
                NuGetMetadataReference.MicrosoftAspNetCoreMvcRazorRuntime());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttribute_CS_Concurrent() =>
            OldVerifier.VerifyAnalyzer(new[] { @"TestCases\MarkAssemblyWithAssemblyVersionAttribute.cs", @"TestCases\MarkAssemblyWithAssemblyVersionAttributeRazor.cs", },
                new CS.MarkAssemblyWithAssemblyVersionAttribute(),
                default,
                NuGetMetadataReference.MicrosoftAspNetCoreMvcRazorRuntime());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeNoncompliant_CS()
        {
            Action action = () => OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttributeNoncompliant.cs", new CS.MarkAssemblyWithAssemblyVersionAttribute());
            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage("*Provide an 'AssemblyVersion' attribute for assembly 'project0'.*");
        }

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeNoncompliant_NoTargets_ShouldNotRaise_CS()
        {
            Action action = () => OldVerifier.VerifyNonConcurrentAnalyzer(
                @"TestCases\MarkAssemblyWithAssemblyVersionAttributeNoncompliant.cs",
                new CS.MarkAssemblyWithAssemblyVersionAttribute(),
                NuGetMetadataReference.MicrosoftBuildNoTargets());

            // False positive. No assembly gets generated when Microsoft.Build.NoTargets is referenced.
            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage("*Provide an 'AssemblyVersion' attribute for assembly 'project0'.*");
        }

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttribute_VB() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttribute.vb", new VB.MarkAssemblyWithAssemblyVersionAttribute());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeRazor_VB() =>
            OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttributeRazor.vb",
                new VB.MarkAssemblyWithAssemblyVersionAttribute(),
                NuGetMetadataReference.MicrosoftAspNetCoreMvcRazorRuntime());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttribute_VB_Concurrent() =>
            OldVerifier.VerifyAnalyzer(new[] { @"TestCases\MarkAssemblyWithAssemblyVersionAttribute.vb", @"TestCases\MarkAssemblyWithAssemblyVersionAttributeRazor.vb", },
                new VB.MarkAssemblyWithAssemblyVersionAttribute(),
                default,
                NuGetMetadataReference.MicrosoftAspNetCoreMvcRazorRuntime());

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeNoncompliant_VB()
        {
            Action action = () => OldVerifier.VerifyNonConcurrentAnalyzer(@"TestCases\MarkAssemblyWithAssemblyVersionAttributeNoncompliant.vb", new VB.MarkAssemblyWithAssemblyVersionAttribute());
            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage("*Provide an 'AssemblyVersion' attribute for assembly 'project0'.*");
        }

        [TestMethod]
        public void MarkAssemblyWithAssemblyVersionAttributeNoncompliant_NoTargets_ShouldNotRaise_VB()
        {
            Action action = () => OldVerifier.VerifyNonConcurrentAnalyzer(
                @"TestCases\MarkAssemblyWithAssemblyVersionAttributeNoncompliant.vb",
                new VB.MarkAssemblyWithAssemblyVersionAttribute(),
                NuGetMetadataReference.MicrosoftBuildNoTargets());

            // False positive. No assembly gets generated when Microsoft.Build.NoTargets is referenced.
            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage("*Provide an 'AssemblyVersion' attribute for assembly 'project0'.*");
        }
    }
}
