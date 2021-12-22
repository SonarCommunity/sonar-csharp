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

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MarkWindowsFormsMainWithStaThreadTest
    {
        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_CS() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\MarkWindowsFormsMainWithStaThread.cs",
                new CS.MarkWindowsFormsMainWithStaThread(),
                default,
                CompilationErrorBehavior.Ignore,
                OutputKind.WindowsApplication,
                MetadataReferenceFacade.SystemWindowsForms);

        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_VB() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\MarkWindowsFormsMainWithStaThread.vb",
                new VB.MarkWindowsFormsMainWithStaThread(),
                default,
                CompilationErrorBehavior.Ignore,
                OutputKind.WindowsApplication,
                MetadataReferenceFacade.SystemWindowsForms);

        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_ClassLibrary_CS() =>
            OldVerifier.VerifyNoIssueReported(
                @"TestCases\MarkWindowsFormsMainWithStaThread.cs",
                new CS.MarkWindowsFormsMainWithStaThread(),
                null,
                CompilationErrorBehavior.Ignore,
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferenceFacade.SystemWindowsForms);

        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_ClassLibrary_VB() =>
            OldVerifier.VerifyNoIssueReported(
                @"TestCases\MarkWindowsFormsMainWithStaThread.vb",
                new VB.MarkWindowsFormsMainWithStaThread(),
                null,
                CompilationErrorBehavior.Ignore,
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferenceFacade.SystemWindowsForms);

        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_CS_NoWindowsForms() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\MarkWindowsFormsMainWithStaThread_NoWindowsForms.cs",
                new CS.MarkWindowsFormsMainWithStaThread(),
                default,
                CompilationErrorBehavior.Ignore,
                OutputKind.WindowsApplication);

        [TestMethod]
        public void MarkWindowsFormsMainWithStaThread_VB_NoWindowsForms() =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\MarkWindowsFormsMainWithStaThread_NoWindowsForms.vb",
                new VB.MarkWindowsFormsMainWithStaThread(),
                default,
                CompilationErrorBehavior.Ignore,
                OutputKind.WindowsApplication);
    }
}
