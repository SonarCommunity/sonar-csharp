﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class NonDerivedPrivateClassesShouldBeSealed : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3260";
        private const string MessageFormat = "Private classes or records which are not derived in the current assembly should be marked as 'sealed'.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var baseTypeDeclarationSyntax = (BaseTypeDeclarationSyntax)c.Node;
                if (IsPrivateButNotSealedType(baseTypeDeclarationSyntax))
                {
                    var nestedPrivateTypeInfo = (INamedTypeSymbol)c.SemanticModel.GetDeclaredSymbol(c.Node);

                    if (!IsPrivateTypeInherited(nestedPrivateTypeInfo))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, baseTypeDeclarationSyntax.Identifier.GetLocation()));
                    }
                }
            },
            SyntaxKind.ClassDeclaration,
            SyntaxKindEx.RecordClassDeclaration);

        private static bool IsPrivateButNotSealedType(BaseTypeDeclarationSyntax typeDeclaration) =>
            typeDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);

        private static bool IsPrivateTypeInherited(INamedTypeSymbol privateTypeInfo) =>
            privateTypeInfo.ContainingType
                           .GetMembers()
                           .OfType<INamedTypeSymbol>()
                           .Any(symbol => !symbol.Name.Equals(privateTypeInfo.Name)
                                          && symbol.BaseType != null
                                          && symbol.BaseType.Equals(privateTypeInfo));
    }
}
