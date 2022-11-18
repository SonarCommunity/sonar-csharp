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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NonDerivedPrivateClassesShouldBeSealed : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3260";
        private const string MessageFormat = "Private classes or records which are not derived in the current assembly should be marked as 'sealed'.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var typeDeclarationSyntax = (TypeDeclarationSyntax)c.Node;
                if (!c.IsRedundantPositionalRecordContext()
                    && IsPrivateButNotSealedType(typeDeclarationSyntax)
                    && !HasVirtualMembers(typeDeclarationSyntax))
                {
                    var nestedPrivateTypeInfo = (INamedTypeSymbol)c.SemanticModel.GetDeclaredSymbol(c.Node);
                    if (!IsPrivateTypeInherited(nestedPrivateTypeInfo))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, typeDeclarationSyntax.Identifier.GetLocation()));
                    }
                }
            },
            SyntaxKind.ClassDeclaration,
            SyntaxKindEx.RecordClassDeclaration);

        private static bool HasVirtualMembers(TypeDeclarationSyntax typeDeclaration)
        {
            var classMembers = typeDeclaration.Members;
            return classMembers.OfType<MethodDeclarationSyntax>().Any(member => member.Modifiers.Any(SyntaxKind.VirtualKeyword))
                   || classMembers.OfType<PropertyDeclarationSyntax>().Any(member => member.Modifiers.Any(SyntaxKind.VirtualKeyword))
                   || classMembers.OfType<IndexerDeclarationSyntax>().Any(member => member.Modifiers.Any(SyntaxKind.VirtualKeyword))
                   || classMembers.OfType<EventDeclarationSyntax>().Any(member => member.Modifiers.Any(SyntaxKind.VirtualKeyword));
        }

        private static bool IsPrivateButNotSealedType(TypeDeclarationSyntax typeDeclaration) =>
            typeDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword)
            && !typeDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);

        private static bool IsPrivateTypeInherited(INamedTypeSymbol privateTypeInfo) =>
            privateTypeInfo.ContainingType.GetAllNamedTypes()
                                          .Any(symbol => !symbol.MetadataName.Equals(privateTypeInfo.MetadataName)
                                                         && symbol.DerivesFrom(privateTypeInfo));
    }
}
