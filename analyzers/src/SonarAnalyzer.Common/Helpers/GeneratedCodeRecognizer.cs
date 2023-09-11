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

using System.IO;

namespace SonarAnalyzer.Helpers
{
    public abstract class GeneratedCodeRecognizer
    {
        private static readonly string[] GeneratedFileParts =
            {
                ".G.",
                ".GENERATED.",
                ".DESIGNER.",
                "_GENERATED.",
                "TEMPORARYGENERATEDFILE_",
                ".ASSEMBLYATTRIBUTES.VB" // The C# version of this file can already be detected because it contains special comments
            };

        private static readonly string[] AutoGeneratedCommentParts =
            {
                "<AUTO-GENERATED",
                "<AUTOGENERATED",
                "GENERATED BY"
            };

        private static readonly string[] GeneratedCodeAttributes =
            {
                "DebuggerNonUserCode",
                "DebuggerNonUserCodeAttribute",
                "GeneratedCode",
                "GeneratedCodeAttribute",
                "CompilerGenerated",
                "CompilerGeneratedAttribute"
            };

        protected abstract bool IsTriviaComment(SyntaxTrivia trivia);
        protected abstract string GetAttributeName(SyntaxNode node);

        public bool IsGenerated(SyntaxTree tree) =>
             !string.IsNullOrEmpty(tree.FilePath)
             && (HasGeneratedFileName(tree) || HasGeneratedCommentOrAttribute(tree));

        public static bool IsRazorGeneratedFile(SyntaxTree tree) =>
            tree is not null && (IsRazor(tree) || IsCshtml(tree));

        public static bool IsRazor(SyntaxTree tree) =>
            // razor.ide.g.cs is the extension for razor-generated files in the context of design-time builds.
            // However, it is not considered here because of https://github.com/dotnet/razor/issues/9108
            tree.FilePath.EndsWith("razor.g.cs", StringComparison.OrdinalIgnoreCase);

        public static bool IsCshtml(SyntaxTree tree) =>
            // cshtml.ide.g.cs is the extension for razor-generated files in the context of design-time builds.
            // However, it is not considered here because of https://github.com/dotnet/razor/issues/9108
            tree.FilePath.EndsWith("cshtml.g.cs", StringComparison.OrdinalIgnoreCase);

        private bool HasGeneratedCommentOrAttribute(SyntaxTree tree)
        {
            var root = tree.GetRoot();
            if (root == null)
            {
                return false;
            }
            return HasAutoGeneratedComment(root) || HasGeneratedCodeAttribute(root);
        }

        private bool HasAutoGeneratedComment(SyntaxNode root)
        {
            var firstToken = root.GetFirstToken(true);

            if (!firstToken.HasLeadingTrivia)
            {
                return false;
            }

            return firstToken.LeadingTrivia
                .Where(IsTriviaComment)
                .Any(trivia =>
                {
                    var commentText = trivia.ToString().ToUpperInvariant();
                    return Array.Exists(AutoGeneratedCommentParts, commentText.Contains);
                });
        }

        private bool HasGeneratedCodeAttribute(SyntaxNode root)
        {
            var attributeNames = root
                .DescendantNodesAndSelf()
                .Select(GetAttributeName)
                .Where(name => !string.IsNullOrEmpty(name));

            return attributeNames.Any(attributeName =>
                Array.Exists(GeneratedCodeAttributes, generatedCodeAttribute =>
                    attributeName.EndsWith(generatedCodeAttribute, StringComparison.Ordinal)));
        }

        private static bool HasGeneratedFileName(SyntaxTree tree)
        {
            var fileName = Path.GetFileName(tree.FilePath).ToUpperInvariant();
            return Array.Exists(GeneratedFileParts, fileName.Contains);
        }
    }
}
