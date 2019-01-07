﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2019 SonarSource SA
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
extern alias vbnet;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using CSharp = csharp::SonarAnalyzer.Rules.CSharp;
using VisualBasic = vbnet::SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class HandlingFilesTest
    {
        private static readonly IEnumerable<MetadataReference> AdditionalReferences =
            Enumerable.Empty<MetadataReference>()
                .Concat(FrameworkMetadataReference.SystemIOCompression)
                .Concat(FrameworkMetadataReference.SystemIOCompressionFileSystem);

        [TestMethod]
        [TestCategory("Rule")]
        public void HandlingFiles_CS()
        {
            Verifier.VerifyAnalyzer(@"TestCases\HandlingFiles.cs",
                new CSharp.HandlingFiles(AnalyzerConfiguration.AlwaysEnabled),
                additionalReferences: AdditionalReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void HandlingFiles_CS_Disabled()
        {
            Verifier.VerifyNoIssueReported(@"TestCases\HandlingFiles.cs",
                new CSharp.HandlingFiles(),
                additionalReferences: AdditionalReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void HandlingFiles_VB()
        {
            Verifier.VerifyAnalyzer(@"TestCases\HandlingFiles.vb",
                new VisualBasic.HandlingFiles(AnalyzerConfiguration.AlwaysEnabled),
                additionalReferences: AdditionalReferences);
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void HandlingFiles_VB_Disabled()
        {
            Verifier.VerifyNoIssueReported(@"TestCases\HandlingFiles.vb",
                new VisualBasic.HandlingFiles(),
                additionalReferences: AdditionalReferences);
        }
    }
}
