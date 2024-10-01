/*
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

using System.Collections.Concurrent;
using System.IO;
using Google.Protobuf;
using SonarAnalyzer.Protobuf;

namespace SonarAnalyzer.Rules
{
    public readonly record struct UtilityAnalyzerParameters(bool IsAnalyzerEnabled, bool IgnoreHeaderComments, bool AnalyzeGeneratedCode, bool AnalyzeTestProjects, string OutPath, bool IsTestProject)
    {
        public static readonly UtilityAnalyzerParameters Default =
            new(IsAnalyzerEnabled: false, IgnoreHeaderComments: false, AnalyzeGeneratedCode: false, AnalyzeTestProjects: true, OutPath: null, IsTestProject: false);
    }

    public abstract class UtilityAnalyzerBase : SonarDiagnosticAnalyzer
    {
        protected static readonly ISet<string> FileExtensionWhitelist = new HashSet<string> { ".cs", ".csx", ".vb" };
        private readonly DiagnosticDescriptor rule;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(rule);

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

        protected virtual UtilityAnalyzerParameters ReadParameters<T>(SonarAnalysisContextBase<T> context)
        {
            var outPath = context.ProjectConfiguration().OutPath;
            // For backward compatibility with S4MSB <= 5.0
            if (outPath == null && context.Options.ProjectOutFolderPath() is { } projectOutFolderAdditionalFile)
            {
                outPath = projectOutFolderAdditionalFile.GetText().ToString().TrimEnd();
            }
            if (context.Options.SonarLintXml() != null && !string.IsNullOrEmpty(outPath))
            {
                var language = context.Compilation.Language;
                var sonarLintXml = context.SonarLintXml();
                return new UtilityAnalyzerParameters(
                    IsAnalyzerEnabled: true,
                    IgnoreHeaderComments: sonarLintXml.IgnoreHeaderComments(language),
                    AnalyzeGeneratedCode: sonarLintXml.AnalyzeGeneratedCode(language),
                    AnalyzeTestProjects: true,
                    OutPath: Path.Combine(outPath, language == LanguageNames.CSharp ? "output-cs" : "output-vbnet"),
                    IsTestProject: context.IsTestProject());
            }
            return UtilityAnalyzerParameters.Default;
        }
    }

    public abstract class UtilityAnalyzerBase<TSyntaxKind, TMessage> : UtilityAnalyzerBase
        where TSyntaxKind : struct
        where TMessage : class, IMessage, new()
    {
        protected abstract ILanguageFacade<TSyntaxKind> Language { get; }
        protected abstract string FileName { get; }
        protected abstract TMessage CreateMessage(UtilityAnalyzerParameters parameters, SyntaxTree tree, SemanticModel model);

        protected virtual bool AnalyzeUnchangedFiles => false;

        protected virtual IEnumerable<TMessage> CreateAnalysisMessages(SonarCompilationReportingContext c) => Enumerable.Empty<TMessage>();

        protected UtilityAnalyzerBase(string diagnosticId, string title) : base(diagnosticId, title) { }

        protected sealed override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationStartAction(startContext =>
            {
                var parameters = ReadParameters(startContext);
                if (!parameters.IsAnalyzerEnabled)
                {
                    return;
                }
                var cancel = startContext.Cancel;
                var outPath = parameters.OutPath;
                var treeMessages = new BlockingCollection<TMessage>();
                var consumerTask = Task.Factory.StartNew(() =>
                {
                    // Consume all messages as they arrive during the compilation and write them to disk.
                    // The Task starts on CompilationStart and in CompilationEnd we block until it is finished via CompleteAdding().
                    // Note: CompilationEndAction is not guaranteed to be called for each CompilationStart.
                    // Therefore it is important to properly handle cancelation here.
                    // LongRunning: We probably run on a dedicated thread outside of the thread pool
                    // If any of the IO operations throw, CompilationEnd takes care of the clean up.
                    Directory.CreateDirectory(outPath);
                    using var stream = File.Create(Path.Combine(outPath, FileName));
                    foreach (var message in treeMessages.GetConsumingEnumerable(cancel).WhereNotNull())
                    {
                        message.WriteDelimitedTo(stream);
                    }
                }, cancel, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                startContext.RegisterSemanticModelAction(modelContext =>
                {
                    if (ShouldGenerateMetrics(parameters, modelContext) && !cancel.IsCancellationRequested)
                    {
                        var message = CreateMessage(parameters, modelContext.Tree, modelContext.SemanticModel);
                        treeMessages.Add(message);
                    }
                });
                startContext.RegisterCompilationEndAction(endContext =>
                {
                    var analysisMessages = CreateAnalysisMessages(endContext);
                    foreach (var message in analysisMessages)
                    {
                        treeMessages.Add(message);
                    }
                    treeMessages.CompleteAdding();
                    try
                    {
                        consumerTask.Wait(cancel); // Wait until all messages are written to disk. Throws, if the task failed.
                    }
                    finally
                    {
                        treeMessages.Dispose();
                    }
                });
            });

        protected virtual bool ShouldGenerateMetrics(UtilityAnalyzerParameters parameters, SyntaxTree tree) =>
            // The results of Metrics and CopyPasteToken analyzers are not needed for Test projects yet the plugin side expects the protobuf files, so we create empty ones.
            (parameters.AnalyzeTestProjects || !parameters.IsTestProject)
            && FileExtensionWhitelist.Contains(Path.GetExtension(tree.FilePath))
            && ShouldGenerateMetricsByType(parameters, tree);

        protected static string GetFilePath(SyntaxTree tree) =>
            // If the syntax tree is constructed for a razor generated file, we need to provide the original file path.
            GeneratedCodeRecognizer.IsRazorGeneratedFile(tree) && tree.GetRoot() is var root && root.ContainsDirectives
                ? root.GetMappedFilePathFromRoot()
                : tree.FilePath;

        private bool ShouldGenerateMetrics(UtilityAnalyzerParameters parameters, SonarSemanticModelReportingContext context) =>
            (AnalyzeUnchangedFiles || !context.IsUnchanged(context.Tree))
            && ShouldGenerateMetrics(parameters, context.Tree);

        private bool ShouldGenerateMetricsByType(UtilityAnalyzerParameters parameters, SyntaxTree tree) =>
            parameters.AnalyzeGeneratedCode
                ? !GeneratedCodeRecognizer.IsCshtml(tree) // We cannot upload metrics for .cshtml files. The file is owned by the html plugin.
                : !tree.IsGenerated(Language.GeneratedCodeRecognizer) || GeneratedCodeRecognizer.IsRazor(tree);
    }
}
