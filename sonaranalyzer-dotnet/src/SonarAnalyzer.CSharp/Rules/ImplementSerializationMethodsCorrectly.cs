/*
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
    public sealed class ImplementSerializationMethodsCorrectly : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3927";
        private const string MessageFormat = "Make this method {0}.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        private const string problemMakePrivateText = "'private'";
        private const string problemReturnVoidText = "return 'void'";
        private const string problemParameterText = "have a single parameter of type 'StreamingContext'";
        private const string problemGenericParameterText = "have no type parameters";

        private static ISet<KnownType> serializationAttributes = new HashSet<KnownType>
        {
            KnownType.System_Runtime_Serialization_OnSerializingAttribute,
            KnownType.System_Runtime_Serialization_OnSerializedAttribute,
            KnownType.System_Runtime_Serialization_OnDeserializingAttribute,
            KnownType.System_Runtime_Serialization_OnDeserializedAttribute
        };

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var methodDeclaration = (MethodDeclarationSyntax)c.Node;
                var methodSymbol = c.SemanticModel.GetDeclaredSymbol(methodDeclaration);

                string errorMessage;
                if (!methodDeclaration.Identifier.IsMissing &&
                    HasSerializationAttribute(methodSymbol) &&
                    HasIssues(methodSymbol, out errorMessage))
                {
                    c.ReportDiagnostic(Diagnostic.Create(rule, methodDeclaration.Identifier.GetLocation(), errorMessage));
                }
            },
            SyntaxKind.MethodDeclaration);
        }

        private static bool HasSerializationAttribute(IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.GetAttributes().Any(attr => attr.AttributeClass.IsAny(serializationAttributes));
        }

        private static bool HasIssues(IMethodSymbol methodSymbol, out string errorMessage)
        {
            errorMessage = null;
            var errors = new List<string>();

            if (methodSymbol.DeclaredAccessibility != Accessibility.Private)
            {
                errors.Add(problemMakePrivateText);
            }

            if (!methodSymbol.ReturnsVoid)
            {
                errors.Add(problemReturnVoidText);
            }

            if (!methodSymbol.TypeParameters.IsEmpty)
            {
                errors.Add(problemGenericParameterText);
            }

            if (methodSymbol.Parameters.Length != 1 ||
                !methodSymbol.Parameters.First().IsType(KnownType.System_Runtime_Serialization_StreamingContext))
            {
                errors.Add(problemParameterText);
            }

            if (errors.Count > 0)
            {
                const string separator = ", ";
                const string lastSeparator = " and ";

                errorMessage = string.Join(separator, errors);

                int lastCommaIdx = errorMessage.LastIndexOf(separator);
                if (lastCommaIdx != -1)
                {
                    errorMessage = errorMessage
                        .Remove(lastCommaIdx, separator.Length)
                        .Insert(lastCommaIdx, lastSeparator);
                }

                return true;
            }

            return false;
        }
    }
}
