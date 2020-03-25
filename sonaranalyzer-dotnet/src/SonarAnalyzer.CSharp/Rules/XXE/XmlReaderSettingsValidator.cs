﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2020 SonarSource SA
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
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.XXE
{
    /// <summary>
    /// This class is responsible to check if a XmlReaderSettings node is vulnerable to XXE attacks.
    ///
    /// By default the XmlReaderSettings is safe:
    ///  - before .Net 4.5.2 it has ProhibitDtd set to true (even if the internal XmlResolver is not secure)
    ///  - starting with .Net 4.5.2 XmlResolver is set to null, ProhibitDtd set to true and DtdProcessing set to ignore
    ///
    /// If the properties are modified, in order to be secure, we have to check that either ProhibitDtd is set to true, DtdProcessing set to Ignore or
    /// the internal XmlResolver is secure.
    /// </summary>
    internal class XmlReaderSettingsValidator
    {
        private readonly SemanticModel semanticModel;
        private readonly bool isXmlResolverSafeByDefault;

        public XmlReaderSettingsValidator(SemanticModel semanticModel, NetFrameworkVersion version)
        {
            this.semanticModel = semanticModel;
            this.isXmlResolverSafeByDefault = IsXmlResolverPropertySafeByDefault(version);
        }

        /// <summary>
        /// Checks if a method invocation (e.g. XmlReader.Create) receives a secure instance of XmlReaderSettings.
        /// </summary>
        /// <param name="invocation">A method invocation syntax node (e.g. XmlReader.Create).</param>
        /// <param name="settings">The symbol of the XmlReaderSettings node received as parameter. This is used to check
        /// if certain properties (ProhibitDtd, DtdProcessing or XmlUrlResolver) were modified for the given symbol.</param>
        public bool IsUnsafe(InvocationExpressionSyntax invocation, ISymbol settings)
        {
            // By default ProhibitDtd is 'true' and DtdProcessing is 'ignore'
            var unsafeDtdProcessing = false;
            var unsafeResolver = isXmlResolverSafeByDefault;

            var objectCreation = GetObjectCreation(settings, invocation, semanticModel);
            var objectCreationAssignments = objectCreation.GetInitializerExpressions().OfType<AssignmentExpressionSyntax>();

            var propertyAssignments = GetAssignments(invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>())
                .Where(assignment => IsMemberAccessOnSymbol(assignment.Left, settings, this.semanticModel));

            foreach (var assignment in objectCreationAssignments.Union(propertyAssignments))
            {
                var name = assignment.Left.GetName();

                if (name =="ProhibitDtd" || name == "DtdProcessing")
                {
                    unsafeDtdProcessing = IsXmlResolverDtdProcessingUnsafe(assignment, semanticModel);
                }
                else if (name == "XmlResolver")
                {
                    unsafeResolver = IsXmlResolverAssignmentUnsafe(assignment, semanticModel);
                }
            }

            return unsafeDtdProcessing && unsafeResolver;
        }

        private static bool IsMemberAccessOnSymbol(ExpressionSyntax expression, ISymbol symbol, SemanticModel semanticModel) =>
            expression is MemberAccessExpressionSyntax memberAccess &&
            IsXmlReaderSettings(memberAccess.Expression, semanticModel) &&
            symbol.Equals(semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol);

        private static IEnumerable<AssignmentExpressionSyntax> GetAssignments(SyntaxNode node) =>
            node == null
                ? Enumerable.Empty<AssignmentExpressionSyntax>()
                : node.DescendantNodes().OfType<AssignmentExpressionSyntax>();

        private static bool IsXmlResolverPropertySafeByDefault(NetFrameworkVersion version) =>
            version == NetFrameworkVersion.Probably35 || version == NetFrameworkVersion.Between4And451;

        private static bool IsXmlReaderSettings(ExpressionSyntax expressionSyntax, SemanticModel semanticModel) =>
            semanticModel.GetTypeInfo(expressionSyntax).Type.Is(KnownType.System_Xml_XmlReaderSettings);

        private static ObjectCreationExpressionSyntax GetObjectCreation(ISymbol symbol, InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
            symbol.Locations
                .SelectMany(location => GetDescendantNodes(location, invocation).OfType<ObjectCreationExpressionSyntax>())
                .FirstOrDefault(objectCreation => objectCreation.Initializer != null && IsXmlReaderSettings(objectCreation, semanticModel));

        private static IEnumerable<SyntaxNode> GetDescendantNodes(Location location, SyntaxNode invocation)
        {
            var locationRootNode = location.SourceTree?.GetRoot();
            var invocationRootNode = invocation.SyntaxTree.GetRoot();

            // We don't look for descendants when the location is outside the current context root
            if (locationRootNode != null && locationRootNode != invocationRootNode)
            {
                return Enumerable.Empty<SyntaxNode>();
            }

            // To optimise, we search first for the class constructor, then for the method declaration.
            // If these cannot be found (e.g. fields), we get the root of the syntax tree and search from there.
            var root = locationRootNode?.FindNode(location.SourceSpan) ??
                       invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>() ??
                       invocationRootNode;

            return root.DescendantNodes();
        }

        private static bool IsXmlResolverDtdProcessingUnsafe(AssignmentExpressionSyntax assignment, SemanticModel semanticModel) =>
            semanticModel.GetConstantValue(assignment.Right).Value switch
            {
                false => true, // If ProhibitDtd is set to false the settings will be unsafe (parsing is allowed)
                (int)DtdProcessing.Parse => true,
                _ => false
            };

        private static bool IsXmlResolverAssignmentUnsafe(AssignmentExpressionSyntax assignment, SemanticModel semanticModel)
        {
            if (assignment.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            var type = semanticModel.GetTypeInfo(assignment.Right).Type;
            return type.IsAny(KnownType.System_Xml_XmlUrlResolver, KnownType.System_Xml_Resolvers_XmlPreloadedResolver);
        }
    }
}
