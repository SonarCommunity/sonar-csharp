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

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class HardcodedIpAddress : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1313";
        private const string MessageFormat = "Make this IP '{0}' address configurable.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        private static readonly ISet<string> SkippedWords = ImmutableHashSet.Create("VERSION", "ASSEMBLY");

        private static readonly ISet<Type> NodeTypesToCheck = ImmutableHashSet.Create(
            typeof(StatementSyntax),
            typeof(VariableDeclaratorSyntax),
            typeof(ParameterSyntax));

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var stringLiteral = (LiteralExpressionSyntax)c.Node;
                    var text = stringLiteral.Token.ValueText;

                    if (text == "::")
                    {
                        return;
                    }

                    IPAddress address;
                    if (!IPAddress.TryParse(text, out address))
                    {
                        return;
                    }

                    if (address.AddressFamily == AddressFamily.InterNetwork &&
                        text.Split('.').Length != 4)
                    {
                        return;
                    }

                    foreach (var type in NodeTypesToCheck)
                    {
                        var ancestorOrSelf = stringLiteral.FirstAncestorOrSelf<SyntaxNode>(type.IsInstanceOfType);
                        var ancestorString = ancestorOrSelf?.ToString().ToUpperInvariant();
                        if (ancestorString != null && SkippedWords.Any(s => ancestorString.Contains(s)))
                        {
                            return;
                        }
                    }

                    var attribute = stringLiteral.FirstAncestorOrSelf<AttributeSyntax>();
                    if (attribute != null)
                    {
                        return;
                    }

                    c.ReportDiagnostic(Diagnostic.Create(rule, stringLiteral.GetLocation(), text));
                },
                SyntaxKind.StringLiteralExpression);
        }
    }
}
