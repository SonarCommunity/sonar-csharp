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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SonarAnalyzer.Helpers
{
    public class CSharpBuilderPatternCondition : BuilderPatternCondition<InvocationExpressionSyntax>
    {

        public CSharpBuilderPatternCondition(bool constructorIsSafe, params BuilderPatternDescriptor<InvocationExpressionSyntax>[] descriptors) : base(constructorIsSafe, descriptors) { }

        protected override SyntaxNode RemoveParentheses(SyntaxNode node) =>
            node.RemoveParentheses();

        protected override SyntaxNode GetTopMostContainingMethod(SyntaxNode node) =>
            node.GetTopMostContainingMethod();

        protected override SyntaxNode GetExpression(InvocationExpressionSyntax node)
        {
            return node.Expression;
        }

        protected override string GetIdentifierName(InvocationExpressionSyntax node)
        {
            return node.Expression.GetName();
        }

        protected override bool IsMemberAccess(SyntaxNode node, out SyntaxNode memberAccessExpression)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                memberAccessExpression = memberAccess.Expression;
                return true;
            }
            memberAccessExpression = null;
            return false;
        }

        protected override bool IsObjectCreation(SyntaxNode node) =>
            node is ObjectCreationExpressionSyntax;

        protected override bool IsIdentifier(SyntaxNode node, out string identifierName)
        {
            if (node is IdentifierNameSyntax identifier)
            {
                identifierName = identifier.Identifier.ValueText;
                return true;
            }
            identifierName = null;
            return false;
        }

        protected override bool IsAssignmentToIdentifier(SyntaxNode node, string identifierName, out SyntaxNode rightExpression)
        {
            if (node is ExpressionStatementSyntax statement)
            {
                node = statement.Expression;
            }
            if (node is AssignmentExpressionSyntax assignment && assignment.Left.NameIs(identifierName))
            {
                rightExpression = assignment.Right;
                return true;
            }
            rightExpression = null;
            return false;
        }

        protected override bool IsIdentifierDeclaration(SyntaxNode node, string identifierName, out SyntaxNode initializer)
        {
            if (node is LocalDeclarationStatementSyntax declarationStatement
                        && declarationStatement.Declaration.Variables.SingleOrDefault(x => x.Identifier.ValueText == identifierName) is { } declaration)
            {
                initializer = declaration.Initializer?.Value;
                return true;
            }
            initializer = null;
            return false;
        }
    }
}
