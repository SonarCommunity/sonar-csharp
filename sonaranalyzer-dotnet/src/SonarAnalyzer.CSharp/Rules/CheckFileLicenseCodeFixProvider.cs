﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace SonarAnalyzer.Rules.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class CheckFileLicenseCodeFixProvider : SonarCodeFixProvider
    {
        internal const string Title = "Add or update license header";
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CheckFileLicense.DiagnosticId);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected sealed override Task RegisterCodeFixesAsync(SyntaxNode root, CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (!diagnostic.Properties.Any() ||
                !diagnostic.Properties.ContainsKey(CheckFileLicense.IsRegularExpressionPropertyKey) ||
                !diagnostic.Properties.ContainsKey(CheckFileLicense.HeaderFormatPropertyKey))
            {
                return TaskHelper.CompletedTask;
            }

            bool b;
            if (!bool.TryParse(diagnostic.Properties[CheckFileLicense.IsRegularExpressionPropertyKey], out b) || b)
            {
                return TaskHelper.CompletedTask;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var syntaxNode = root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    c =>
                    {
                        var fileHeaderTrivias = CreateFileHeaderTrivias(diagnostic.Properties[CheckFileLicense.HeaderFormatPropertyKey]);
                        var newRoot = root.ReplaceNode(syntaxNode, syntaxNode.WithLeadingTrivia(fileHeaderTrivias));
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }),
                context.Diagnostics);

            return TaskHelper.CompletedTask;
        }

        private static IEnumerable<SyntaxTrivia> CreateFileHeaderTrivias(string comment)
        {
            return new[] { SyntaxFactory.Comment(comment), SyntaxFactory.CarriageReturnLineFeed };
        }
    }
}