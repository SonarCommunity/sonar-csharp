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

extern alias csharp;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.TestFramework;
using System.Collections.Generic;
using SonarAnalyzer.UnitTest.MetadataReferences;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;
using System.Linq;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class PropertiesAccessCorrectFieldTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void PropertiesAccessCorrectField_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\PropertiesAccessCorrectField.cs",
                                    new CS.PropertiesAccessCorrectField(),
                                    additionalReferences: AdditionalReferences);

        [TestMethod]
        [TestCategory("Rule")]
        public void PropertiesAccessCorrectField_CSharp8() =>
            Verifier.VerifyAnalyzer(@"TestCases\PropertiesAccessCorrectField.CSharp8.cs",
                                    new CS.PropertiesAccessCorrectField(),
                                    ParseOptionsHelper.FromCSharp8);

#if NET
        [TestMethod]
        [TestCategory("Rule")]
        public void PropertiesAccessCorrectField_CSharp9() =>
            Verifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\PropertiesAccessCorrectField.CSharp9.cs", new CS.PropertiesAccessCorrectField());
#endif

        [TestMethod]
        [TestCategory("Rule")]
        public void PropertiesAccessCorrectField_VB() =>
            Verifier.VerifyAnalyzer(@"TestCases\PropertiesAccessCorrectField.vb",
                                    new VB.PropertiesAccessCorrectField(),
                                    additionalReferences: AdditionalReferences);

        private static IEnumerable<MetadataReference> AdditionalReferences =>
            NuGetMetadataReference.MvvmLightLibs("5.4.1.1")
                .Concat(MetadataReferenceFacade.GetWindowsBase())
                .Concat(MetadataReferenceFacade.GetPresentationFramework());
    }
}

