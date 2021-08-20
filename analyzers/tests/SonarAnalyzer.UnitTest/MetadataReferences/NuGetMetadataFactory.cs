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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using NuGet.Packaging;

namespace SonarAnalyzer.UnitTest.MetadataReferences
{
    public static partial class NuGetMetadataFactory
    {
        private const string NugetConfigFolderRelativePath = @"..\..\..";
        private const string PackagesFolderRelativePath = @"..\..\..\..\..\packages\";

        // We use the global nuget cache for storing our packages if the NUGET_PACKAGES environment variable is defined.
        // This is especially helpful on the build agents where the packages are precached
        // (since we don't need to spawn a new process for calling the nuget.exe to install or copy them from global cache)
        private static readonly string PackagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? PackagesFolderRelativePath;

        private static readonly string[] SortedAllowedDirectories =
        {
            "net",
            "netstandard2.1",
            "netstandard2.0",
            "net47",
            "net461",
            "netstandard1.6",
            "netstandard1.3",
            "netstandard1.1",
            "netstandard1.0",
            "net451",
            "net45",
            "net40",
            "net20",
            "portable-net45",
            "lib", // This has to be last, some packages have DLLs directly in "lib" directory
        };

        /// <param name="dllDirectory">Name of the directory containing DLL files inside *.nupgk/lib/{dllDirectory}/ or *.nupgk/runtimes/{runtime}/lib/{dllDirectory}/ folder.
        /// This directory name represents target framework in most cases.</param>
        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime, string dllDirectory) =>
            Create(new Package(packageId, packageVersion, runtime), new[] { dllDirectory });

        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime = null) =>
            Create(new Package(packageId, packageVersion, runtime), SortedAllowedDirectories);

        /// <param name="allowedDirectories">List of allowed directories sorted by preference to search for DLL files.</param>
        private static IEnumerable<MetadataReference> Create(Package package, string[] allowedDirectories)
        {
            package.EnsurePackageIsInstalled();
            var dllsPerDirectory = new Dictionary<string, IEnumerable<string>>();
            var packageLibDirectory = package.PackageDirectory();
            var foldersWithDlls = new List<string> { Path.Combine(packageLibDirectory, "lib"), Path.Combine(packageLibDirectory, "ref") };
            foreach (var folder in foldersWithDlls.Where(Directory.Exists))
            {
                dllsPerDirectory.AddRange(DllsPerDirectory(folder).Where(x => !dllsPerDirectory.ContainsKey(x.Key)));
            }

            // if this throws because it doesn't find a DLL (because the lib contains "_._"), maybe you are referencing the wrong DLL (check the dependencies of the DLL you use)
            var directory = allowedDirectories.FirstOrDefault(x => dllsPerDirectory.ContainsKey(x))
                            ?? throw new InvalidOperationException($"No allowed directory with DLL files was found in {packageLibDirectory}. " +
                                                                   "Add new target framework to SortedAllowedDirectories or set targetFramework argument explicitly.");
            var dlls = dllsPerDirectory[directory].ToList();
            foreach (var filePath in dlls)
            {
                Console.WriteLine($"File: {filePath}");
            }

            return dlls.Select(x => MetadataReference.CreateFromFile(x)).ToArray();

            static Dictionary<string, IEnumerable<string>> DllsPerDirectory(string s)
            {
                return Directory.GetFiles(s, "*.dll", SearchOption.AllDirectories)
                                .GroupBy(x => new FileInfo(x).Directory.Name)
                                .ToDictionary(x => x.Key.Split('+').First(), x => x.AsEnumerable());
            }
        }
    }
}
