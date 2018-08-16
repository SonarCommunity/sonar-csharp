﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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

using System.IO;
using Microsoft.CodeAnalysis;

namespace SonarAnalyzer.UnitTest
{
    internal static class FrameworkMetadataReference
    {
        #region Helpers

        private static readonly string systemAssembliesFolder =
            new FileInfo(typeof(object).Assembly.Location).Directory.FullName;

        private static MetadataReference Create(string assemblyName) =>
            MetadataReference.CreateFromFile(Path.Combine(systemAssembliesFolder, assemblyName));

        #endregion Helpers

        internal static MetadataReference MicrosoftVisualBasic { get; }
            = Create("Microsoft.VisualBasic.dll");

        internal static MetadataReference Mscorlib { get; }
            = Create("mscorlib.dll");

        internal static MetadataReference System { get; }
            = Create("System.dll");

        internal static MetadataReference SystemComponentModelComposition { get; }
            = Create("System.ComponentModel.Composition.dll");

        internal static MetadataReference SystemCore { get; }
            = Create("System.Core.dll");

        internal static MetadataReference SystemData { get; }
            = Create("System.Data.dll");

        internal static MetadataReference SystemDirectoryServices { get; }
            = Create("System.DirectoryServices.dll");

        internal static MetadataReference SystemRuntimeSerialization { get; }
            = Create("System.Runtime.Serialization.dll");

        internal static MetadataReference SystemServiceModel { get; }
            = Create("System.ServiceModel.dll");

        internal static MetadataReference SystemWeb { get; }
            = Create("System.Web.dll");

        internal static MetadataReference SystemWindowsForms { get; }
            = Create("System.Windows.Forms.dll");

        internal static MetadataReference SystemXaml { get; }
            = Create("System.Xaml.dll");

        internal static MetadataReference SystemXml { get; }
            = Create("System.Xml.dll");
    }
}
