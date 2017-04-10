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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class RedundantInheritanceList : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1939";
        private const string MessageFormat = "{0}";
        internal const string MessageEnum = "'int' should not be explicitly used as the underlying type.";
        internal const string MessageObjectBase = "'Object' should not be explicitly extended.";
        internal const string MessageAlreadyImplements = "'{0}' implements '{1}' so '{1}' can be removed from the inheritance list.";
        internal const string RedundantIndexKey = "redundantIndex";
        private const IdeVisibility ideVisibility = IdeVisibility.Hidden;

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, ideVisibility, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckEnum(c),
                SyntaxKind.EnumDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckInterface(c),
                SyntaxKind.InterfaceDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckClass(c),
                SyntaxKind.ClassDeclaration);
        }

        private static void CheckClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.BaseList == null ||
                !classDeclaration.BaseList.Types.Any())
            {
                return;
            }

            var baseTypeSyntax = classDeclaration.BaseList.Types.First().Type;
            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseTypeSyntax).Symbol as ITypeSymbol;
            if (baseTypeSymbol == null)
            {
                return;
            }

            if (baseTypeSymbol.Is(KnownType.System_Object))
            {
                var location = GetLocationWithToken(baseTypeSyntax, classDeclaration.BaseList.Types);
                context.ReportDiagnostic(Diagnostic.Create(rule, location,
                    ImmutableDictionary<string, string>.Empty.Add(RedundantIndexKey, "0"),
                    MessageObjectBase));
            }

            ReportRedundantInterfaces(context, classDeclaration);
        }

        private static void CheckInterface(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
            if (interfaceDeclaration.BaseList == null ||
                !interfaceDeclaration.BaseList.Types.Any())
            {
                return;
            }

            ReportRedundantInterfaces(context, interfaceDeclaration);
        }

        private static void CheckEnum(SyntaxNodeAnalysisContext context)
        {
            var enumDeclaration = (EnumDeclarationSyntax)context.Node;
            if (enumDeclaration.BaseList == null ||
                !enumDeclaration.BaseList.Types.Any())
            {
                return;
            }

            var baseTypeSyntax = enumDeclaration.BaseList.Types.First().Type;
            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseTypeSyntax).Symbol as ITypeSymbol;
            if (!baseTypeSymbol.Is(KnownType.System_Int32))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(rule, enumDeclaration.BaseList.GetLocation(),
                ImmutableDictionary<string, string>.Empty.Add(RedundantIndexKey, "0"),
                MessageEnum));
        }

        private static void ReportRedundantInterfaces(SyntaxNodeAnalysisContext context, BaseTypeDeclarationSyntax typeDeclaration)
        {
            var declaredType = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
            if (declaredType == null)
            {
                return;
            }

            var baseList = typeDeclaration.BaseList;
            var interfaceTypesWithAllInterfaces = GetImplementedInterfaceMappings(baseList, context.SemanticModel);

            for (int i = 0; i < baseList.Types.Count; i++)
            {
                var baseType = baseList.Types[i];
                var interfaceType = context.SemanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
                if (interfaceType == null ||
                    !interfaceType.IsInterface())
                {
                    continue;
                }

                INamedTypeSymbol collidingDeclaration;
                if (!TryGetCollidingDeclaration(declaredType, interfaceType, interfaceTypesWithAllInterfaces, out collidingDeclaration))
                {
                    continue;
                }

                var location = GetLocationWithToken(baseType.Type, baseList.Types);
                var message = string.Format(MessageAlreadyImplements,
                    collidingDeclaration.ToMinimalDisplayString(context.SemanticModel, baseType.Type.SpanStart),
                    interfaceType.ToMinimalDisplayString(context.SemanticModel, baseType.Type.SpanStart));

                context.ReportDiagnostic(Diagnostic.Create(rule, location,
                    ImmutableDictionary<string, string>.Empty.Add(RedundantIndexKey, i.ToString(CultureInfo.InvariantCulture)),
                    message));
            }
        }

        private static MultiValueDictionary<INamedTypeSymbol, INamedTypeSymbol> GetImplementedInterfaceMappings(
            BaseListSyntax baseList, SemanticModel semanticModel)
        {
            return baseList.Types
                .Select(baseType => semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol)
                .Where(symbol => symbol != null)
                .Select(symbol => new Tuple<INamedTypeSymbol, ICollection<INamedTypeSymbol>>(symbol, symbol.AllInterfaces))
                .ToMultiValueDictionary(kv => kv.Item1, kv => kv.Item2);
        }

        private static bool TryGetCollidingDeclaration(INamedTypeSymbol declaredType, INamedTypeSymbol interfaceType,
            MultiValueDictionary<INamedTypeSymbol, INamedTypeSymbol> interfaceMappings, out INamedTypeSymbol collidingDeclaration)
        {
            var collisionMapping = interfaceMappings
                .Where(i => i.Key.IsInterface())
                .FirstOrDefault(v => v.Value.Contains(interfaceType));

            if (collisionMapping.Key != null)
            {
                collidingDeclaration = collisionMapping.Key;
                return true;
            }

            var baseClassMapping = interfaceMappings.FirstOrDefault(i => i.Key.IsClass());
            if (baseClassMapping.Key == null)
            {
                collidingDeclaration = null;
                return false;
            }

            collidingDeclaration = baseClassMapping.Key;
            return CanInterfacebeRemovedbasedOnMembers(declaredType, interfaceType);
        }

        private static bool CanInterfacebeRemovedbasedOnMembers(INamedTypeSymbol declaredType, INamedTypeSymbol interfaceType)
        {
            var allMembersOfInterface = interfaceType.AllInterfaces.Concat(new[] { interfaceType })
                .SelectMany(i => i.GetMembers())
                .ToList();

            if (!allMembersOfInterface.Any())
            {
                return false;
            }

            foreach (var interfaceMember in allMembersOfInterface)
            {
                var classMember = declaredType.FindImplementationForInterfaceMember(interfaceMember);
                if (classMember != null &&
                    (classMember.ContainingType.Equals(declaredType) ||
                    !classMember.ContainingType.Interfaces.SelectMany(i => i.AllInterfaces.Concat(new[] { i })).Contains(interfaceType)))
                {
                    return false;
                }
            }
            return true;
        }

        private static Location GetLocationWithToken(TypeSyntax type, SeparatedSyntaxList<BaseTypeSyntax> baseTypes)
        {
            int start;
            int end;

            if (baseTypes.Count == 1 ||
                baseTypes.First().Type != type)
            {
                start = type.GetFirstToken().GetPreviousToken().Span.Start;
                end = type.Span.End;
            }
            else
            {
                start = type.SpanStart;
                end = type.GetLastToken().GetNextToken().Span.End;
            }

            return Location.Create(type.SyntaxTree, new TextSpan(start, end - start));
        }
    }
}
