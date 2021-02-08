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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.Hotspots
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class LooseFilePermissions : HotspotDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2612";
        private const string MessageFormat = "Make sure this permission is safe.";
        private const string Everyone = "Everyone";

        private static readonly HashSet<string> WeakFileAccessPermissions = new HashSet<string>
        {
            "AllPermissions",
            "DefaultPermissions",
            "OtherExecute",
            "OtherWrite",
            "OtherRead",
            "OtherReadWriteExecute"
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager).WithNotConfigurable();

        public LooseFilePermissions() : this(AnalyzerConfiguration.Hotspot) { }

        internal LooseFilePermissions(IAnalyzerConfiguration configuration) : base(configuration)
        {
        }

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationStartAction(c =>
            {
                if (!IsEnabled(c.Options))
                {
                    return;
                }

                c.RegisterSyntaxNodeActionInNonGenerated(VisitInvocations, SyntaxKind.InvocationExpression);

                c.RegisterSyntaxNodeActionInNonGenerated(VisitAssignments, SyntaxKind.IdentifierName);
            });

        private static void VisitAssignments(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;
            if (IsFileAccessPermissions(identifier, context.SemanticModel) && !identifier.IsPartOfBinaryNegationOrCondition())
            {
                context.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, identifier.GetLocation()));
            }
        }

        private static bool IsFileAccessPermissions(SimpleNameSyntax identifierNameSyntax, SemanticModel semanticModel) =>
            WeakFileAccessPermissions.Contains(identifierNameSyntax.Identifier.Text)
            && identifierNameSyntax.IsKnownType(KnownType.Mono_Unix_FileAccessPermissions, semanticModel);

        private static void VisitInvocations(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!invocation.IsMemberAccessOnKnownType("SetAccessRule", KnownType.System_Security_AccessControl_FileSystemSecurity, context.SemanticModel) &&
                !invocation.IsMemberAccessOnKnownType("AddAccessRule", KnownType.System_Security_AccessControl_FileSystemSecurity, context.SemanticModel))
            {
                return;
            }

            if (GetObjectCreation(invocation, context.SemanticModel) is { } objectCreation)
            {
                var invocationLocation = invocation.GetLocation();
                var secondaryLocation = objectCreation.GetLocation();

                var diagnostic = invocationLocation.GetLineSpan().StartLinePosition.Line == secondaryLocation.GetLineSpan().StartLinePosition.Line
                    ? Diagnostic.Create(Rule, invocationLocation)
                    : Diagnostic.Create(Rule, invocationLocation, additionalLocations: new[] {secondaryLocation});

                context.ReportDiagnosticWhenActive(diagnostic);
            }
        }

        private static ObjectCreationExpressionSyntax GetObjectCreation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var accessRuleSyntaxNode = GetVulnerableFileSystemAccessRule(invocation.DescendantNodes());
            if (accessRuleSyntaxNode != null)
            {
                return accessRuleSyntaxNode;
            }

            var accessRuleSymbol = invocation.GetArgumentSymbolsOfKnownType(KnownType.System_Security_AccessControl_FileSystemAccessRule, semanticModel).FirstOrDefault();
            if (accessRuleSymbol == null || accessRuleSymbol is IMethodSymbol)
            {
                return null;
            }

            return GetVulnerableFileSystemAccessRule(accessRuleSymbol.GetLocationNodes(invocation));

            ObjectCreationExpressionSyntax GetVulnerableFileSystemAccessRule(IEnumerable<SyntaxNode> nodes) =>
                nodes.OfType<ObjectCreationExpressionSyntax>()
                     .FirstOrDefault(objectCreation => IsFileSystemAccessRuleForEveryoneWithAllow(objectCreation, semanticModel));
        }

        private static bool IsFileSystemAccessRuleForEveryoneWithAllow(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel) =>
            objectCreation.IsKnownType(KnownType.System_Security_AccessControl_FileSystemAccessRule, semanticModel)
            && objectCreation.ArgumentList is { } argumentList
            && IsEveryone(argumentList.Arguments.First().Expression, semanticModel)
            && semanticModel.GetConstantValue(argumentList.Arguments.Last().Expression) is {HasValue: true, Value: 0};

        private static bool IsEveryone(SyntaxNode syntaxNode, SemanticModel semanticModel)
        {
            if (semanticModel.GetConstantValue(syntaxNode) is {HasValue: true, Value: Everyone})
            {
                return true;
            }

            return syntaxNode.DescendantNodesAndSelf()
                             .OfType<ObjectCreationExpressionSyntax>()
                             .Any(objectCreation => IsNTAccountWithEveryone(objectCreation, semanticModel) || IsSecurityIdentifierWithEveryone(objectCreation, semanticModel));
        }

        private static bool IsNTAccountWithEveryone(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel) =>
            objectCreation.IsKnownType(KnownType.System_Security_Principal_NTAccount, semanticModel)
            && objectCreation.ArgumentList is { } argumentList
            && semanticModel.GetConstantValue(argumentList.Arguments.Last().Expression) is { HasValue: true, Value: Everyone};

        private static bool IsSecurityIdentifierWithEveryone(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel) =>
            objectCreation.IsKnownType(KnownType.System_Security_Principal_SecurityIdentifier, semanticModel)
            && objectCreation.ArgumentList is { } argumentList
            && semanticModel.GetConstantValue(argumentList.Arguments.First().Expression) is { HasValue: true, Value: 1};
    }
}
