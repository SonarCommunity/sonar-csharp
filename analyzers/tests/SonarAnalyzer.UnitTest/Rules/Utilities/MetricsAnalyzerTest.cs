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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MetricsAnalyzerTest
    {
        private const string Root = @"TestCases\Utilities\MetricsAnalyzer\";

        [DataTestMethod]
        public void VerifyMetrics()
        {
            const string testRoot = Root + nameof(VerifyMetrics);

            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<MetricsInfo>(
                new[] { Root + "AllMetrics.cs" },
                new TestMetricsAnalyzer(testRoot, false),
                @$"{testRoot}\metrics.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                messages =>
                {
                    messages.Should().HaveCount(1);
                    var metrics = messages.Single();
                    metrics.FilePath.Should().Be("AllMetrics.cs");
                    metrics.ClassCount.Should().Be(4);
                    metrics.CodeLine.Should().HaveCount(24);
                    metrics.CognitiveComplexity.Should().Be(1);
                    metrics.Complexity.Should().Be(2);
                    metrics.ExecutableLines.Should().HaveCount(5);
                    metrics.FunctionCount.Should().Be(1);
                    metrics.NoSonarComment.Should().HaveCount(1);
                    metrics.NonBlankComment.Should().HaveCount(1);
                    metrics.StatementCount.Should().Be(5);
                },
                ParseOptionsHelper.CSharpLatest);
        }

        [TestMethod]
        public void Verify_NotRunForTestProject()
        {
            const string testRoot = Root + nameof(Verify_NotRunForTestProject);
            OldVerifier.VerifyUtilityAnalyzerIsNotRun(
                new[] { Root + "AllMetrics.cs" },
                new TestMetricsAnalyzer(testRoot, true),
                @$"{testRoot}\metrics.pb",
                ParseOptionsHelper.CSharpLatest);
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private class TestMetricsAnalyzer : MetricsAnalyzer
        {
            public TestMetricsAnalyzer(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }
    }
}
