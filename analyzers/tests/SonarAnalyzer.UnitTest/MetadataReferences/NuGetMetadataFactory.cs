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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NuGet;

namespace SonarAnalyzer.UnitTest.MetadataReferences
{
    public static partial class NuGetMetadataFactory
    {
        private const string PackagesFolderRelativePath = @"..\..\..\..\..\packages\";
        private const string NuGetConfigFileRelativePath = @"..\..\..\nuget.config";

        private static readonly PackageManager PackageManager = new PackageManager(CreatePackageRepository(), PackagesFolderRelativePath);

        private static readonly string[] SortedAllowedDirectories = new string[]
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

        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime, string targetFramework) =>
            Create(new Package(packageId, packageVersion, runtime), new[] { targetFramework });

        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime = null) =>
            Create(new Package(packageId, packageVersion, runtime), SortedAllowedDirectories);

        public static IEnumerable<MetadataReference> CreateNETStandard21()
        {
            var packageDir = Path.GetFullPath($@"{PackagesFolderRelativePath}NETStandard.Library.Ref.2.1.0\ref\netstandard2.1");
            if (Directory.Exists(packageDir))
            {
                LogMessage($"Package found at {packageDir}");
            }
            else
            {
                LogMessage($"Package not found at {packageDir}");
                PackageManager.InstallPackage("NETStandard.Library.Ref", SemanticVersion.ParseOptionalVersion("2.1.0"), ignoreDependencies: true, allowPrereleaseVersions: false);
                if (!Directory.Exists(packageDir))
                {
                    throw new ApplicationException($"Test setup error: folder for downloaded package does not exist. Folder: {packageDir}");
                }
            }

            return Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories)
               .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
               .ToImmutableArray();
        }

        private static IEnumerable<MetadataReference> Create(Package package, string[] allowedDirectories)
        {
            Console.WriteLine();
            Console.WriteLine($"Package: {package.Id}, {package.Version}");
            package.EnsurePackageIsInstalled();

            var packageDirectory = package.PackageDirectory();
            if (!Directory.Exists(packageDirectory))
            {
                throw new ApplicationException($"Test setup error: Package directory doesn't exist: {packageDirectory}");
            }
            var dllsPerDirectory = Directory.GetFiles(packageDirectory, "*.dll", SearchOption.AllDirectories)
                .GroupBy(x => new FileInfo(x).Directory.Name)
                .ToDictionary(x => x.Key.Split('+').First(), x => x.AsEnumerable());
            var dlls = dllsPerDirectory[allowedDirectories.First(x => dllsPerDirectory.ContainsKey(x))];
            foreach (var file in dlls)
            {
                Console.WriteLine($"File: {file}");
            }
            return dlls.Select(x => MetadataReference.CreateFromFile(x)).ToArray();
        }

        private static IPackageRepository CreatePackageRepository()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var localSettings = Settings.LoadDefaultSettings(new PhysicalFileSystem(currentFolder), null, null);
            // Get a package source provider that can use the settings
            var packageSourceProvider = new PackageSourceProvider(localSettings);
            // Create an aggregate repository that uses all of the configured sources
            return packageSourceProvider.CreateAggregateRepository(PackageRepositoryFactory.Default, true /* ignore failing repos. Errors will be logged as warnings. */ );
        }

        private static void LogMessage(string message) =>
             Console.WriteLine($"[{DateTime.Now}] Test setup: {message}");
    }
}
