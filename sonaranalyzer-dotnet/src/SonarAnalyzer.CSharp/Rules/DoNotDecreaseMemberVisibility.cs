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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Generic;
using System;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class DoNotDecreaseMemberVisibility : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4015";
        private const string MessageFormat = "This member hides '{0}'. Make it non-private or seal the class.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    var classDeclaration = c.Node as ClassDeclarationSyntax;
                    var classSymbol = c.SemanticModel.GetDeclaredSymbol(classDeclaration);

                    if (classSymbol == null ||
                        classDeclaration.Identifier.IsMissing ||
                        classSymbol.IsSealed)
                    {
                        return;
                    }

                    var issueFinder = new IssueFinder(classSymbol, c.SemanticModel);

                    classDeclaration
                        .Members
                        .Select(issueFinder.FindIssue)
                        .WhereNotNull()
                        .ForEach(c.ReportDiagnostic);
                },
                SyntaxKind.ClassDeclaration);
        }

        private class IssueFinder
        {
            private readonly IEnumerable<IMethodSymbol> allBaseClassMethods;
            private readonly IEnumerable<IPropertySymbol> allBaseClassProperties;
            private readonly SemanticModel semanticModel;

            public IssueFinder(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
                var allBaseClassMembers = GetAllBaseMembers(classSymbol, m => m.DeclaredAccessibility != Accessibility.Private);
                allBaseClassMethods = allBaseClassMembers.OfType<IMethodSymbol>();
                allBaseClassProperties = allBaseClassMembers.OfType<IPropertySymbol>();
            }

            private static IEnumerable<ISymbol> GetAllBaseMembers(INamedTypeSymbol classType, Func<ISymbol, bool> filter)
            {
                while (classType?.BaseType != null)
                {
                    foreach (var member in classType.BaseType.GetMembers().Where(filter))
                    {
                        yield return member;
                    }

                    classType = classType.BaseType;
                }
            }

            public Diagnostic FindIssue(MemberDeclarationSyntax memberDeclaration)
            {
                var memberSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration);

                var methodSymbol = memberSymbol as IMethodSymbol;
                if (methodSymbol != null)
                {
                    var hidingMethod = allBaseClassMethods.FirstOrDefault(
                        m => DecrasesAccess(m.DeclaredAccessibility, methodSymbol.DeclaredAccessibility) &&
                             IsMatchingSignature(m, methodSymbol));

                    if (hidingMethod != null)
                    {
                        var location = (memberDeclaration as MethodDeclarationSyntax)?.Identifier.GetLocation();
                        if (location != null)
                        {
                            return Diagnostic.Create(rule, location, hidingMethod);
                        }
                    }

                    return null;
                }

                var propertySymbol = memberSymbol as IPropertySymbol;
                if (propertySymbol != null)
                {
                    var hidingProperty = allBaseClassProperties.FirstOrDefault(p => DecreasesPropertyAccess(p, propertySymbol));
                    if (hidingProperty != null)
                    {
                        var location = (memberDeclaration as PropertyDeclarationSyntax).Identifier.GetLocation();
                        return Diagnostic.Create(rule, location, hidingProperty);
                    }
                }
                return null;
            }

            private static bool DecreasesPropertyAccess(IPropertySymbol baseProperty, IPropertySymbol propertySymbol)
            {
                if (baseProperty.Name != propertySymbol.Name ||
                    !Equals(baseProperty.Type, propertySymbol.Type))
                {
                    return false;
                }

                var baseGetAccess = GetEffectiveDeclaredAccess(baseProperty.GetMethod, baseProperty.DeclaredAccessibility);
                var baseSetAccess = GetEffectiveDeclaredAccess(baseProperty.SetMethod, baseProperty.DeclaredAccessibility);

                var propertyGetAccess = GetEffectiveDeclaredAccess(propertySymbol.GetMethod, baseProperty.DeclaredAccessibility);
                var propertySetAccess = GetEffectiveDeclaredAccess(propertySymbol.SetMethod, baseProperty.DeclaredAccessibility);

                return DecrasesAccess(baseGetAccess, propertyGetAccess) ||
                       DecrasesAccess(baseSetAccess, propertySetAccess);
            }

            private static Accessibility GetEffectiveDeclaredAccess(IMethodSymbol method, Accessibility propertyDefaultAccess)
            {
                if (method == null)
                {
                    return Accessibility.NotApplicable;
                }

                return method.DeclaredAccessibility != Accessibility.NotApplicable ? method.DeclaredAccessibility : propertyDefaultAccess;
            }

            private static bool IsMatchingSignature(IMethodSymbol baseMethod, IMethodSymbol methodSymbol)
            {
                if (baseMethod.Name != methodSymbol.Name ||
                    baseMethod.TypeParameters.Length != methodSymbol.TypeParameters.Length)
                {
                    return false;
                }

                bool hasMatchingParameterTypes = CollectionUtils.AreEqual(baseMethod.Parameters, methodSymbol.Parameters,
                    AreParameterTypesEqual);

                return hasMatchingParameterTypes;
            }

            private static bool AreParameterTypesEqual(IParameterSymbol p1, IParameterSymbol p2)
            {
                if (p1.Type.TypeKind == TypeKind.TypeParameter)
                {
                    return p2.Type.TypeKind == TypeKind.TypeParameter;
                }

                return Equals(p1.Type.OriginalDefinition, p2.Type.OriginalDefinition);
            }

            private static bool DecrasesAccess(Accessibility baseAccess, Accessibility memberAccess)
            {
                if (baseAccess == Accessibility.NotApplicable || memberAccess == Accessibility.NotApplicable)
                {
                    return false;
                }

                return memberAccess == Accessibility.Private ||
                       (baseAccess == Accessibility.Public && memberAccess != Accessibility.Public);
            }

        }
    }
}
