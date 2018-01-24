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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class ShouldImplementExportedInterfaces : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4159";
        private const string MessageFormat = "Implement '{0}' on '{1}' or remove this Export attribute.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var attributeSyntax = (AttributeSyntax)c.Node;

                    var exportedType = GetExportedTypeSyntax(attributeSyntax, c.SemanticModel);
                    var attributeTargetType = GetAttributeTargetSymbol(attributeSyntax, c.SemanticModel);

                    if (exportedType != null &&
                        attributeTargetType != null &&
                        !attributeTargetType.DerivesOrImplements(exportedType))
                    {
                        c.ReportDiagnosticWhenActive(
                            Diagnostic.Create(rule, attributeSyntax.GetLocation(), exportedType.Name, attributeTargetType.Name));
                    }
                },
                SyntaxKind.Attribute);
        }

        private ITypeSymbol GetAttributeTargetSymbol(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
        {
            // Parent is AttributeListSyntax, we handle only class attributes
            var attributeTarget = attributeSyntax.Parent?.Parent as ClassDeclarationSyntax;
            if (attributeTarget == null)
            {
                return null;
            }
            return semanticModel.GetDeclaredSymbol(attributeTarget);
        }

        private ITypeSymbol GetExportedTypeSyntax(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            if (attribute.ArgumentList == null)
            {
                return null;
            }

            var arguments = attribute.ArgumentList.Arguments;
            if (arguments.Count == 0 ||
                arguments.Count > 2)
            {
                return null;
            }

            var typeOfExpression = GetTypeFromNamedArgument(arguments) ??
                GetTypeFromSingleArgumentAttribute(arguments) ??
                GetTypeFromDoubleArgumentAttribute(arguments, semanticModel);

            var exportedTypeSyntax = (typeOfExpression as TypeOfExpressionSyntax)?.Type;
            if (exportedTypeSyntax == null)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(exportedTypeSyntax).Symbol as ITypeSymbol;
        }

        private static ExpressionSyntax GetTypeFromNamedArgument(IEnumerable<AttributeArgumentSyntax> arguments) =>
            arguments.FirstOrDefault(IsContractTypeNamedArgument)?.Expression;

        private static ExpressionSyntax GetTypeFromDoubleArgumentAttribute(IEnumerable<AttributeArgumentSyntax> arguments,
            SemanticModel semanticModel)
        {
            var firstArgument = arguments.ElementAtOrDefault(0)?.Expression;
            if (firstArgument != null &&
                semanticModel.GetConstantValue(firstArgument).Value is string)
            {
                // Two arguments, second should be typeof expression
                return arguments.ElementAtOrDefault(1)?.Expression;
            }

            return null;
        }

        private static ExpressionSyntax GetTypeFromSingleArgumentAttribute(IEnumerable<AttributeArgumentSyntax> arguments)
        {
            if (arguments.ElementAtOrDefault(1) != null)
            {
                return null;
            }

            // Only one argument, should be typeof expression
            return arguments.ElementAtOrDefault(0)?.Expression;
        }

        private static bool IsContractTypeNamedArgument(AttributeArgumentSyntax argument) =>
            argument.NameColon?.Name.Identifier.ValueText == "contractType";
    }
}
