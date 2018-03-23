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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SonarAnalyzer.Helpers
{
    internal static class CSharpSyntaxHelper
    {
        private static readonly ISet<SyntaxKind> BooleanLiterals = new HashSet<SyntaxKind>
        {
            SyntaxKind.TrueLiteralExpression,
            SyntaxKind.FalseLiteralExpression
        };

        public static readonly ExpressionSyntax NullLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        public static readonly ExpressionSyntax FalseLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        public static readonly ExpressionSyntax TrueLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        public static readonly string NameOfKeywordText = SyntaxFacts.GetText(SyntaxKind.NameOfKeyword);

        public static bool HasExactlyNArguments(this InvocationExpressionSyntax invocation, int count)
        {
            return invocation != null &&
                invocation.ArgumentList != null &&
                invocation.ArgumentList.Arguments.Count == count;
        }

        public static ExpressionSyntax RemoveParentheses(this ExpressionSyntax expression)
        {
            var currentExpression = expression;
            var parentheses = expression as ParenthesizedExpressionSyntax;
            while (parentheses != null)
            {
                currentExpression = parentheses.Expression;
                parentheses = currentExpression as ParenthesizedExpressionSyntax;
            }
            return currentExpression;
        }

        public static ExpressionSyntax GetSelfOrTopParenthesizedExpression(this ExpressionSyntax node)
        {
            var current = node;
            var parent = current.Parent as ParenthesizedExpressionSyntax;
            while (parent != null)
            {
                current = parent;
                parent = current.Parent as ParenthesizedExpressionSyntax;
            }
            return current;
        }

        public static bool TryGetAttribute(this SyntaxList<AttributeListSyntax> attributeLists,
            KnownType attributeKnownType, SemanticModel semanticModel, out AttributeSyntax searchedAttribute)
        {
            searchedAttribute = null;

            if (!attributeLists.Any())
            {
                return false;
            }

            foreach (var attribute in attributeLists.SelectMany(attributeList => attributeList.Attributes))
            {
                var attributeType = semanticModel.GetTypeInfo(attribute).Type;

                if (attributeType.Is(attributeKnownType))
                {
                    searchedAttribute = attribute;
                    return true;
                }
            }

            return false;
        }

        public static bool IsOnThis(this ExpressionSyntax expression)
        {
            if (expression is NameSyntax)
            {
                return true;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression.RemoveParentheses().IsKind(SyntaxKind.ThisExpression))
            {
                return true;
            }

            if (expression is ConditionalAccessExpressionSyntax conditionalAccess &&
                conditionalAccess.Expression.RemoveParentheses().IsKind(SyntaxKind.ThisExpression))
            {
                return true;
            }

            return false;
        }

        public static bool IsInNameofCall(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var argumentList = (expression.Parent as ArgumentSyntax)?.Parent as ArgumentListSyntax;
            var nameofCall = argumentList?.Parent as InvocationExpressionSyntax;

            if (nameofCall == null)
            {
                return false;
            }

            return nameofCall.IsNameof(semanticModel);
        }

        public static bool IsNameof(this InvocationExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol calledSymbol)
            {
                return false;
            }

            var nameofIdentifier = (expression?.Expression as IdentifierNameSyntax)?.Identifier;

            return nameofIdentifier.HasValue &&
                (nameofIdentifier.Value.ToString() == NameOfKeywordText);
        }

        public static bool IsStringEmpty(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var memberAccessExpression = expression as MemberAccessExpressionSyntax;
            if (memberAccessExpression == null)
            {
                return false;
            }

            var name = semanticModel.GetSymbolInfo(memberAccessExpression.Name);

            return name.Symbol != null &&
                   name.Symbol.IsInType(KnownType.System_String) &&
                   name.Symbol.Name == nameof(string.Empty);
        }

        public static bool IsAnyKind(this SyntaxNode syntaxNode, ICollection<SyntaxKind> collection)
        {
            return syntaxNode != null && collection.Contains((SyntaxKind)syntaxNode.RawKind);
        }

        public static bool IsAnyKind(this SyntaxToken syntaxToken, ICollection<SyntaxKind> collection)
        {
            return collection.Contains((SyntaxKind)syntaxToken.RawKind);
        }

        public static bool ContainsMethodInvocation(BaseMethodDeclarationSyntax methodDeclarationBase,
            SemanticModel semanticModel,
            Func<InvocationExpressionSyntax, bool> syntaxPredicate, Func<IMethodSymbol, bool> symbolPredicate)
        {
            var childNodes = methodDeclarationBase?.Body?.DescendantNodes();
            if (childNodes == null)
            {
                childNodes = (methodDeclarationBase as MethodDeclarationSyntax)?.ExpressionBody?.DescendantNodes();
            }

            return childNodes != null &&
                ContainsMethodInvocation(childNodes, semanticModel, syntaxPredicate, symbolPredicate);
        }

        private static bool ContainsMethodInvocation(IEnumerable<SyntaxNode> syntaxNodes, SemanticModel semanticModel,
            Func<InvocationExpressionSyntax, bool> syntaxPredicate, Func<IMethodSymbol, bool> symbolPredicate)
        {
            // See issue: https://github.com/SonarSource/sonar-csharp/issues/416
            // Where clause excludes nodes that are not defined on the same SyntaxTree as the
            // SemanticModel (because of partial definition).

            // More details: https://github.com/dotnet/roslyn/issues/18730

            return syntaxNodes
                .OfType<InvocationExpressionSyntax>()
                .Where(syntaxPredicate)
                .Select(e => semanticModel.GetSyntaxTreeSemanticModel(e.Expression).GetSymbolInfo(e.Expression).Symbol)
                .OfType<IMethodSymbol>()
                .Any(symbolPredicate);
        }

        public static SyntaxToken? GetIdentifierOrDefault(this BaseMethodDeclarationSyntax methodDeclaration)
        {
            return (methodDeclaration as MethodDeclarationSyntax)?.Identifier ??
                   (methodDeclaration as ConstructorDeclarationSyntax)?.Identifier ??
                   (methodDeclaration as DestructorDeclarationSyntax)?.Identifier;
        }

        public static Location FindIdentifierLocation(this BaseMethodDeclarationSyntax methodDeclaration)
        {
            return GetIdentifierOrDefault(methodDeclaration)?.GetLocation();
        }

        public static bool IsCatchingAllExceptions(this CatchClauseSyntax catchClause)
        {
            if (catchClause.Declaration == null)
            {
                return true;
            }

            var exceptionTypeName = catchClause.Declaration.Type.GetText().ToString();

            return catchClause.Filter == null &&
                (exceptionTypeName == "Exception" || exceptionTypeName == "System.Exception");
        }

        public static bool IsBooleanConstant(this SyntaxNode syntaxNode, SemanticModel semanticModel)
        {
            if (syntaxNode is MemberAccessExpressionSyntax ||
                syntaxNode is IdentifierNameSyntax)
            {
                var constant = semanticModel.GetConstantValue(syntaxNode);
                return constant.HasValue && constant.Value is bool;
            }
            return false;
        }

        public static bool IsBooleanLiteral(this SyntaxNode syntaxNode)
        {
            return syntaxNode.IsAnyKind(BooleanLiterals);
        }

        /// <summary>
        /// Determines whether the node is being used as part of an expression tree
        /// i.e. whether it is part of lamba being assigned to System.Linq.Expressions.Expression[TDelegate].
        /// This could be a local declaration, an assignment, a field, or a property
        /// </summary>
        public static bool IsInExpressionTree(this SyntaxNode node, SemanticModel semanticModel)
        {
            // Possible ancestors:
            // * VariableDeclarationSyntax (for local variable or field),
            // * PropertyDeclarationSyntax,
            // * SimpleAssigmentExpression

            var potentialExpressionNode = node.Ancestors().FirstOrDefault(ancestor =>
                ancestor is VariableDeclarationSyntax ||
                ancestor is PropertyDeclarationSyntax ||
                ancestor is AssignmentExpressionSyntax);

            SyntaxNode typeIdentifiedNode = null;
            switch (potentialExpressionNode)
            {
                case VariableDeclarationSyntax varDecl:
                    typeIdentifiedNode = varDecl.Type;
                    break;
                case PropertyDeclarationSyntax propDecl:
                    typeIdentifiedNode = propDecl.Type;
                    break;
                case AssignmentExpressionSyntax assignExpr:
                    typeIdentifiedNode = assignExpr.Left;
                    break;
                default:
                    return false;
            }

            return typeIdentifiedNode?.IsKnownType(KnownType.System_Linq_Expressions_Expression_T, semanticModel) ?? false;
        }

        public static bool HasDefaultLabel(this SwitchStatementSyntax node)
        {
            return node.Sections.Any(section => section.Labels.Any(label => label.IsKind(SyntaxKind.DefaultSwitchLabel)));
        }
    }
}
