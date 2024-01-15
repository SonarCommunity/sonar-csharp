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

namespace SonarAnalyzer.Rules
{
    public abstract class MethodsShouldNotHaveIdenticalImplementationsBase<TSyntaxKind, TMethodDeclarationSyntax> : SonarDiagnosticAnalyzer<TSyntaxKind>
        where TSyntaxKind : struct
    {
        private const string DiagnosticId = "S4144";

        protected abstract TSyntaxKind[] SyntaxKinds { get; }

        protected abstract IEnumerable<TMethodDeclarationSyntax> GetMethodDeclarations(SyntaxNode node);
        protected abstract SyntaxToken GetMethodIdentifier(TMethodDeclarationSyntax method);
        protected abstract bool AreDuplicates(TMethodDeclarationSyntax firstMethod, TMethodDeclarationSyntax secondMethod);

        protected override string MessageFormat => "Update this method so that its implementation is not identical to '{0}'.";

        protected MethodsShouldNotHaveIdenticalImplementationsBase() : base(DiagnosticId) { }

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(Language.GeneratedCodeRecognizer,
                c =>
                {
                    if (IsExcludedFromBeingExamined(c))
                    {
                        return;
                    }

                    var methodsToHandle = new LinkedList<TMethodDeclarationSyntax>(GetMethodDeclarations(c.Node));

                    while (methodsToHandle.Any())
                    {
                        var method = methodsToHandle.First.Value;
                        methodsToHandle.RemoveFirst();
                        var duplicates = methodsToHandle.Where(x => AreDuplicates(method, x)).ToList();

                        foreach (var duplicate in duplicates)
                        {
                            methodsToHandle.Remove(duplicate);
                            c.ReportIssue(SupportedDiagnostics[0].CreateDiagnostic(c.Compilation,
                                GetMethodIdentifier(duplicate).GetLocation(),
                                additionalLocations: new[] { GetMethodIdentifier(method).GetLocation() },
                                properties: null,
                                messageArgs: GetMethodIdentifier(method).ValueText));
                        }
                    }
                },
                SyntaxKinds);

        protected virtual bool IsExcludedFromBeingExamined(SonarSyntaxNodeReportingContext context) =>
            context.ContainingSymbol.Kind != SymbolKind.NamedType;

        protected static bool HaveSameParameters<TSyntax>(SeparatedSyntaxList<TSyntax>? leftParameters, SeparatedSyntaxList<TSyntax>? rightParameters)
            where TSyntax : SyntaxNode =>
            (leftParameters == null && rightParameters == null)
            || (leftParameters != null
                && rightParameters != null
                && leftParameters.Value.Count == rightParameters.Value.Count
                && leftParameters.Value.Select((left, index) => left.IsEquivalentTo(rightParameters.Value[index])).All(x => x));
    }
}
