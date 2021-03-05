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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonarAnalyzer.Helpers
{
    public class FilesToAnalyzeStorage
    {
        private readonly Lazy<IEnumerable<string>> filestoAnalyze;

        [ExcludeFromCodeCoverage]
        public FilesToAnalyzeStorage(string filePath) : this(filePath, new FilesToAnalyzeRetriever()) { }

        internal FilesToAnalyzeStorage(string filePath, IFilesToAnalyzeRetriever filesToAnalyzeRetriever)
        {
            filestoAnalyze = new Lazy<IEnumerable<string>>(() => filesToAnalyzeRetriever.RetrieveFilesToAnalyze(filePath));
        }

        public IEnumerable<string> FilesToAnalyze(string fileName) =>
            filestoAnalyze.Value.Where(x => Path.GetFileName(x).IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0);

        public IEnumerable<string> FilesToAnalyze(Regex fileFilter) =>
            filestoAnalyze.Value.Where(x => fileFilter.IsMatch(x));
    }
}
