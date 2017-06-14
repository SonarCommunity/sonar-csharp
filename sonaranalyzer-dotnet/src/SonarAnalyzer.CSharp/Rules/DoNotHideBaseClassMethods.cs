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
    public sealed class DoNotHideBaseClassMethods : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4019";
        private const string MessageFormat = "Remove or rename that method because it hides '{0}'.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    var classDeclaration = c.Node as ClassDeclarationSyntax;
                    var classSymbol = c.SemanticModel.GetDeclaredSymbol(classDeclaration);

                    if (classSymbol == null || classDeclaration.Identifier.IsMissing)
                    {
                        return;
                    }

                    var issueFinder = new IssueFinder(classSymbol, c.SemanticModel);

                    classDeclaration
                        .Members
                        .OfType<MemberDeclarationSyntax>()
                        .Select(issueFinder.FindIssue)
                        .WhereNotNull()
                        .ToList()
                        .ForEach(c.ReportDiagnostic);
                },
                SyntaxKind.ClassDeclaration);
        }

        private class IssueFinder
        {
            private readonly IList<IMethodSymbol> allBaseClassMethods;
            private readonly SemanticModel semanticModel;

            public IssueFinder(INamedTypeSymbol classTypeSymbol, SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
                allBaseClassMethods = GetAllBaseMethods(classTypeSymbol).ToList();
            }

            private static IEnumerable<IMethodSymbol> GetAllBaseMethods(INamedTypeSymbol classTypeSymbol)
            {
                while (classTypeSymbol?.BaseType != null)
                {
                    var members = classTypeSymbol.BaseType
                            .GetMembers()
                            .OfType<IMethodSymbol>()
                            .Where(m => m.DeclaredAccessibility != Accessibility.Private)
                            .Where(m => m.Parameters.Length > 0)
                            .Where(m => !string.IsNullOrEmpty(m.Name));

                    foreach (var m in members)
                    {
                        yield return m;
                    }

                    classTypeSymbol = classTypeSymbol.BaseType;
                }
            }

            public Diagnostic FindIssue(MemberDeclarationSyntax memberDeclaration)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration) as IMethodSymbol;
                var issueLocation = (memberDeclaration as MethodDeclarationSyntax)?.Identifier.GetLocation();

                if (methodSymbol == null || issueLocation == null)
                {
                    return null;
                }

                var baseMethodHidden = FindBaseMethodHiddenByMethod(methodSymbol);
                return baseMethodHidden != null ? Diagnostic.Create(rule, issueLocation, baseMethodHidden) : null;
            }

            private IMethodSymbol FindBaseMethodHiddenByMethod(IMethodSymbol methodSymbol)
            {
                var baseMemberCandidates = allBaseClassMethods.Where(m => m.Name == methodSymbol.Name);

                IMethodSymbol hiddenBaseMethodCandidate = null;
                var hasBaseMethodWithSameSignature = baseMemberCandidates.Any(baseMember =>
                {
                    var result = ComputSignatureMatch(baseMember, methodSymbol);
                    if (result == Match.WeaklyDerived)
                    {
                        hiddenBaseMethodCandidate = hiddenBaseMethodCandidate ?? baseMember;
                    }

                    return result == Match.Identical;
                });

                return hasBaseMethodWithSameSignature ? null : hiddenBaseMethodCandidate;
            }

            private enum Match { Different, Identical, WeaklyDerived }

            private Match ComputSignatureMatch(IMethodSymbol baseMethodSymbol, IMethodSymbol methodSymbol)
            {
                var baseMethodParams = baseMethodSymbol.Parameters;
                var methodParams = methodSymbol.Parameters;

                if (baseMethodParams.Length != methodParams.Length)
                {
                    return Match.Different;
                }

                bool hasWeaklyDerivedParams = false;
                for (int i = 0; i < methodParams.Length; i++)
                {
                    var match = ComputeParameterMatch(baseMethodParams[i], methodParams[i]);

                    if (match == Match.Different)
                    {
                        return Match.Different;
                    }

                    if (match == Match.WeaklyDerived)
                    {
                        hasWeaklyDerivedParams = true;
                    }
                }

                return hasWeaklyDerivedParams ? Match.WeaklyDerived : Match.Identical;
            }

            private Match ComputeParameterMatch(IParameterSymbol baseParam, IParameterSymbol methodParam)
            {
                if (baseParam.Type.TypeKind == TypeKind.TypeParameter)
                {
                    return methodParam.Type.TypeKind == TypeKind.TypeParameter ? Match.Identical : Match.Different;
                }

                if (Equals(baseParam.Type.OriginalDefinition, methodParam.Type.OriginalDefinition))
                {
                    return Match.Identical;
                }

                return baseParam.Type.DerivesOrImplements(methodParam.Type) ? Match.WeaklyDerived : Match.Different;
            }
        }
    }
}