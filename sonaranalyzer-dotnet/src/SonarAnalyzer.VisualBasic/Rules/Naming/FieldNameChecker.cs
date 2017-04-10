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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.VisualBasic
{
    public abstract class FieldNameChecker : ParameterLoadingDiagnosticAnalyzer
    {
        internal const string Category = SonarAnalyzer.Common.Category.Maintainability;
        internal const Severity RuleSeverity = Severity.Minor;
        internal const bool IsActivatedByDefault = false;

        public virtual string Pattern { get; set; }

        protected abstract bool IsCandidateSymbol(IFieldSymbol symbol);

        protected sealed override void Initialize(ParameterLoadingAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var fieldDeclaration = (FieldDeclarationSyntax)c.Node;
                    foreach (var name in fieldDeclaration.Declarators.SelectMany(v => v.Names).Where(n => n != null))
                    {
                        var symbol = c.SemanticModel.GetDeclaredSymbol(name) as IFieldSymbol;
                        if (symbol != null &&
                            IsCandidateSymbol(symbol) &&
                            !NamingHelper.IsRegexMatch(symbol.Name, Pattern))
                        {
                            c.ReportDiagnostic(Diagnostic.Create(Rule, name.GetLocation(),
                                symbol.Name, Pattern));
                        }
                    }
                },
                SyntaxKind.FieldDeclaration);
        }

        protected abstract DiagnosticDescriptor Rule { get; }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    }
}