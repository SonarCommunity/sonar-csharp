﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class AllBranchesShouldNotHaveSameImplementationBase : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3923";
        internal const string MessageFormat = "{0}";

        protected abstract class SameImplementationAnalyzerBase
        {
            public abstract Action<SyntaxNodeAnalysisContext> GetAction(DiagnosticDescriptor rule, params object[] messageArgs);
        }

        protected abstract class IfStatementAnalyzerBase<TElseSyntax, TIfSyntax> : SameImplementationAnalyzerBase
            where TElseSyntax : SyntaxNode
            where TIfSyntax : SyntaxNode
        {
            public override Action<SyntaxNodeAnalysisContext> GetAction(DiagnosticDescriptor rule, params object[] messageArgs) =>
                context =>
                {
                    var elseSyntax = (TElseSyntax)context.Node;

                    if (!IsLastElseInChain(elseSyntax))
                    {
                        return;
                    }

                    var ifBlocksStatements = GetIfBlocksStatements(elseSyntax, out var topLevelIf);

                    var elseStatements = GetStatements(elseSyntax);

                    if (ifBlocksStatements.All(ifStatements => AreEquivalent(ifStatements, elseStatements)))
                    {
                        context.ReportDiagnosticWhenActive(Diagnostic.Create(rule, topLevelIf.GetLocation(), messageArgs));
                    }
                };

            protected abstract IEnumerable<SyntaxNode> GetStatements(TElseSyntax elseSyntax);

            protected abstract IEnumerable<IEnumerable<SyntaxNode>> GetIfBlocksStatements(TElseSyntax elseSyntax,
                out TIfSyntax topLevelIf);

            protected abstract bool IsLastElseInChain(TElseSyntax elseSyntax);

            private static bool AreEquivalent(IEnumerable<SyntaxNode> nodes1, IEnumerable<SyntaxNode> nodes2) =>
                nodes1.Equals(nodes2, (x, y) => x.IsEquivalentTo(y, false));
        }

        protected abstract class TernaryStatementAnalyzerBase<TTernaryStatement> : SameImplementationAnalyzerBase
            where TTernaryStatement : SyntaxNode
        {
            protected abstract SyntaxNode GetWhenTrue(TTernaryStatement ternaryStatement);

            protected abstract SyntaxNode GetWhenFalse(TTernaryStatement ternaryStatement);

            public override Action<SyntaxNodeAnalysisContext> GetAction(DiagnosticDescriptor rule, params object[] messageArgs) =>
                context =>
                {
                    var ternaryStatement = (TTernaryStatement)context.Node;

                    var whenTrue = GetWhenTrue(ternaryStatement);
                    var whenFalse = GetWhenFalse(ternaryStatement);

                    if (whenTrue.IsEquivalentTo(whenFalse, false))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(rule, ternaryStatement.GetLocation(), messageArgs));
                    }
                };
        }

        protected abstract class SwitchStatementAnalyzerBase<TSwitchStatement, TSwitchSection> : SameImplementationAnalyzerBase
            where TSwitchStatement : SyntaxNode
            where TSwitchSection : SyntaxNode
        {
            protected abstract IEnumerable<TSwitchSection> GetSections(TSwitchStatement switchStatement);

            protected abstract bool HasDefaultLabel(TSwitchStatement switchStatement);

            protected abstract bool AreEquivalent(TSwitchSection section1, TSwitchSection section2);

            public override Action<SyntaxNodeAnalysisContext> GetAction(DiagnosticDescriptor rule, params object[] messageArgs) =>
                context =>
                {
                    var switchStatement = (TSwitchStatement)context.Node;

                    var sections = GetSections(switchStatement).ToList();

                    if (sections.Count >= 2 &&
                        HasDefaultLabel(switchStatement) &&
                        sections.Skip(1).All(section => AreEquivalent(section, sections[0])))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(rule, switchStatement.GetLocation(), messageArgs));
                    }
                };
        }
    }
}
