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
    public class WcfNonVoidOneWay : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3598";
        private const string MessageFormat = "This method can't return any values because it is marked as one-way operation.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var methodDeclaration = (MethodDeclarationSyntax)c.Node;
                    var methodSymbol = c.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                    if (methodSymbol == null ||
                        methodSymbol.ReturnsVoid)
                    {
                        return;
                    }

                    AttributeData attribute;
                    if (!TryGetOperationContract(methodSymbol, out attribute))
                    {
                        return;
                    }

                    var asyncPattern = attribute.NamedArguments.FirstOrDefault(na => na.Key == "AsyncPattern").Value.Value as bool?;
                    if (asyncPattern.HasValue &&
                        asyncPattern.Value)
                    {
                        return;
                    }

                    var isOneWay = attribute.NamedArguments.FirstOrDefault(na => na.Key == "IsOneWay").Value.Value as bool?;
                    if (isOneWay.HasValue &&
                        isOneWay.Value)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(rule, methodDeclaration.ReturnType.GetLocation()));
                    }
                },
                SyntaxKind.MethodDeclaration);
        }

        private static bool TryGetOperationContract(IMethodSymbol methodSymbol, out AttributeData attribute)
        {
            attribute = methodSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass.Is(KnownType.System_ServiceModel_OperationContractAttribute));

            return attribute != null;
        }
    }
}
