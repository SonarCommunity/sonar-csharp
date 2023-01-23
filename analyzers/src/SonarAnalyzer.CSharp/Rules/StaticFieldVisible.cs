﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
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
    public sealed class StaticFieldVisible : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2223";
        private const string MessageFormat = "Change the visibility of '{0}' or make it 'const' or 'readonly'.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(
                c =>
                {
                    var fieldDeclaration = (FieldDeclarationSyntax)c.Node;
                    foreach (var diagnostic in GetDiagnostics(fieldDeclaration.Declaration, c.SemanticModel))
                    {
                        c.ReportIssue(diagnostic);
                    }
                },
                SyntaxKind.FieldDeclaration);

        private static IEnumerable<Diagnostic> GetDiagnostics(VariableDeclarationSyntax declaration, SemanticModel semanticModel) =>
            declaration.Variables
                       .Where(x => FieldIsRelevant(semanticModel.GetDeclaredSymbol(x) as IFieldSymbol))
                       .Select(x => Diagnostic.Create(Rule, x.Identifier.GetLocation(), x.Identifier.ValueText));

        private static bool FieldIsRelevant(IFieldSymbol fieldSymbol) =>
            fieldSymbol is {IsStatic: true, IsConst: false, IsReadOnly: false}
            && fieldSymbol.DeclaredAccessibility != Accessibility.Private
            && !fieldSymbol.GetAttributes().Any(x => x.AttributeClass.Is(KnownType.System_ThreadStaticAttribute));
    }
}
