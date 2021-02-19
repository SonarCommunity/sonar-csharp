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

        private static readonly string[] AllowedNuGetLibDirectoriesInOrderOfPreference = new string[]
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
                "lib",
            };

        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime, string targetFramework) =>
            Create(new Package(packageId, packageVersion, runtime), new[] { targetFramework });

        public static IEnumerable<MetadataReference> Create(string packageId, string packageVersion, string runtime = null) =>
            Create(new Package(packageId, packageVersion, runtime), AllowedNuGetLibDirectoriesInOrderOfPreference);

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

        private static IEnumerable<MetadataReference> Create(Package package, string[] allowedTargetFrameworks)
        {
            package.EnsurePackageIsInstalled();

            var allowedNuGetLibDirectoriesByPreference = allowedTargetFrameworks.Select((folder, priority) => new { folder, priority });
            var packageDirectory = package.PackageDirectory();
            LogMessage($"Download package directory: {packageDirectory}");
            if (!Directory.Exists(packageDirectory))
            {
                throw new ApplicationException($"Test setup error: folder for downloaded package does not exist. Folder: {packageDirectory}");
            }

            var matchingDllsGroups = Directory.GetFiles(packageDirectory, "*.dll", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .GroupBy(file => file.Directory.Name).ToArray();
            IGrouping<string, FileInfo> selectedGroup;

            selectedGroup = matchingDllsGroups.Length == 1 && matchingDllsGroups[0].Key.EndsWith(".dll")
                ? matchingDllsGroups[0]
                : matchingDllsGroups.Join(allowedNuGetLibDirectoriesByPreference,
                    group => group.Key.Split('+').First(),
                    allowed => allowed.folder,
                    (group, allowed) => new { group, allowed.priority })
                .OrderBy(merged => merged.priority)
                .First()
                .group;

            DumpSelectedGroup(package, selectedGroup);

            return selectedGroup.Select(file => MetadataReference.CreateFromFile(file.FullName)).ToImmutableArray();
        }

        private static void DumpSelectedGroup(Package package, IGrouping<string, FileInfo> fileGroup)
        {
            Console.WriteLine();
            Console.WriteLine($"Package: {package.Id}");
            Console.WriteLine($"Version: {package.Version}, chosen targetFramework: {fileGroup.Key}");
            foreach (var file in fileGroup)
            {
                Console.WriteLine($"File: {file.FullName}");
            }
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
