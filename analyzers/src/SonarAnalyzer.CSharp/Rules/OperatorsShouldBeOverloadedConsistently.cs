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
    public sealed class OperatorsShouldBeOverloadedConsistently : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S4050";
        private const string MessageFormat = "Provide an implementation for: {0}.";

        private static readonly DiagnosticDescriptor Rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        private static class MethodName
        {
            public const string OperatorPlus = "operator+";
            public const string OperatorMinus = "operator-";
            public const string OperatorMultiply = "operator*";
            public const string OperatorDivide = "operator/";
            public const string OperatorReminder = "operator%";
            public const string OperatorEquals = "operator==";
            public const string OperatorNotEquals = "operator!=";

            public const string ObjectEquals = "Object.Equals";
            public const string ObjectGetHashCode = "Object.GetHashCode";
        }

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var classDeclaration = (ClassDeclarationSyntax)c.Node;
                var classSymbol = (INamedTypeSymbol)c.ContainingSymbol;

                if (classDeclaration.Identifier.IsMissing
                    || !classSymbol.IsPubliclyAccessible())
                {
                    return;
                }

                var missingMethods = FindMissingMethods(classSymbol).ToList();
                if (missingMethods.Count > 0)
                {
                    c.ReportIssue(Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(),
                        missingMethods.ToSentence(quoteWords: true)));
                }
            },
            // This rule is not applicable for records, as for records it is not possible to override the == operator.
            SyntaxKind.ClassDeclaration);

        private static IEnumerable<string> FindMissingMethods(INamedTypeSymbol classSymbol)
        {
            var implementedMethods = GetImplementedMethods(classSymbol).ToHashSet();
            var requiredMethods = new HashSet<string>();

            if (implementedMethods.Contains(MethodName.OperatorPlus)
                || implementedMethods.Contains(MethodName.OperatorMinus)
                || implementedMethods.Contains(MethodName.OperatorMultiply)
                || implementedMethods.Contains(MethodName.OperatorDivide)
                || implementedMethods.Contains(MethodName.OperatorReminder))
            {
                requiredMethods.Add(MethodName.OperatorEquals);
                requiredMethods.Add(MethodName.OperatorNotEquals);
                requiredMethods.Add(MethodName.ObjectEquals);
                requiredMethods.Add(MethodName.ObjectGetHashCode);
            }

            if (implementedMethods.Contains(MethodName.OperatorEquals))
            {
                requiredMethods.Add(MethodName.OperatorNotEquals);
                requiredMethods.Add(MethodName.ObjectEquals);
                requiredMethods.Add(MethodName.ObjectGetHashCode);
            }

            if (implementedMethods.Contains(MethodName.OperatorNotEquals))
            {
                requiredMethods.Add(MethodName.OperatorEquals);
                requiredMethods.Add(MethodName.ObjectEquals);
                requiredMethods.Add(MethodName.ObjectGetHashCode);
            }

            return requiredMethods.Except(implementedMethods);
        }

        private static IEnumerable<string> GetImplementedMethods(INamedTypeSymbol classSymbol)
        {
            var classMethods = classSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsConstructor())
                .ToList();

            if (classMethods.Any(KnownMethods.IsOperatorBinaryPlus))
            {
                yield return MethodName.OperatorPlus;
            }

            if (classMethods.Any(KnownMethods.IsOperatorBinaryMinus))
            {
                yield return MethodName.OperatorMinus;
            }

            if (classMethods.Any(KnownMethods.IsOperatorBinaryMultiply))
            {
                yield return MethodName.OperatorMultiply;
            }

            if (classMethods.Any(KnownMethods.IsOperatorBinaryDivide))
            {
                yield return MethodName.OperatorDivide;
            }

            if (classMethods.Any(KnownMethods.IsOperatorBinaryModulus))
            {
                yield return MethodName.OperatorReminder;
            }

            if (classMethods.Any(KnownMethods.IsOperatorEquals))
            {
                yield return MethodName.OperatorEquals;
            }

            if (classMethods.Any(KnownMethods.IsOperatorNotEquals))
            {
                yield return MethodName.OperatorNotEquals;
            }

            if (classMethods.Any(KnownMethods.IsObjectEquals))
            {
                yield return MethodName.ObjectEquals;
            }

            if (classMethods.Any(KnownMethods.IsObjectGetHashCode))
            {
                yield return MethodName.ObjectGetHashCode;
            }
        }
    }
}
