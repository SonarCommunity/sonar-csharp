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

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SonarAnalyzer.SourceGenerator
{
    [Generator]
    public class RuleCatalogGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Not needed
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                throw new NotSupportedException("Cannot find ProjectDir");
            }
            var language = context.Compilation.Language switch
            {
                LanguageNames.CSharp => "cs",
                LanguageNames.VisualBasic => "vbnet",
                _ => throw new ArgumentException($"Unexpected language: {context.Compilation.Language}")
            };


            // FIXME: REMOVE DEBUG, just make sure that we can use it
            Newtonsoft.Json.Linq.JObject.Parse("{}");


            context.AddSource("RuleCatalog", GenerateSource(Path.Combine(projectDir, "..", "..", "rspec", language)));
        }

        private static string GenerateSource(string rspecDirectory)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
@"// <auto-generated/>

using System.Collections.Generic;

namespace SonarAnalyzer.Helpers
{
    internal static class RuleCatalog
    {
        public static Dictionary<string, RuleDescriptor> Rules { get; } = new()
        {");
            foreach (var jsonPath in Directory.GetFiles(rspecDirectory, "*.json"))
            {
                var id = Path.GetFileName(jsonPath).Split('_')[0];
                if (id != "Sonar")  // Avoid "Sonar_way_profile.json"
                {
                    var data = Json.Parse(File.ReadAllText(jsonPath));
                    var description = File.ReadAllText(Path.ChangeExtension(jsonPath, ".html"));
                    // FIXME: Read and parse Sonar_way_profile.json
                    sb.AppendLine($@"{{ ""{id}"", new(""{id}"", {Encode(data["title"])}, ""{data["type"]}"", ""{data["defaultSeverity"]}"", SourceScope.{data["scope"]}, true, {Encode(description)}) }},");
                }
            }
            sb.AppendLine(
@"      };
    }
}");
            return sb.ToString();
        }

        private static string Encode(string value) =>
            $@"@""{value.Replace(@"""", @"""""")}""";
    }
}
