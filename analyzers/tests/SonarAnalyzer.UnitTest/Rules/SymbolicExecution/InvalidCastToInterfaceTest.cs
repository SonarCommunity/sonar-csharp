﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.Rules.SymbolicExecution;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules.SymbolicExecution
{
    [TestClass]
    public class InvalidCastToInterfaceTest
    {
        private static readonly DiagnosticDescriptor[] OnlyDiagnostics = new[] { InvalidCastToInterfaceSymbolicExecution.S1944 };

        [DataTestMethod]
        [DataRow(ProjectType.Product)]
        [DataRow(ProjectType.Test)]
        public void InvalidCastToInterface(ProjectType projectType) =>
            OldVerifier.VerifyAnalyzer(
                @"TestCases\SymbolicExecution\Sonar\InvalidCastToInterface.cs",
                Analyzers(),
                additionalReferences: TestHelper.ProjectTypeReference(projectType).Concat(MetadataReferenceFacade.NETStandard21),
                options: ParseOptionsHelper.FromCSharp8,
                onlyDiagnostics: OnlyDiagnostics);

#if NET
        [TestMethod]
        public void InvalidCastToInterface_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(
                @"TestCases\SymbolicExecution\Sonar\InvalidCastToInterface.CSharp9.cs",
                Analyzers(),
                onlyDiagnostics: OnlyDiagnostics);

        [TestMethod]
        public void InvalidCastToInterface_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(
                @"TestCases\SymbolicExecution\Sonar\InvalidCastToInterface.CSharp10.cs",
                Analyzers(),
                onlyDiagnostics: OnlyDiagnostics);
#endif

        private static DiagnosticAnalyzer[] Analyzers() =>
            new DiagnosticAnalyzer[] { new SymbolicExecutionRunner(), new InvalidCastToInterface() };
    }
}
