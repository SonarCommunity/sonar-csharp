﻿/*
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

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.ShimLayer.CSharp;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class NumberPatternShouldBeRegular : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3937";
        private const string MessageFormat = "Review this number; its irregular pattern indicates an error.";

        private const char Underscore = '_';
        private const char Dot = '.';
        private const int NotFound = -1;

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    if (!c.Compilation.IsAtLeastLanguageVersion(LanguageVersionEx.CSharp7))
                    {
                        return;
                    }

                    var literal = (LiteralExpressionSyntax)c.Node;

                    if (HasIrregularPattern(literal.Token.Text))
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(rule, literal.GetLocation()));
                    }
                },
                SyntaxKind.NumericLiteralExpression);
        }

        /// <remarks>internal for test purposes.</remarks>
        internal static bool HasIrregularPattern(string numericToken)
        {
            var splitted = StripNumericPreAndSuffix(numericToken).Split(Dot);

            // ignore multiple dots.
            if (splitted.Length > 2)
            {
                return false;
            }

            var groups = splitted[0].Split(Underscore).Select(s => s.Length).ToArray();

            var size = NotFound;

            if (groups.Length > 1)
            {
                size = groups[1];

                // first should not be bigger.
                if (groups[0] > size)
                {
                    return true;
                }

                // all should have the same size, except the first, and the size should at least be 2.
                if (size < 2 || groups.Skip(1).Any(g => g != size))
                {
                    return true;
                }
            }

            if (splitted.Length == 1)
            {
                return false;
            }

            var decimals = splitted[1].Split(Underscore).Select(s => s.Length).ToArray();

            if (decimals.Length == 1)
            {
                return false;
            }

            size = size == NotFound ? decimals[0] : size;

            // the last should not be bigger
            // all should have the same size, except the last.
            return (decimals.Last() > size)
                || (decimals.Take(decimals.Length - 1).Any(g => g != size));
        }

        private static string StripNumericPreAndSuffix(string numericToken)
        {
            var start = 0;
            var length = numericToken.Length;

            // hexadecimal en binary prefixes
            if (numericToken.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) ||
                numericToken.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
            {
                start = 2;
                length -= 2;
            }
            // UL and LU suffix.
            else if (numericToken.EndsWith("UL", StringComparison.OrdinalIgnoreCase) ||
                numericToken.EndsWith("LU", StringComparison.OrdinalIgnoreCase))
            {
                length -= 2;
            }
            // single suffixes
            else if ("LDFUMldfum".IndexOf(numericToken[numericToken.Length - 1]) != NotFound)
            {
                length--;
            }

            return numericToken.Substring(start, length);
        }
    }
}
