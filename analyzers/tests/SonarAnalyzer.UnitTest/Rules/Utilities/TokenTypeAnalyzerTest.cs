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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class TokenTypeAnalyzerTest
    {
        private const string Root = @"TestCases\Utilities\TokenTypeAnalyzer\";

        public TestContext TestContext { get; set; } // Set automatically by MsTest

        [TestMethod]
        [TestCategory("Rule")]
        public void Verify_MainTokens() =>
            Verify("Tokens.cs", info =>
            {
                info.Should().HaveCount(11);
                info.Where(x => x.TokenType == TokenType.Keyword).Should().HaveCount(8);
                info.Should().ContainSingle(x => x.TokenType == TokenType.TypeName);
                info.Should().ContainSingle(x => x.TokenType == TokenType.StringLiteral);
                info.Should().ContainSingle(x => x.TokenType == TokenType.NumericLiteral);
            });

        [TestMethod]
        [TestCategory("Rule")]
        public void Verify_Identifiers() =>
            Verify("Identifiers.cs", info =>
            {
                info.Should().HaveCount(30);
                info.Where(x => x.TokenType == TokenType.Keyword).Should().HaveCount(21);
                info.Where(x => x.TokenType == TokenType.TypeName).Should().HaveCount(7);
                info.Should().ContainSingle(x => x.TokenType == TokenType.NumericLiteral);
                info.Should().ContainSingle(x => x.TokenType == TokenType.Comment);
            });

        [TestMethod]
        [TestCategory("Rule")]
        public void Verify_Trivia() =>
            Verify("Trivia.cs", info =>
            {
                info.Should().HaveCount(5);
                info.Where(x => x.TokenType == TokenType.Comment).Should().HaveCount(4);
                info.Should().ContainSingle(x => x.TokenType == TokenType.Keyword);
            });

        public void Verify(string fileName, Action<IEnumerable<TokenTypeInfo.Types.TokenInfo>> verifyTokenInfo)
        {
            var testRoot = Root + TestContext.TestName;
            Verifier.VerifyUtilityAnalyzer<TokenTypeInfo>(
                new[] { Root + fileName },
                new TestTokenTypeAnalyzer
                {
                    IsEnabled = true,
                    WorkingPath = testRoot,
                },
                @$"{testRoot}\token-type.pb",
                messages =>
                {
                    messages.Should().HaveCount(1);
                    var info = messages.Single();
                    info.FilePath.Should().Be(fileName);
                    verifyTokenInfo(info.TokenInfo);
                });
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private class TestTokenTypeAnalyzer : TokenTypeAnalyzer
        {
            public bool IsEnabled
            {
                get => IsAnalyzerEnabled;
                set => IsAnalyzerEnabled = value;
            }

            public string WorkingPath
            {
                get => OutPath;
                set => OutPath = value;
            }
        }
    }
}
