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

using System.IO;
using Google.Protobuf;
using SonarAnalyzer.Protobuf;

namespace SonarAnalyzer.Rules
{
    public abstract class UtilityAnalyzerBase : SonarDiagnosticAnalyzer
    {
        protected static readonly ISet<string> FileExtensionWhitelist = new HashSet<string> { ".cs", ".csx", ".vb" };
        private readonly DiagnosticDescriptor rule;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);
        protected bool IsAnalyzerEnabled { get; set; }
        protected bool IgnoreHeaderComments { get; set; }
        protected virtual bool AnalyzeGeneratedCode { get; set; }
        protected virtual bool AnalyzeTestProjects => true;
        protected string OutPath { get; set; }
        protected bool IsTestProject { get; set; }
        protected override bool EnableConcurrentExecution => false;

        protected UtilityAnalyzerBase(string diagnosticId, string title) =>
            rule = DiagnosticDescriptorFactory.CreateUtility(diagnosticId, title);

        internal static TextRange GetTextRange(FileLinePositionSpan lineSpan) =>
            new()
            {
                StartLine = lineSpan.StartLinePosition.GetLineNumberToReport(),
                EndLine = lineSpan.EndLinePosition.GetLineNumberToReport(),
                StartOffset = lineSpan.StartLinePosition.Character,
                EndOffset = lineSpan.EndLinePosition.Character
            };

        protected void ReadParameters(SonarCompilationReportingContext c)
        {
            var settings = c.Options.ParseSonarLintXmlSettings();
            var outPath = c.ProjectConfiguration().OutPath;
            // For backward compatibility with S4MSB <= 5.0
            if (outPath == null && c.Options.ProjectOutFolderPath() is { } projectOutFolderAdditionalFile)
            {
                outPath = projectOutFolderAdditionalFile.GetText().ToString().TrimEnd();
            }
            if (settings.Any() && !string.IsNullOrEmpty(outPath))
            {
                IgnoreHeaderComments = PropertiesHelper.ReadIgnoreHeaderCommentsProperty(settings, c.Compilation.Language);
                AnalyzeGeneratedCode = PropertiesHelper.ReadAnalyzeGeneratedCodeProperty(settings, c.Compilation.Language);
                OutPath = Path.Combine(outPath, c.Compilation.Language == LanguageNames.CSharp ? "output-cs" : "output-vbnet");
                IsAnalyzerEnabled = true;
                IsTestProject = c.IsTestProject();
            }
        }
    }

    public abstract class UtilityAnalyzerBase<TSyntaxKind, TMessage> : UtilityAnalyzerBase
        where TSyntaxKind : struct
        where TMessage : class, IMessage, new()
    {
        private static readonly object FileWriteLock = new TMessage();

        protected abstract ILanguageFacade<TSyntaxKind> Language { get; }
        protected abstract string FileName { get; }
        protected abstract TMessage CreateMessage(SyntaxTree syntaxTree, SemanticModel semanticModel);

        protected virtual bool AnalyzeUnchangedFiles => false;

        protected virtual IEnumerable<TMessage> CreateAnalysisMessages(SonarCompilationReportingContext c) => Enumerable.Empty<TMessage>();

        protected UtilityAnalyzerBase(string diagnosticId, string title) : base(diagnosticId, title) { }

        protected sealed override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationAction(c =>
                {
                    ReadParameters(c);
                    if (!IsAnalyzerEnabled)
                    {
                        return;
                    }

                    var treeMessages = c.Compilation.SyntaxTrees
                        .Where(x => ShouldGenerateMetrics(c, x))
                        .Select(x => CreateMessage(x, c.Compilation.GetSemanticModel(x)));
                    var messages = CreateAnalysisMessages(c)
                        .Concat(treeMessages)
                        .WhereNotNull()
                        .ToArray();
                    lock (FileWriteLock)
                    {
                        Directory.CreateDirectory(OutPath);
                        using var stream = File.Create(Path.Combine(OutPath, FileName));
                        foreach (var message in messages)
                        {
                            message.WriteDelimitedTo(stream);
                        }
                    }
                });

        protected virtual bool ShouldGenerateMetrics(SyntaxTree tree) =>
            // The results of Metrics and CopyPasteToken analyzers are not needed for Test projects yet the plugin side expects the protobuf files, so we create empty ones.
            (AnalyzeTestProjects || !IsTestProject)
            && FileExtensionWhitelist.Contains(Path.GetExtension(tree.FilePath))
            && (AnalyzeGeneratedCode || !Language.GeneratedCodeRecognizer.IsGenerated(tree));

        private bool ShouldGenerateMetrics(SonarCompilationReportingContext context, SyntaxTree tree) =>
            (AnalyzeUnchangedFiles || !context.IsUnchanged(tree))
            && ShouldGenerateMetrics(tree);
    }
}
