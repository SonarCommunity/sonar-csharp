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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class ParameterAssignedToBase<TSyntaxKind, TIdentifierNameSyntax> : SonarDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TIdentifierNameSyntax : SyntaxNode
    {
        private const string DiagnosticId = "S1226";
        private const string MessageFormat = "Introduce a new variable instead of reusing the parameter '{0}'.";

        private readonly DiagnosticDescriptor rule;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected abstract ILanguageFacade<TSyntaxKind> Language { get; }
        protected abstract bool IsAssignmentToCatchVariable(ISymbol symbol, SyntaxNode node);

        protected ParameterAssignedToBase() =>
            rule = Language.CreateDescriptor(DiagnosticId, MessageFormat);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                Language.GeneratedCodeRecognizer,
                c =>
                {
                    var left = Language.Syntax.AssignmentLeft(c.Node);
                    if (c.SemanticModel.GetSymbolInfo(left).Symbol is { } symbol
                        && (symbol is IParameterSymbol { RefKind: RefKind.None } || IsAssignmentToCatchVariable(symbol, left))
                        && (!IsReadBefore(c.SemanticModel, symbol, c.Node)))
                    {
                        c.ReportIssue(Diagnostic.Create(SupportedDiagnostics[0], left.GetLocation(), left.ToString()));
                    }
                },
                Language.SyntaxKind.SimpleAssignment);
        }

        private bool IsReadBefore(SemanticModel semanticModel, ISymbol parameterSymbol, SyntaxNode assignment)
        {
            // Same problem as in VB.NET / IsAssignmentToCatchVariable:
            // parameterSymbol.DeclaringSyntaxReferences is empty for Catch syntax in VB.NET as well as for indexer syntax for C#
            // https://github.com/dotnet/roslyn/issues/6209
            var stopLocation = parameterSymbol.Locations.FirstOrDefault();
            if (stopLocation == null)
            {
                return true; // If we can't find the location, it's going to be FN
            }
            return GetPreviousNodes(stopLocation, assignment)
                .Union(Language.Syntax.AssignmentRight(assignment).DescendantNodes())
                .OfType<TIdentifierNameSyntax>()
                .Any(x => parameterSymbol.Equals(semanticModel.GetSymbolInfo(x).Symbol));
        }

        /// <summary>
        /// Returns all nodes before the specified statement to the declaration of variable/parameter given by stopLocation.
        /// This method recursively traverses all parent blocks of the provided statement.
        /// </summary>
        private static IEnumerable<SyntaxNode> GetPreviousNodes(Location stopLocation, SyntaxNode statement)
        {
            // Method declaration or Catch variable declaration, stop here and do not include this statement
            if (statement == null || statement.GetLocation().SourceSpan.IntersectsWith(stopLocation.SourceSpan))
            {
                return Array.Empty<SyntaxNode>();
            }
            var previousNodes = statement.Parent.ChildNodes()
                .TakeWhile(x => x != statement)     // Take all from beginning, including "catch ex" on the way, down to current statement
                .Reverse()                          // Reverse in order to keep the tail
                .TakeWhile(x => !x.GetLocation().SourceSpan.IntersectsWith(stopLocation.SourceSpan))    // Keep the tail until "catch ex" or "int i" is found
                .SelectMany(x => x.DescendantNodes());

            return previousNodes.Union(GetPreviousNodes(stopLocation, statement.Parent));
        }
    }
}
