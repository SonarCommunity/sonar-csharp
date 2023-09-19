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
    public sealed class DoNotMarkEnumsWithFlags : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4070";
        private const string MessageFormat = "Remove the 'FlagsAttribute' from this enum.";

        private static readonly DiagnosticDescriptor Rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(
                c =>
                {
                    var enumDeclaration = (EnumDeclarationSyntax)c.Node;
                    var enumSymbol = c.SemanticModel.GetDeclaredSymbol(enumDeclaration);

                    if (!enumDeclaration.HasFlagsAttribute(c.SemanticModel)
                        || enumDeclaration.Identifier.IsMissing
                        || enumSymbol == null)
                    {
                        return;
                    }

                    var membersWithValues = enumSymbol.GetMembers()
                        .OfType<IFieldSymbol>()
                        .Select(member => new { Member = member, Value = GetEnumValueOrDefault(member) })
                        .OrderByDescending(tuple => tuple.Value)
                        .ToList();

                    var allValues = membersWithValues.Select(x => x.Value)
                        .OfType<ulong>()
                        .Distinct()
                        .ToList();

                    var invalidMembers = membersWithValues.Where(tuple => !IsValidFlagValue(tuple.Value, allValues))
                        .Select(tuple => tuple.Member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation())
                        .WhereNotNull()
                        .ToList();

                    if (invalidMembers.Count > 0)
                    {
                        c.ReportIssue(Rule.CreateDiagnostic(c.Compilation, enumDeclaration.Identifier.GetLocation(), additionalLocations: invalidMembers));
                    }
                }, SyntaxKind.EnumDeclaration);

        // The idea of this method is to get rid of invalid values for flags such as negative values and decimals
        private static ulong? GetEnumValueOrDefault(IFieldSymbol enumMember) =>
            enumMember.HasConstantValue && ulong.TryParse(enumMember.ConstantValue.ToString(), out var longValue)
                ? longValue
                : null;

        private static bool IsValidFlagValue(ulong? enumValue, List<ulong> allValues) =>
            enumValue.HasValue && (IsZeroOrPowerOfTwo(enumValue.Value) || IsCombinationOfOtherValues(enumValue.Value, allValues));

        // See https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
        private static bool IsZeroOrPowerOfTwo(ulong value) =>
            (value & (value - 1)) == 0;

        private static bool IsCombinationOfOtherValues(ulong value, List<ulong> otherValues)
        {
            // Assume otherValues is not empty and sorted Z -> A
            if (value > otherValues[0])
            {
                return false;
            }

            var newValue = value;
            foreach (var otherValue in otherValues.SkipWhile(v => value <= v))
            {
                if (otherValue <= newValue)
                {
                    newValue ^= otherValue;
                    if (newValue == 0)
                    {
                        return true;
                    }
                }
            }

            return newValue == 0;
        }
    }
}
