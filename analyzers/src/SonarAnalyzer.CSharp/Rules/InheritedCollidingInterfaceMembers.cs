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
    public sealed class InheritedCollidingInterfaceMembers : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3444";
        private const string MessageFormat = "Rename or add member{1} {0} to this interface to resolve ambiguities.";
        private const int MaxMemberDisplayCount = 2;
        private const int MinBaseListTypes = 2;

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var interfaceDeclaration = (InterfaceDeclarationSyntax)c.Node;
                    if (interfaceDeclaration.BaseList == null || interfaceDeclaration.BaseList.Types.Count < MinBaseListTypes)
                    {
                        return;
                    }

                    var interfaceSymbol = c.SemanticModel.GetDeclaredSymbol(interfaceDeclaration);
                    if (interfaceSymbol == null)
                    {
                        return;
                    }

                    var collidingMembers = GetCollidingMembers(interfaceSymbol).Take(MaxMemberDisplayCount + 1).ToList();
                    if (collidingMembers.Any())
                    {
                        var membersText = GetIssueMessageText(collidingMembers, c.SemanticModel, interfaceDeclaration.SpanStart);
                        var pluralize = collidingMembers.Count > 1 ? "s" : string.Empty;

                        var secondaryLocations = collidingMembers.SelectMany(x => x.Locations)
                                                                 .Where(x => x.IsInSource);

                        c.ReportIssue(Rule.CreateDiagnostic(c.Compilation, interfaceDeclaration.Identifier.GetLocation(), secondaryLocations, membersText, pluralize));
                    }
                },
                SyntaxKind.InterfaceDeclaration);

        private static IEnumerable<IMethodSymbol> GetCollidingMembers(ITypeSymbol interfaceSymbol)
        {
            var interfacesToCheck = interfaceSymbol.Interfaces;

            var membersFromDerivedInterface = interfaceSymbol.GetMembers().OfType<IMethodSymbol>().ToList();

            for (var i = 0; i < interfacesToCheck.Length; i++)
            {
                var notRedefinedMembersFromInterface = interfacesToCheck[i].GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(method =>
                        !method.IsStatic &&
                        method.DeclaredAccessibility != Accessibility.Private &&
                        !membersFromDerivedInterface.Any(redefinedMember => AreCollidingMethods(method, redefinedMember)));

                foreach (var notRedefinedMemberFromInterface1 in notRedefinedMembersFromInterface)
                {
                    for (var j = i + 1; j < interfacesToCheck.Length; j++)
                    {
                        var collidingMembersFromInterface2 = interfacesToCheck[j]
                            .GetMembers(notRedefinedMemberFromInterface1.Name)
                            .OfType<IMethodSymbol>()
                            .Where(IsNotEventRemoveAccessor)
                            .Where(methodSymbol2 => AreCollidingMethods(notRedefinedMemberFromInterface1, methodSymbol2));

                        foreach (var collidingMember in collidingMembersFromInterface2)
                        {
                            yield return collidingMember;
                        }
                    }
                }
            }
        }

        private static bool IsNotEventRemoveAccessor(IMethodSymbol methodSymbol2) =>
            // we only want to report on events once, so we are not collecting the "remove" accessors,
            // and handle the "add" accessor reporting separately in <see cref="GetMemberDisplayName"/>
            methodSymbol2.MethodKind != MethodKind.EventRemove;

        private static string GetIssueMessageText(IEnumerable<IMethodSymbol> collidingMembers, SemanticModel semanticModel, int spanStart)
        {
            var names = collidingMembers.Take(MaxMemberDisplayCount)
                                        .Select(member => GetMemberDisplayName(member, spanStart, semanticModel))
                                        .Distinct()
                                        .ToList();

            return names.Count switch
            {
                1 => names[0],
                2 => $"{names[0]} and {names[1]}",
                _ => names.JoinStr(", ") + ", ..."
            };
        }

        private static readonly ISet<SymbolDisplayPartKind> PartKindsToStartWith = new HashSet<SymbolDisplayPartKind>
        {
            SymbolDisplayPartKind.MethodName,
            SymbolDisplayPartKind.PropertyName,
            SymbolDisplayPartKind.EventName
        };

        private static string GetMemberDisplayName(IMethodSymbol method, int spanStart, SemanticModel semanticModel)
        {
            if (method.AssociatedSymbol is IPropertySymbol { IsIndexer: true } property)
            {
                var text = property.ToMinimalDisplayString(semanticModel, spanStart, SymbolDisplayFormat.CSharpShortErrorMessageFormat);
                return $"'{text}'";
            }

            var parts = method.ToMinimalDisplayParts(semanticModel, spanStart, SymbolDisplayFormat.CSharpShortErrorMessageFormat)
                .SkipWhile(part => !PartKindsToStartWith.Contains(part.Kind))
                .ToList();

            if (method.MethodKind == MethodKind.EventAdd)
            {
                parts = parts.Take(parts.Count - 2).ToList();
            }

            return $"'{string.Join(string.Empty, parts)}'";
        }

        private static bool AreCollidingMethods(IMethodSymbol methodSymbol1, IMethodSymbol methodSymbol2)
        {
            if (methodSymbol1.Name != methodSymbol2.Name ||
                methodSymbol1.MethodKind != methodSymbol2.MethodKind ||
                methodSymbol1.Parameters.Length != methodSymbol2.Parameters.Length ||
                methodSymbol1.Arity != methodSymbol2.Arity)
            {
                return false;
            }

            for (var i = 0; i < methodSymbol1.Parameters.Length; i++)
            {
                var param1 = methodSymbol1.Parameters[i];
                var param2 = methodSymbol2.Parameters[i];

                if (param1.RefKind != param2.RefKind ||
                    !object.Equals(param1.Type, param2.Type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
