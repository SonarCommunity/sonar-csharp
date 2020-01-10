/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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

using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace SonarAnalyzer.UnitTest.TestFramework.IssueLocationCollectorTests
{
    [TestClass]
    public class IssueLocationCollector_GetIssueLocations
    {
        private TextLine GetLine(int lineNumber, string code)
        {
            var sourceText = SourceText.From(code);
            return sourceText.Lines[lineNumber];
        }

        private void VerifyIssueLocations(IEnumerable<IIssueLocation> result,
            IEnumerable<bool> expectedIsPrimary, 
            IEnumerable<int> expectedLineNumbers, 
            IEnumerable<string> expectedMessages, 
            IEnumerable<string> expectedIssueIds)
        {
            result.Select(l => l.IsPrimary).Should().Equal(expectedIsPrimary);
            result.Select(l => l.LineNumber).Should().Equal(expectedLineNumbers);
            result.Select(l => l.Message).Should().Equal(expectedMessages);
            result.Select(l => l.IssueId).Should().Equal(expectedIssueIds);
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Two_Flows()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant [flow1,flow2]
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true },
                expectedLineNumbers: new[] { 3, 3 },
                expectedMessages: new string[] { null, null },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Offset_Message_And_Flows()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant@-1 [flow1,flow2] {{Some message}}
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true },
                expectedLineNumbers: new[] { 2, 2 },
                expectedMessages: new string[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Reversed_Message_And_Flows()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant {{Some message}} [flow1,flow2]
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { "Some message" },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Offset()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant@-1
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 2 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Message_And_Flows()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant [flow1,flow2] {{Some message}}
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true },
                expectedLineNumbers: new[] { 3, 3 },
                expectedMessages: new string[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Message()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant {{Some message}}
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { "Some message" },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Invalid_Offset()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant@=1
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_With_Flow()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant [last,flow1,flow2]
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().HaveCount(3);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true, true },
                expectedLineNumbers: new[] { 3, 3, 3 },
                expectedMessages: new string[] { null, null, null },
                expectedIssueIds: new[] { "flow1", "flow2", "last" });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Flow_With_Offset_Message_And_Flows()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Secondary@-1 [flow1,flow2] {{Some message}}
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false, false },
                expectedLineNumbers: new[] { 2, 2 },
                expectedMessages: new string[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetIssueLocations_NoComment()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a);
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_ExactColumn()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant^5#7
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetIssueLocations_Secondary_ExactColumn_Ids()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Secondary ^13#9 [myId]
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { "myId" });
        }

        [TestMethod]
        public void GetIssueLocations_Noncompliant_Offset_ExactColumn_Message_Whitespaces()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant @-2 ^5#16 [myIssueId] {{MyMessage}}               
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 1 },
                expectedMessages: new string[] { "MyMessage" },
                expectedIssueIds: new string[] { "myIssueId" });
            result.Select(issue => issue.Start).Should().Equal(new[] { 4 });
            result.Select(issue => issue.Length).Should().Equal(new[] { 16 });
        }


        [TestMethod]
        public void GetIssueLocations_Noncompliant_Offset_ExactColumn_Message_NoWhitespace()
        {
            var line = GetLine(2, @"if (a > b)
{
    Console.WriteLine(a); //Noncompliant@-2^5#16[myIssueId]{{MyMessage}}        
}");
            var result = new IssueLocationCollector().GetIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 1 },
                expectedMessages: new string[] { "MyMessage" },
                expectedIssueIds: new string[] { "myIssueId" });
            result.Select(issue => issue.Start).Should().Equal(new[] { 4 });
            result.Select(issue => issue.Length).Should().Equal(new[] { 16 });
        }
    }
}
