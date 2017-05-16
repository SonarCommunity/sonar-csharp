/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarAnalyzer.Helpers
{
    internal static class ProjectTypeHelper
    {
        public static bool IsTest(this SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.Compilation.IsTest();
        }

        public static bool IsTest(this Compilation compilation)
        {
            var assemblyName = string.Empty;
            if (compilation.AssemblyName != null)
            {
                assemblyName = compilation.AssemblyName;
            }

            return
                assemblyName.ToUpperInvariant().Contains(TestAssemblyNamePattern) ||
                compilation.ReferencedAssemblyNames
                    .Any(assembly => TestAssemblyNames.Contains(assembly.Name.ToUpperInvariant()));
        }

        private const string TestAssemblyNamePattern = "TEST";

        private static readonly ISet<string> TestAssemblyNames = ImmutableHashSet.Create(
            "MICROSOFT.VISUALSTUDIO.QUALITYTOOLS.UNITTESTFRAMEWORK",
            "XUNIT.CORE",
            "NUNIT.FRAMEWORK");
    }
}