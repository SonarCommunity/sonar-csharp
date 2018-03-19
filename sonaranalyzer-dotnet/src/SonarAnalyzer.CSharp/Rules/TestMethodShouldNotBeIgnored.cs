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
    public sealed class TestMethodShouldNotBeIgnored : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1607";
        private const string MessageFormat = "Either remove this 'Ignore' attribute or add an explanation about why " +
            "this test is ignored.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        private static readonly ISet<KnownType> TrackedTestMethodAttributes = new HashSet<KnownType>
        {
            KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_TestMethodAttribute,
            KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_TestClassAttribute,
            KnownType.NUnit_Framework_TestAttribute,
            KnownType.NUnit_Framework_TestCaseAttribute,
            KnownType.NUnit_Framework_TestCaseSourceAttribute,
            KnownType.Xunit_FactAttribute,
            KnownType.Xunit_TheoryAttribute
        };

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                if (!c.IsTest())
                {
                    return;
                }

                var attribute = (AttributeSyntax)c.Node;
                if (HasReasonPhrase(attribute) ||
                    HasTrailingComment(attribute) ||
                    !IsMsTestIgnoreAttribute(attribute, c.SemanticModel))
                {
                    return;
                }

                var attributeTarget = attribute.Parent?.Parent;
                if (attributeTarget == null)
                {
                    return;
                }

                var attributes = GetAllAttributes(attributeTarget, c.SemanticModel);

                if (attributes.Any(IsTestOrTestClassAttribute) &&
                    !attributes.Any(IsWorkItemAttribute))
                {
                    c.ReportDiagnosticWhenActive(Diagnostic.Create(rule, attribute.GetLocation()));
                }
            },
            SyntaxKind.Attribute);
        }

        private IEnumerable<AttributeData> GetAllAttributes(SyntaxNode syntaxNode, SemanticModel semanticModel)
        {
            var testMethodOrClass = semanticModel.GetDeclaredSymbol(syntaxNode);

            return testMethodOrClass == null
                ? Enumerable.Empty<AttributeData>()
                : testMethodOrClass.GetAttributes();
        }

        private bool HasReasonPhrase(AttributeSyntax ignoreAttributeSyntax) =>
            ignoreAttributeSyntax.ArgumentList?.Arguments.Count > 0; // Any ctor argument counts are reason phrase

        private static bool HasTrailingComment(SyntaxNode ignoreAttributeSyntax) =>
            ignoreAttributeSyntax.Parent.GetTrailingTrivia()
                .Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia));

        private static bool IsWorkItemAttribute(AttributeData a) =>
            a.AttributeClass.Is(KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_WorkItemAttribute);

        private static bool IsMsTestIgnoreAttribute(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);

            var attributeConstructor = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            return attributeConstructor != null
                && attributeConstructor.ContainingType
                        .Is(KnownType.Microsoft_VisualStudio_TestTools_UnitTesting_IgnoreAttribute);
        }

        private static bool IsTestOrTestClassAttribute(AttributeData a) =>
            a.AttributeClass.IsAny(TrackedTestMethodAttributes);
    }
}
