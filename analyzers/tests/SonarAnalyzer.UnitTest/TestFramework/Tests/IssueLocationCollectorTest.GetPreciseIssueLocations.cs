﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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

namespace SonarAnalyzer.UnitTest.TestFramework.Tests
{
    public partial class IssueLocationCollectorTest
    {
        [TestMethod]
        public void GetPreciseIssueLocations_NoMessage_NoIds()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_With_Offset()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ @-1
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 2 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_NoMessage_NoIds_Secondary()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ Secondary
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Secondary_With_Offset()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ Secondary@-1
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false },
                expectedLineNumbers: new[] { 2 },
                expectedMessages: new string[] { null },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_IssueIds()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ [flow1,flow2]
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true },
                expectedLineNumbers: new[] { 3, 3 },
                expectedMessages: new string[] { null, null },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_IssueIds_Secondary()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ Secondary [last1,flow1,flow2]
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(3);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false, false, false },
                expectedLineNumbers: new[] { 3, 3, 3 },
                expectedMessages: new string[] { null, null, null },
                expectedIssueIds: new[] { "flow1", "flow2", "last1" });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message_And_IssueIds()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ [flow1,flow2] {{Some message}}
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true, true },
                expectedLineNumbers: new[] { 3, 3 },
                expectedMessages: new[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message_And_IssueIds_Secondary_CS()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ Secondary [flow1,flow2] {{Some message}}
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false, false },
                expectedLineNumbers: new[] { 3, 3 },
                expectedMessages: new[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message_And_IssueIds_Secondary_XML()
        {
            var line = GetLine(2, @"<Root>
            <Baaad />
<!--        ^^^^^^^^^ Secondary [flow1,flow2] {{Some message}}         -->
</Root>");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false, false },
                expectedLineNumbers: new[] { 2, 2 },
                expectedMessages: new[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ {{Some message}}
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { true },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new[] { "Some message" },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message_Secondary()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ Secondary {{Some message}}
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().ContainSingle();

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false },
                expectedLineNumbers: new[] { 3 },
                expectedMessages: new[] { "Some message" },
                expectedIssueIds: new string[] { null });
        }

        [TestMethod]
        public void GetPreciseIssueLocations_NoComment()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetPreciseIssueLocations_NotStartOfLineIsOk()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
    //      ^^^^^^^^^
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            var issueLocation = result.First();

            issueLocation.IsPrimary.Should().BeTrue();
            issueLocation.LineNumber.Should().Be(3);
            issueLocation.Start.Should().Be(12);
            issueLocation.Length.Should().Be(9);
        }

        [TestMethod]
        public void GetPreciseIssueLocations_InvalidPattern()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//          ^^^^^^^^^ SecondaryNoncompliantSecondary {{Some message}}
}");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetPreciseIssueLocations_MultiplePatternsOnSameLine()
        {
            var line = GetLine(3, @"if (a > b)
{
    Console.WriteLine(a);
//  ^^^^^^^ ^^^^^^^^^ ^
}");
            Action action = () => IssueLocationCollector.GetPreciseIssueLocations(line);
            action.Should()
                  .Throw<InvalidOperationException>()
                  .WithMessage(@"Expecting only one precise location per line, found 3 on line 3. If you want to specify more than one precise location per line you need to omit the Noncompliant comment:
internal class MyClass : IInterface1 // there should be no Noncompliant comment
^^^^^^^ {{Do not create internal classes.}}
                         ^^^^^^^^^^^ @-1 {{IInterface1 is bad for your health.}}");
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Xml()
        {
            const string code = @"<Root>
<Space><SelfClosing /></Space>
<!--   ^^^^^^^^^^^^^^^ -->
<NoSpace><SelfClosing /></NoSpace>
     <!--^^^^^^^^^^^^^^^-->
<Multiline><SelfClosing /></Multiline>
<!--       ^^^^^^^^^^^^^^^
-->
</Root>";
            var line = GetLine(2, code);
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            var issueLocation = result.Single();
            issueLocation.Start.Should().Be(7);
            issueLocation.Length.Should().Be(15);

            line = GetLine(4, code);
            result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            issueLocation = result.Single();
            issueLocation.Start.Should().Be(9);
            issueLocation.Length.Should().Be(15);

            line = GetLine(6, code);
            result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            issueLocation = result.Single();
            issueLocation.Start.Should().Be(11);
            issueLocation.Length.Should().Be(15);
        }

        [TestMethod]
        public void GetPreciseIssueLocations_RazorWithSpaces()
        {
            const string code = @"
<p>With spaces: 42</p>
@*              ^^ *@";

            var line = GetLine(2, code);
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            var issueLocation = result.Single();
            issueLocation.Start.Should().Be(16);
            issueLocation.Length.Should().Be(2);
        }

        [TestMethod]
        public void GetPreciseIssueLocations_RazorWithoutSpaces()
        {
            const string code = @"
<p>Without spaces: 42</p>
                 @*^^*@";

            var line = GetLine(2, code);
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            var issueLocation = result.Single();
            issueLocation.Start.Should().Be(19);
            issueLocation.Length.Should().Be(2);
        }

        [TestMethod]
        public void GetPreciseIssueLocations_RazorWithMultiline()
        {
            const string code = @"
<p>Multiline: 42</p>
@*            ^^
*@
";
            var line = GetLine(2, code);
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();
            result.Should().ContainSingle();
            var issueLocation = result.Single();
            issueLocation.Start.Should().Be(14);
            issueLocation.Length.Should().Be(2);
        }

        [TestMethod]
        public void GetPreciseIssueLocations_Message_And_IssueIds_Secondary_Razor()
        {
            var line = GetLine(2, @"
            <p>The solution is: 42</p>
@*                              ^^ Secondary [flow1,flow2] {{Some message}}         *@");
            var result = IssueLocationCollector.GetPreciseIssueLocations(line).ToList();

            result.Should().HaveCount(2);

            VerifyIssueLocations(result,
                expectedIsPrimary: new[] { false, false },
                expectedLineNumbers: new[] { 2, 2 },
                expectedMessages: new[] { "Some message", "Some message" },
                expectedIssueIds: new[] { "flow1", "flow2" });
        }
    }
}
