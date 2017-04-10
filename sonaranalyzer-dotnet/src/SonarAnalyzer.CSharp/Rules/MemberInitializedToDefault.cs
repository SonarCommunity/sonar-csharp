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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Helpers.CSharp;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class MemberInitializedToDefault : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3052";
        private const string MessageFormat = "Remove this initialization to '{0}', the compiler will do that for you.";
        private const IdeVisibility ideVisibility = IdeVisibility.Hidden;

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, ideVisibility, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckField(c),
                SyntaxKind.FieldDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckEvent(c),
                SyntaxKind.EventFieldDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckAutoProperty(c),
                SyntaxKind.PropertyDeclaration);
        }

        private static void CheckAutoProperty(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

            if (propertyDeclaration.Initializer == null ||
                !IsAutoProperty(propertyDeclaration))
            {
                return;
            }

            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

            if (propertySymbol != null &&
                IsDefaultValueInitializer(propertyDeclaration.Initializer, propertySymbol.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, propertyDeclaration.Initializer.GetLocation(), propertySymbol.Name));
                return;
            }
        }

        internal static bool IsAutoProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            return propertyDeclaration.AccessorList != null &&
                propertyDeclaration.AccessorList.Accessors.All(accessor => accessor.Body == null);
        }

        private static void CheckEvent(SyntaxNodeAnalysisContext context)
        {
            var field = (EventFieldDeclarationSyntax)context.Node;

            foreach (var eventDeclaration in field.Declaration.Variables.Where(v => v.Initializer != null))
            {
                var eventSymbol = context.SemanticModel.GetDeclaredSymbol(eventDeclaration) as IEventSymbol;
                if (eventSymbol == null)
                {
                    continue;
                }

                if (IsDefaultValueInitializer(eventDeclaration.Initializer, eventSymbol.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(rule, eventDeclaration.Initializer.GetLocation(), eventSymbol.Name));
                    return;
                }
            }
        }

        private static void CheckField(SyntaxNodeAnalysisContext context)
        {
            var field = (FieldDeclarationSyntax)context.Node;

            foreach (var variableDeclarator in field.Declaration.Variables.Where(v => v.Initializer != null))
            {
                var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variableDeclarator) as IFieldSymbol;

                if (fieldSymbol != null &&
                    !fieldSymbol.IsConst &&
                    IsDefaultValueInitializer(variableDeclarator.Initializer, fieldSymbol.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(rule, variableDeclarator.Initializer.GetLocation(), fieldSymbol.Name));
                }
            }
        }

        internal static bool IsDefaultValueInitializer(EqualsValueClauseSyntax initializer, ITypeSymbol type)
        {
            return CheckDefaultExpressionInitializer(initializer) ||
                CheckReferenceTypeNullInitializer(initializer, type) ||
                CheckValueTypeDefaultValueInitializer(initializer, type);
        }

        private static bool CheckDefaultExpressionInitializer(EqualsValueClauseSyntax initializer)
        {
            var defaultValue = initializer.Value as DefaultExpressionSyntax;
            return defaultValue != null;
        }

        private static bool CheckReferenceTypeNullInitializer(EqualsValueClauseSyntax initializer, ITypeSymbol type)
        {
            return type.IsReferenceType &&
                EquivalenceChecker.AreEquivalent(SyntaxHelper.NullLiteralExpression, initializer.Value);
        }

        private static bool CheckValueTypeDefaultValueInitializer(EqualsValueClauseSyntax initializer, ITypeSymbol type)
        {
            if (!type.IsValueType)
            {
                return false;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return EquivalenceChecker.AreEquivalent(initializer.Value, SyntaxHelper.FalseLiteralExpression);
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    {
                        double constantValue;
                        return ExpressionNumericConverter.TryGetConstantDoubleValue(initializer.Value, out constantValue) &&
                            Math.Abs(constantValue - default(double)) < double.Epsilon;
                    }
                case SpecialType.System_Char:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    {
                        int constantValue;
                        return ExpressionNumericConverter.TryGetConstantIntValue(initializer.Value, out constantValue) &&
                            constantValue == default(int);
                    }
                default:
                    return false;
            }
        }
    }
}
