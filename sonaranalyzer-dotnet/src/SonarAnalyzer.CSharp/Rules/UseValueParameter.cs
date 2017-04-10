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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class UseValueParameter : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3237";
        private const string MessageFormat = "Use the 'value' parameter in this {0} accessor declaration.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterCodeBlockStartActionInNonGenerated<SyntaxKind>(
                cbc =>
                {
                    if (cbc.SemanticModel.Compilation.IsTest())
                    {
                        return;
                    }

                    var accessorDeclaration = cbc.CodeBlock as AccessorDeclarationSyntax;
                    if (accessorDeclaration == null ||
                        accessorDeclaration.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        return;
                    }

                    if (accessorDeclaration.Body.Statements.Count == 1 &&
                        accessorDeclaration.Body.Statements.Single() is ThrowStatementSyntax)
                    {
                        return;
                    }

                    var foundValueReference = false;
                    cbc.RegisterSyntaxNodeAction(
                        c =>
                        {
                            var identifier = (IdentifierNameSyntax)c.Node;
                            var parameter = c.SemanticModel.GetSymbolInfo(identifier).Symbol as IParameterSymbol;

                            if (identifier.Identifier.ValueText == "value" &&
                                parameter != null &&
                                parameter.IsImplicitlyDeclared)
                            {
                                foundValueReference = true;
                            }
                        },
                        SyntaxKind.IdentifierName);

                    cbc.RegisterCodeBlockEndAction(
                        c =>
                        {
                            if (!foundValueReference)
                            {
                                var accessorType = GetAccessorType(accessorDeclaration);
                                c.ReportDiagnostic(Diagnostic.Create(rule, accessorDeclaration.Keyword.GetLocation(), accessorType));
                            }
                        });
                });
        }

        private static string GetAccessorType(AccessorDeclarationSyntax accessorDeclaration)
        {
            string accessorType;

            if (accessorDeclaration.IsKind(SyntaxKind.AddAccessorDeclaration) ||
                accessorDeclaration.IsKind(SyntaxKind.RemoveAccessorDeclaration))
            {
                accessorType = "event";
            }
            else
            {
                var accessorList = accessorDeclaration.Parent;
                if (accessorList == null)
                {
                    return null;
                }
                var indexerOrProperty = accessorList.Parent;
                if (indexerOrProperty is IndexerDeclarationSyntax)
                {
                    accessorType = "indexer set";
                }
                else if (indexerOrProperty is PropertyDeclarationSyntax)
                {
                    accessorType = "property set";
                }
                else
                {
                    return null;
                }
            }

            return accessorType;
        }
    }
}
