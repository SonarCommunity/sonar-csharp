﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

using System.Text.RegularExpressions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis.Text;

namespace SonarAnalyzer.TestFramework.Verification.IssueValidation
{
    /// <summary>
    /// See <see href="https://github.com/SonarSource/sonar-dotnet/blob/master/docs/verifier-syntax.md">docs/verifier-syntax.md</see>
    /// for a comprehensive documentation of the verifier syntax.
    /// </summary>
    internal static class IssueLocationCollector
    {
        private const string CommentPattern = @"(?<comment>//|'|<!--|/\*|@\*)";
        private const string PrecisePositionPattern = @"\s*(?<position>\^+)(\s+(?<invalid>\^+))*";
        private const string NoPrecisePositionPattern = @"(?<!\s*\^+\s)";
        private const string IssueTypePattern = @"\s*(?<issueType>Noncompliant|Secondary)";
        private const string ErrorTypePattern = @"\s*Error";
        private const string OffsetPattern = @"(\s*@(?<offset>[+-]?\d+))?";
        private const string ExactColumnPattern = @"(\s*\^(?<columnStart>\d+)#(?<length>\d+))?";
        private const string IssueIdsPattern = @"(\s*\[(?<issueIds>[^]]+)\])?";
        private const string MessagePattern = @"(\s*\{\{(?<message>.+)\}\})?";

        public static readonly Regex RxIssue =
            CreateRegex(CommentPattern + NoPrecisePositionPattern + IssueTypePattern + OffsetPattern + ExactColumnPattern + IssueIdsPattern + MessagePattern);

        public static readonly Regex RxPreciseLocation =
            CreateRegex(@"^\s*" + CommentPattern + PrecisePositionPattern + IssueTypePattern + "?" + OffsetPattern + IssueIdsPattern + MessagePattern + @"\s*(-->|\*/|\*@)?$");

        private static readonly Regex RxBuildError = CreateRegex(CommentPattern + ErrorTypePattern + OffsetPattern + ExactColumnPattern + IssueIdsPattern);
        private static readonly Regex RxInvalidType = CreateRegex(CommentPattern + ".*" + IssueTypePattern);
        private static readonly Regex RxInvalidPreciseLocation = CreateRegex(@"^\s*" + CommentPattern + ".*" + PrecisePositionPattern);

        public static IList<IssueLocation> GetExpectedIssueLocations(IEnumerable<TextLine> lines)
        {
            var preciseLocations = new List<IssueLocation>();
            var locations = new List<IssueLocation>();

            foreach (var line in lines)
            {
                var newPreciseLocations = GetPreciseIssueLocations(line).ToList();
                if (newPreciseLocations.Any())
                {
                    preciseLocations.AddRange(newPreciseLocations);
                }
                else if (GetIssueLocations(line).ToList() is var newLocations && newLocations.Any())
                {
                    locations.AddRange(newLocations);
                }
                else
                {
                    EnsureNoInvalidFormat(line);
                }
            }

            return EnsureNoDuplicatedPrimaryIds(MergeLocations(locations.ToArray(), preciseLocations.ToArray()));
        }

        public static IEnumerable<IssueLocation> GetExpectedBuildErrors(IEnumerable<TextLine> lines) =>
            lines?.SelectMany(GetBuildErrorsLocations) ?? Enumerable.Empty<IssueLocation>();

        public static IList<IssueLocation> MergeLocations(IssueLocation[] locations, IssueLocation[] preciseLocations)
        {
            var usedLocations = new List<IssueLocation>();
            foreach (var location in locations)
            {
                var preciseLocationsOnSameLine = preciseLocations.Where(l => l.LineNumber == location.LineNumber).ToList();
                if (preciseLocationsOnSameLine.Count > 1)
                {
                    ThrowUnexpectedPreciseLocationCount(preciseLocationsOnSameLine.Count, preciseLocationsOnSameLine[0].LineNumber);
                }

                if (preciseLocationsOnSameLine.SingleOrDefault() is { } preciseLocation)
                {
                    if (location.Start.HasValue)
                    {
                        throw new InvalidOperationException($"Unexpected redundant issue location on line {location.LineNumber}. Issue location can " +
                                                            "be set either with 'precise issue location' or 'exact column location' pattern but not both.");
                    }

                    location.Start = preciseLocation.Start;
                    location.Length = preciseLocation.Length;
                    usedLocations.Add(preciseLocation);
                }
            }

            return locations
                   .Union(preciseLocations.Except(usedLocations))
                   .ToList();
        }

        public static /*for testing*/ IEnumerable<IssueLocation> GetIssueLocations(TextLine line) =>
            GetLocations(line, RxIssue);

        public static /*for testing*/ IEnumerable<IssueLocation> GetPreciseIssueLocations(TextLine line)
        {
            var match = RxPreciseLocation.Match(line.ToString());
            if (match.Success)
            {
                EnsureNoRemainingCurlyBrace(line, match);
                return CreateIssueLocations(match, line.LineNumber);
            }

            return Enumerable.Empty<IssueLocation>();
        }

        private static IEnumerable<IssueLocation> GetBuildErrorsLocations(TextLine line) =>
            GetLocations(line, RxBuildError);

        private static IEnumerable<IssueLocation> GetLocations(TextLine line, Regex rx)
        {
            var match = rx.Match(line.ToString());
            if (match.Success)
            {
                EnsureNoRemainingCurlyBrace(line, match);
                return CreateIssueLocations(match, line.LineNumber + 1);
            }

            return Enumerable.Empty<IssueLocation>();
        }

        private static IEnumerable<IssueLocation> CreateIssueLocations(Match match, int lineNumber)
        {
            var line = lineNumber + GetOffset(match);
            var isPrimary = GetIsPrimary(match);
            var message = GetMessage(match);
            var start = GetStart(match) ?? GetColumnStart(match);
            var length = GetLength(match) ?? GetColumnLength(match);

            var invalid = match.Groups["invalid"];
            if (invalid.Success)
            {
                ThrowUnexpectedPreciseLocationCount(invalid.Captures.Count + 1, line);
            }

            return GetIssueIds(match).Select(
                issueId => new IssueLocation
                {
                    IsPrimary = isPrimary,
                    LineNumber = line,
                    Message = message,
                    IssueId = issueId,
                    Start = start,
                    Length = length,
                });
        }

        private static int? GetStart(Match match) =>
            match.Groups["position"] is { Success: true } position ? position.Index : null;

        private static int? GetLength(Match match) =>
            match.Groups["position"] is { Success: true } position ? position.Length : null;

        private static int? GetColumnStart(Match match) =>
            match.Groups["columnStart"] is { Success: true } columnStart ? (int?)int.Parse(columnStart.Value) - 1 : null;

        private static int? GetColumnLength(Match match) =>
            match.Groups["length"] is { Success: true } length ? int.Parse(length.Value) : null;

        private static bool GetIsPrimary(Match match) =>
            match.Groups["issueType"] is var issueType
            && (!issueType.Success || issueType.Value == "Noncompliant");

        private static string GetMessage(Match match) =>
            match.Groups["message"] is { Success: true } message ? message.Value : null;

        private static int GetOffset(Match match) =>
            match.Groups["offset"] is { Success: true } offset ? int.Parse(offset.Value) : 0;

        private static IEnumerable<string> GetIssueIds(Match match)
        {
            var issueIds = match.Groups["issueIds"];
            if (!issueIds.Success)
            {
                // We have a single issue without ID even if the group did not match
                return new string[] { null };
            }

            return issueIds.Value
                           .Split(',')
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .OrderBy(s => s);
        }

        private static void EnsureNoInvalidFormat(TextLine line)
        {
            var lineText = line.ToString();
            if (RxInvalidType.IsMatch(lineText) || RxInvalidPreciseLocation.IsMatch(lineText))
            {
                throw new InvalidOperationException($@"Line {line.LineNumber} looks like it contains comment for noncompliant code, but it is not recognized as one of the expected pattern.
Either remove the Noncompliant/Secondary word or precise pattern '^^' from the comment, or fix the pattern.");
            }
        }

        private static IList<IssueLocation> EnsureNoDuplicatedPrimaryIds(IList<IssueLocation> mergedLocations)
        {
            var duplicateLocationsIds = mergedLocations
                            .Where(x => x.IsPrimary && x.IssueId != null)
                            .GroupBy(x => x.IssueId)
                            .FirstOrDefault(group => group.Count() > 1);
            if (duplicateLocationsIds != null)
            {
                var duplicatedIdLines = duplicateLocationsIds.Select(issueLocation => issueLocation.LineNumber).JoinStr(", ");
                throw new InvalidOperationException($"Primary location with id [{duplicateLocationsIds.Key}] found on multiple lines: {duplicatedIdLines}");
            }
            return mergedLocations;
        }

        private static void EnsureNoRemainingCurlyBrace(TextLine line, Capture match)
        {
            var remainingLine = line.ToString().Substring(match.Index + match.Length);
            if (remainingLine.Contains('{') || remainingLine.Contains('}'))
            {
                Execute.Assertion.FailWith("Unexpected '{{' or '}}' found on line: {0}. Either correctly use the '{{{{message}}}}' " +
                    "format or remove the curly braces on the line of the expected issue", line.LineNumber);
            }
        }

        private static void ThrowUnexpectedPreciseLocationCount(int count, int line)
        {
            var message = $"Expecting only one precise location per line, found {count} on line {line}. " +
                @"If you want to specify more than one precise location per line you need to omit the Noncompliant comment:
internal class MyClass : IInterface1 // there should be no Noncompliant comment
^^^^^^^ {{Do not create internal classes.}}
                         ^^^^^^^^^^^ @-1 {{IInterface1 is bad for your health.}}";
            throw new InvalidOperationException(message);
        }

        private static Regex CreateRegex(string pattern) =>
            new Regex(pattern, RegexOptions.Compiled, RegexConstants.DefaultTimeout);
    }
}
