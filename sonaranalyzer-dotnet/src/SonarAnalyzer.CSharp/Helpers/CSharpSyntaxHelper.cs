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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.ShimLayer.CSharp;

namespace SonarAnalyzer.Helpers
{
    internal static class CSharpSyntaxHelper
    {
        public static readonly ExpressionSyntax NullLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        public static readonly ExpressionSyntax FalseLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        public static readonly ExpressionSyntax TrueLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        public static readonly string NameOfKeywordText =
            SyntaxFacts.GetText(SyntaxKind.NameOfKeyword);

        public static bool AnyOfKind(this IEnumerable<SyntaxNode> nodes, SyntaxKind kind) =>
            nodes.Any(n => n.RawKind == (int)kind);

        public static bool AnyOfKind(this IEnumerable<SyntaxToken> tokens, SyntaxKind kind) =>
            tokens.Any(n => n.RawKind == (int)kind);

        public static bool HasExactlyNArguments(this InvocationExpressionSyntax invocation, int count)
        {
            return invocation != null &&
                invocation.ArgumentList != null &&
                invocation.ArgumentList.Arguments.Count == count;
        }

        public static SyntaxNode RemoveParentheses(this SyntaxNode expression)
        {
            var currentExpression = expression;
            while (currentExpression?.IsKind(SyntaxKind.ParenthesizedExpression) ?? false)
            {
                currentExpression = ((ParenthesizedExpressionSyntax)currentExpression).Expression;
            }
            return currentExpression;
        }

        public static ExpressionSyntax RemoveParentheses(this ExpressionSyntax expression) =>
            (ExpressionSyntax)RemoveParentheses((SyntaxNode)expression);

        public static SyntaxNode GetSelfOrTopParenthesizedExpression(this SyntaxNode node)
        {
            var current = node;
            while (current?.Parent?.IsKind(SyntaxKind.ParenthesizedExpression) ?? false)
            {
                current = current.Parent;
            }
            return current;
        }

        public static ExpressionSyntax GetSelfOrTopParenthesizedExpression(this ExpressionSyntax node) =>
             (ExpressionSyntax)GetSelfOrTopParenthesizedExpression((SyntaxNode)node);

        public static SyntaxNode GetFirstNonParenthesizedParent(this SyntaxNode node) =>
            node.GetSelfOrTopParenthesizedExpression().Parent;

        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists,
            KnownType attributeKnownType, SemanticModel semanticModel) =>
            attributeLists.SelectMany(list => list.Attributes)
                .Where(a => semanticModel.GetTypeInfo(a).Type.Is(attributeKnownType));

        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists,
            ImmutableArray<KnownType> attributeKnownTypes, SemanticModel semanticModel) =>
            attributeLists.SelectMany(list => list.Attributes)
                .Where(a => semanticModel.GetTypeInfo(a).Type.IsAny(attributeKnownTypes));

        public static bool IsOnThis(this ExpressionSyntax expression) =>
            IsOn(expression, SyntaxKind.ThisExpression);

        public static bool IsOnBase(this ExpressionSyntax expression) =>
            IsOn(expression, SyntaxKind.BaseExpression);

        private static bool IsOn(this ExpressionSyntax expression, SyntaxKind onKind)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    return IsOn(((InvocationExpressionSyntax)expression).Expression, onKind);

                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.GenericName:
                case SyntaxKind.IdentifierName:
                case SyntaxKind.QualifiedName:
                    // This is a simplification as we don't check where the method is defined (so this could be this or base)
                    return true;

                case SyntaxKind.PointerMemberAccessExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

                case SyntaxKind.ConditionalAccessExpression:
                    return ((ConditionalAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

                default:
                    return false;
            }
        }

        public static bool IsInNameofCall(this ExpressionSyntax expression, SemanticModel semanticModel) =>
            expression.Parent != null &&
            expression.Parent.IsKind(SyntaxKind.Argument) &&
            expression.Parent.Parent != null &&
            expression.Parent.Parent.IsKind(SyntaxKind.ArgumentList) &&
            expression.Parent.Parent.Parent != null &&
            expression.Parent.Parent.Parent.IsKind(SyntaxKind.InvocationExpression) &&
            ((InvocationExpressionSyntax)expression.Parent.Parent.Parent).IsNameof(semanticModel);

        public static bool IsNameof(this InvocationExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression == null ||
                !expression.Expression.IsKind(SyntaxKind.IdentifierName) ||
                semanticModel.GetSymbolInfo(expression).Symbol?.Kind == SymbolKind.Method)
            {
                return false;
            }

            return ((IdentifierNameSyntax)expression.Expression).Identifier.ToString() == NameOfKeywordText;
        }

        public static bool IsStringEmpty(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (!expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                !expression.IsKind(SyntaxKind.PointerMemberAccessExpression))
            {
                return false;
            }

            var nameSymbolInfo = semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)expression).Name);

            return nameSymbolInfo.Symbol != null &&
                   nameSymbolInfo.Symbol.IsInType(KnownType.System_String) &&
                   nameSymbolInfo.Symbol.Name == nameof(string.Empty);
        }

        public static bool IsAnyKind(this SyntaxNode syntaxNode, ISet<SyntaxKind> syntaxKinds) =>
            syntaxNode != null && syntaxKinds.Contains((SyntaxKind)syntaxNode.RawKind);

        public static bool IsAnyKind(this SyntaxToken syntaxToken, ISet<SyntaxKind> syntaxKinds) =>
            syntaxKinds.Contains((SyntaxKind)syntaxToken.RawKind);

        public static bool ContainsMethodInvocation(this BaseMethodDeclarationSyntax methodDeclarationBase,
            SemanticModel semanticModel,
            Func<InvocationExpressionSyntax, bool> syntaxPredicate, Func<IMethodSymbol, bool> symbolPredicate)
        {
            var childNodes = methodDeclarationBase?.Body?.DescendantNodes()
                ?? methodDeclarationBase?.ExpressionBody()?.DescendantNodes()
                ?? Enumerable.Empty<SyntaxNode>();

            // See issue: https://github.com/SonarSource/sonar-csharp/issues/416
            // Where clause excludes nodes that are not defined on the same SyntaxTree as the SemanticModel
            // (because of partial definition).
            // More details: https://github.com/dotnet/roslyn/issues/18730
            return childNodes
                .OfType<InvocationExpressionSyntax>()
                .Where(syntaxPredicate)
                .Select(e => e.Expression.SyntaxTree.GetSemanticModelOrDefault(semanticModel)?.GetSymbolInfo(e.Expression).Symbol)
                .OfType<IMethodSymbol>()
                .Any(symbolPredicate);
        }

        public static SyntaxToken? GetIdentifierOrDefault(this BaseMethodDeclarationSyntax methodDeclaration)
        {
            switch (methodDeclaration.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)methodDeclaration).Identifier;

                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)methodDeclaration).Identifier;

                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)methodDeclaration).Identifier;

                default:
                    return null;
            }
        }

        public static SyntaxToken? GetMethodCallIdentifier(this InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return null;
            }
            var expression = invocation.Expression;
            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                    // method()
                    return ((IdentifierNameSyntax)expression).Identifier;

                case SyntaxKind.SimpleMemberAccessExpression:
                    // foo.method()
                    return ((MemberAccessExpressionSyntax)expression).Name.Identifier;

                case SyntaxKind.MemberBindingExpression:
                    // foo?.method()
                    return ((MemberBindingExpressionSyntax)expression).Name.Identifier;

                default:
                    return null;
            }
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

        /// <summary>
        /// Determines whether the node is being used as part of an expression tree
        /// i.e. whether it is part of lambda being assigned to System.Linq.Expressions.Expression[TDelegate].
        /// This could be a local declaration, an assignment, a field, or a property
        /// </summary>
        public static bool IsInExpressionTree(this SyntaxNode node, SemanticModel semanticModel)
        {
            // Possible ancestors:
            // * VariableDeclarationSyntax (for local variable or field),
            // * PropertyDeclarationSyntax,
            // * SimpleAssigmentExpression
            return node.Ancestors()
                .Select(GetExpressionTree)
                .FirstOrDefault(tuple => tuple.Item1)
                ?.Item2?.IsKnownType(KnownType.System_Linq_Expressions_Expression_T, semanticModel)
                ?? false;

            Tuple<bool, SyntaxNode> GetExpressionTree(SyntaxNode syntaxNode)
            {
                switch (syntaxNode.Kind())
                {
                    case SyntaxKind.VariableDeclaration:
                        return new Tuple<bool, SyntaxNode>(true, ((VariableDeclarationSyntax)syntaxNode).Type);

                    case SyntaxKind.PropertyDeclaration:
                        return new Tuple<bool, SyntaxNode>(true, ((PropertyDeclarationSyntax)syntaxNode).Type);

                    case SyntaxKind.AddAssignmentExpression:
                    case SyntaxKind.AndAssignmentExpression:
                    case SyntaxKind.DivideAssignmentExpression:
                    case SyntaxKind.ExclusiveOrAssignmentExpression:
                    case SyntaxKind.LeftShiftAssignmentExpression:
                    case SyntaxKind.ModuloAssignmentExpression:
                    case SyntaxKind.MultiplyAssignmentExpression:
                    case SyntaxKind.OrAssignmentExpression:
                    case SyntaxKind.RightShiftAssignmentExpression:
                    case SyntaxKind.SimpleAssignmentExpression:
                    case SyntaxKind.SubtractAssignmentExpression:
                        return new Tuple<bool, SyntaxNode>(true, ((AssignmentExpressionSyntax)syntaxNode).Left);

                    default:
                        return new Tuple<bool, SyntaxNode>(false, null);
                }
            }
        }

        public static bool HasDefaultLabel(this SwitchStatementSyntax node) =>
            GetDefaultLabelSectionIndex(node) >= 0;

        public static int GetDefaultLabelSectionIndex(this SwitchStatementSyntax node) =>
            node.Sections.IndexOf(section => section.Labels.AnyOfKind(SyntaxKind.DefaultSwitchLabel));

        public static bool HasBodyOrExpressionBody(this AccessorDeclarationSyntax node) =>
            node.Body != null || node.ExpressionBody() != null;

        public static bool HasBodyOrExpressionBody(this BaseMethodDeclarationSyntax node) =>
            node?.Body != null || node?.ExpressionBody() != null;
    }
}
