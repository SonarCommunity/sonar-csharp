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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PureAttributeOnVoidMethod : PureAttributeOnVoidMethodBase<SyntaxKind>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

        protected override void Initialize(SonarAnalysisContext context)
        {
            base.Initialize(context);
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    if ((LocalFunctionStatementSyntaxWrapper)c.Node is var localFunction
                        && localFunction.AttributeLists.SelectMany(x => x.Attributes).Any(IsPureAttribute)
                        && InvalidPureDataAttributeUsage((IMethodSymbol)c.SemanticModel.GetDeclaredSymbol(c.Node)) is { } pureAttribute)
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, pureAttribute.ApplicationSyntaxReference.GetSyntax().GetLocation()));
                    }
                },
                SyntaxKindEx.LocalFunctionStatement);
        }

        private static bool IsPureAttribute(AttributeSyntax attribute) =>
            attribute.Name.GetIdentifier() is { } name
            && (name.Identifier.ValueText == "Pure" || name.Identifier.ValueText == "PureAttribute");
    }
}
