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
    public sealed class PropertiesAccessCorrectField : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4275";
        private const string MessageFormat = "Refactor this {0} so that it actually refers to the field '{1}'";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            // We want to check the fields read and assigned in all properties in this class
            // so this is a symbol-level rule (also means the callback is called only once
            // for partial classes)
            context.RegisterSymbolAction(CheckType, SymbolKind.NamedType);
        }

        private void CheckType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind != TypeKind.Class &&
                symbol.TypeKind != TypeKind.Structure)
            {
                return;
            }

            var fields = symbol.GetMembers().Where(m => m.Kind == SymbolKind.Field).OfType<IFieldSymbol>();
            if (!fields.Any())
            {
                return;
            }

            var properties = GetExplictlyDeclaredProperties(symbol);
            if (!properties.Any())
            {
                return;
            }

            var propertyToFieldMatcher = new PropertyToFieldMatcher(fields);
            var allPropertyData = CollectPropertyData(properties, context.Compilation);

            // Check that if there is a single matching field name it is used by the property
            foreach (var data in allPropertyData)
            {
                var expectedField = propertyToFieldMatcher.GetSingleMatchingFieldOrNull(data.PropertySymbol);
                if (expectedField != null)
                {
                    CheckExpectedFieldIsUsed(expectedField, data.FieldUpdated, context);
                    CheckExpectedFieldIsUsed(expectedField, data.FieldReturned, context);
                }
            }
        }

        private static IEnumerable<IPropertySymbol> GetExplictlyDeclaredProperties(INamedTypeSymbol symbol) =>
            symbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Property)
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsImplicitlyDeclared);

        private static void CheckExpectedFieldIsUsed(IFieldSymbol expectedField, FieldData? actualField, SymbolAnalysisContext context)
        {
            if (actualField.HasValue && actualField.Value.Field != expectedField)
            {
                context.ReportDiagnosticWhenActive(Diagnostic.Create(
                    rule,
                    actualField.Value.Location,
                    actualField.Value.AccessorKind == AccessorKind.Getter ? "getter" : "setter",
                    expectedField.Name
                    ));
            }
        }

        private static IList<PropertyData> CollectPropertyData(IEnumerable<IPropertySymbol> properties, Compilation compilation)
        {
            IList<PropertyData> allPropertyData = new List<PropertyData>();

            // Collect the list of fields read/written by each property
            foreach (var property in properties)
            {
                var returned = FindReturnedField(property, compilation);
                var updated = FindFieldAssignment(property, compilation);
                var data = new PropertyData(property, returned, updated);
                allPropertyData.Add(data);
            }
            return allPropertyData;
        }

        private static FieldData? FindFieldAssignment(IPropertySymbol property, Compilation compilation)
        {
            if (property.SetMethod?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is AccessorDeclarationSyntax accessor &&
                // We assume that if there are multiple field assignments in a property
                // then they are all to the same field
                accessor.DescendantNodes().FirstOrDefault(n => n is ExpressionStatementSyntax) is ExpressionStatementSyntax expression &&
                expression.Expression is AssignmentExpressionSyntax assignment &&
                assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return ExtractFieldFromExpression(AccessorKind.Setter, assignment.Left, compilation);
            }

            return null;
        }

        private static FieldData? FindReturnedField(IPropertySymbol property, Compilation compilation)
        {
            // We don't handle properties with multiple returns that return different fields
            if (property.GetMethod?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is AccessorDeclarationSyntax accessor &&
                accessor.DescendantNodes().FirstOrDefault(n => n is ReturnStatementSyntax) is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression != null)
            {
                return ExtractFieldFromExpression(AccessorKind.Getter, returnStatement.Expression, compilation);
            }
            return null;
        }

        private static FieldData? ExtractFieldFromExpression(AccessorKind accessorKind,
            ExpressionSyntax expression,
            Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(expression.SyntaxTree);
            if (semanticModel == null)
            {
                return null;
            }

            var strippedExpression = expression.RemoveParentheses();

            // Check for direct field access: "foo"
            if (strippedExpression is IdentifierNameSyntax &&
                semanticModel.GetSymbolInfo(strippedExpression).Symbol is IFieldSymbol field)
            {
                return new FieldData(accessorKind, field, strippedExpression.GetLocation());
            }
            else
            {
                // Check for "this.foo"
                if (strippedExpression is MemberAccessExpressionSyntax member &&
                    member.Expression is ThisExpressionSyntax thisExpression &&
                    semanticModel.GetSymbolInfo(expression).Symbol is IFieldSymbol field2)
                {
                    return new FieldData(accessorKind, field2, member.Name.GetLocation());
                }
            }

            return null;
        }

        private struct PropertyData
        {
            public PropertyData(IPropertySymbol propertySymbol, FieldData? returned, FieldData? updated)
            {
                this.PropertySymbol = propertySymbol;
                this.FieldReturned = returned;
                this.FieldUpdated = updated;
            }

            public IPropertySymbol PropertySymbol { get; }

            public FieldData? FieldReturned { get; }

            public FieldData? FieldUpdated { get; }
        }

        private enum AccessorKind
        {
            Getter,
            Setter
        }

        private struct FieldData
        {
            public FieldData(AccessorKind accessor, IFieldSymbol field, Location location)
            {
                this.AccessorKind = accessor;
                this.Field = field;
                this.Location = location;
            }

            public AccessorKind AccessorKind { get; }

            public IFieldSymbol Field { get; }

            public Location Location { get; }
        }

        /// <summary>
        /// The rule decides if a property is returning/settings the expected field.
        /// We decide what the expected field name should be based on a fuzzy match
        /// between the field name and the property name.
        /// This class hides the details of matching logic.
        /// </summary>
        private class PropertyToFieldMatcher
        {
            private readonly IDictionary<IFieldSymbol, string> fieldToStandardNameMap;

            public PropertyToFieldMatcher(IEnumerable<IFieldSymbol> fields)
            {
                // Calcuate and cache the standardised versions of the field names to avoid
                // calculating them every time
                this.fieldToStandardNameMap = fields.ToDictionary(f => f, f => GetCanonicalFieldName(f.Name));
            }

            public IFieldSymbol GetSingleMatchingFieldOrNull(IPropertySymbol propertySymbol)
            {
                // We're not caching the property name as only expect to be called once per property
                var standardisedPropertyName = GetCanonicalFieldName(propertySymbol.Name);

                var matchingFields = fieldToStandardNameMap.Keys
                    .Where(k => AreCanonicalNamesEqual(fieldToStandardNameMap[k], standardisedPropertyName));

                if (matchingFields.Count() != 1)
                {
                    return null;
                }
                return matchingFields.Single();
            }

            private static string GetCanonicalFieldName(string name) =>
                name.Replace("_", string.Empty);

            private static bool AreCanonicalNamesEqual(string name1, string name2) =>
                name1.Equals(name2, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
