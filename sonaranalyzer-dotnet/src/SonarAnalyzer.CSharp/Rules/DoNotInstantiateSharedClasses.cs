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
    public sealed class DoNotInstantiateSharedClasses : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4277";
        private const string MessageFormat = "Refactor this code so that it doesn't invoke the constructor of this class.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    if (c.IsTest())
                    {
                        return;
                    }

                    var creationSyntax = (ObjectCreationExpressionSyntax)c.Node;

                    var createdType = c.SemanticModel.GetTypeInfo(creationSyntax).Type;
                    if (createdType != null &&
                        createdType.GetAttributes().Any(IsSharedPartCreationPolicyAttribute))
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(rule, creationSyntax.GetLocation()));
                    }
                },
                SyntaxKind.ObjectCreationExpression);
        }

        private static bool IsSharedPartCreationPolicyAttribute(AttributeData data)
        {
            return IsPartCreationPolicyAttribute(data) && IsShared(data);
        }

        private static bool IsPartCreationPolicyAttribute(AttributeData data)
        {
            return data.AttributeClass.Is(KnownType.System_ComponentModel_Composition_PartCreationPolicyAttribute);
        }

        private static bool IsShared(AttributeData data)
        {
            // This is equivalent to System.ComponentModel.Composition.CreationPolicy.Shared,
            // but we do not want dependency on System.ComponentModel.Composition just for that.
            const int CreationPolicy_Shared = 1;

            return data.ConstructorArguments.Any(arg =>
                    arg.Type.Is(KnownType.System_ComponentModel_Composition_CreationPolicy) &&
                    Equals(arg.Value, CreationPolicy_Shared));
        }
    }
}
