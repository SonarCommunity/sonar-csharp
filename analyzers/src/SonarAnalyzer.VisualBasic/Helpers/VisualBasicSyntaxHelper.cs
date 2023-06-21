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

namespace SonarAnalyzer.Helpers;

internal static class VisualBasicSyntaxHelper
{
    private static readonly SyntaxKind[] LiteralSyntaxKinds =
        new[]
        {
            SyntaxKind.CharacterLiteralExpression,
            SyntaxKind.FalseLiteralExpression,
            SyntaxKind.NothingLiteralExpression,
            SyntaxKind.NumericLiteralExpression,
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.TrueLiteralExpression,
        };

    public static SyntaxNode GetTopMostContainingMethod(this SyntaxNode node) =>
        node.AncestorsAndSelf().LastOrDefault(ancestor => ancestor is MethodBaseSyntax || ancestor is PropertyBlockSyntax);

    public static SyntaxNode RemoveParentheses(this SyntaxNode expression)
    {
        var current = expression;
        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }
        return current;
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

    public static ExpressionSyntax GetSelfOrTopParenthesizedExpression(this ExpressionSyntax expression) =>
        (ExpressionSyntax)GetSelfOrTopParenthesizedExpression((SyntaxNode)expression);

    public static SyntaxNode GetFirstNonParenthesizedParent(this SyntaxNode node) =>
        node.GetSelfOrTopParenthesizedExpression().Parent;

    #region Statement

    public static StatementSyntax GetPrecedingStatement(this StatementSyntax currentStatement)
    {
        var children = currentStatement.Parent.ChildNodes().ToList();
        var index = children.IndexOf(currentStatement);
        return index == 0 ? null : children[index - 1] as StatementSyntax;
    }

    public static StatementSyntax GetSucceedingStatement(this StatementSyntax currentStatement)
    {
        var children = currentStatement.Parent.ChildNodes().ToList();
        var index = children.IndexOf(currentStatement);
        return index == children.Count - 1 ? null : children[index + 1] as StatementSyntax;
    }

    #endregion Statement

    public static bool IsNothingLiteral(this SyntaxNode syntaxNode) =>
        syntaxNode != null && syntaxNode.IsKind(SyntaxKind.NothingLiteralExpression);

    public static bool IsAnyKind(this SyntaxNode syntaxNode, params SyntaxKind[] syntaxKinds) =>
       syntaxNode != null && syntaxKinds.Contains((SyntaxKind)syntaxNode.RawKind);

    public static bool IsAnyKind(this SyntaxToken syntaxToken, ISet<SyntaxKind> collection) =>
        collection.Contains((SyntaxKind)syntaxToken.RawKind);

    public static bool IsAnyKind(this SyntaxNode syntaxNode, ISet<SyntaxKind> collection) =>
        syntaxNode != null && collection.Contains((SyntaxKind)syntaxNode.RawKind);

    public static bool IsAnyKind(this SyntaxToken syntaxToken, params SyntaxKind[] syntaxKinds) =>
        syntaxKinds.Contains((SyntaxKind)syntaxToken.RawKind);

    public static bool IsAnyKind(this SyntaxTrivia syntaxTrivia, params SyntaxKind[] syntaxKinds) =>
        syntaxKinds.Contains((SyntaxKind)syntaxTrivia.RawKind);

    public static bool AnyOfKind(this IEnumerable<SyntaxNode> nodes, SyntaxKind kind) =>
        nodes.Any(n => n.RawKind == (int)kind);

    public static SyntaxToken? GetMethodCallIdentifier(this InvocationExpressionSyntax invocation)
    {
        if (invocation == null ||
            invocation.Expression == null)
        {
            return null;
        }

        var expressionType = invocation.Expression.Kind();
        // in vb.net when using the null - conditional operator (e.g.handle?.IsClosed), the parser
        // will generate a SimpleMemberAccessExpression and not a MemberBindingExpressionSyntax like for C#
        switch (expressionType)
        {
            case SyntaxKind.IdentifierName:
                return ((IdentifierNameSyntax)invocation.Expression).Identifier;
            case SyntaxKind.SimpleMemberAccessExpression:
                return ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier;
            default:
                return null;
        }
    }
    public static bool IsMethodInvocation(this InvocationExpressionSyntax expression, KnownType type, string methodName, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol methodSymbol &&
        methodSymbol.IsInType(type) &&
        // vbnet is case insensitive
        methodName.Equals(methodSymbol.Name, System.StringComparison.InvariantCultureIgnoreCase);

    public static bool IsOnBase(this ExpressionSyntax expression) =>
        IsOn(expression, SyntaxKind.MyBaseExpression);

    private static bool IsOn(this ExpressionSyntax expression, SyntaxKind onKind)
    {
        switch (expression?.Kind())
        {
            case SyntaxKind.InvocationExpression:
                return IsOn(((InvocationExpressionSyntax)expression).Expression, onKind);

            case SyntaxKind.GlobalName:
            case SyntaxKind.GenericName:
            case SyntaxKind.IdentifierName:
            case SyntaxKind.QualifiedName:
                // This is a simplification as we don't check where the method is defined (so this could be this or base)
                return true;

            case SyntaxKind.SimpleMemberAccessExpression:
                return ((MemberAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

            case SyntaxKind.ConditionalAccessExpression:
                return ((ConditionalAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

            default:
                return false;
        }
    }

    public static SyntaxToken? GetIdentifier(this SyntaxNode node) =>
        node?.RemoveParentheses() switch
        {
            AttributeSyntax x => x.Name?.GetIdentifier(),
            ClassBlockSyntax x => x.ClassStatement.Identifier,
            ClassStatementSyntax x => x.Identifier,
            IdentifierNameSyntax x => x.Identifier,
            MemberAccessExpressionSyntax x => x.Name.Identifier,
            MethodBlockSyntax x => x.SubOrFunctionStatement?.GetIdentifier(),
            MethodStatementSyntax x => x.Identifier,
            ModuleBlockSyntax x => x.ModuleStatement.Identifier,
            EnumStatementSyntax x => x.Identifier,
            EnumMemberDeclarationSyntax x => x.Identifier,
            InvocationExpressionSyntax x => x.Expression?.GetIdentifier(),
            ModifiedIdentifierSyntax x => x.Identifier,
            PredefinedTypeSyntax x => x.Keyword,
            ParameterSyntax x => x.Identifier?.GetIdentifier(),
            PropertyStatementSyntax x => x.Identifier,
            SimpleArgumentSyntax x => x.NameColonEquals?.Name.Identifier,
            SimpleNameSyntax x => x.Identifier,
            StructureBlockSyntax x => x.StructureStatement.Identifier,
            QualifiedNameSyntax x => x.Right.Identifier,
            _ => null,
        };

    public static string GetName(this SyntaxNode expression) =>
        expression.GetIdentifier()?.ValueText ?? string.Empty;

    public static bool NameIs(this ExpressionSyntax expression, string name) =>
        expression.GetName().Equals(name, StringComparison.InvariantCultureIgnoreCase);

    public static bool HasConstantValue(this ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression.RemoveParentheses().IsAnyKind(LiteralSyntaxKinds) || expression.FindConstantValue(semanticModel) != null;

    public static string StringValue(this SyntaxNode node, SemanticModel semanticModel) =>
        node switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
            InterpolatedStringExpressionSyntax expression => expression.TryGetInterpolatedTextValue(semanticModel, out var interpolatedValue) ? interpolatedValue : expression.GetContentsText(),
            _ => null
        };

    public static bool IsLeftSideOfAssignment(this ExpressionSyntax expression)
    {
        var topParenthesizedExpression = expression.GetSelfOrTopParenthesizedExpression();
        return topParenthesizedExpression.Parent.IsKind(SyntaxKind.SimpleAssignmentStatement) &&
            topParenthesizedExpression.Parent is AssignmentStatementSyntax assignment &&
            assignment.Left == topParenthesizedExpression;
    }

    public static bool IsComment(this SyntaxTrivia trivia)
    {
        switch (trivia.Kind())
        {
            case SyntaxKind.CommentTrivia:
            case SyntaxKind.DocumentationCommentExteriorTrivia:
            case SyntaxKind.DocumentationCommentTrivia:
                return true;

            default:
                return false;
        }
    }

    public static Location FindIdentifierLocation(this MethodBlockBaseSyntax methodBlockBase) =>
        GetIdentifierOrDefault(methodBlockBase)?.GetLocation();

    public static SyntaxToken? GetIdentifierOrDefault(this MethodBlockBaseSyntax methodBlockBase)
    {
        var blockStatement = methodBlockBase?.BlockStatement;

        switch (blockStatement?.Kind())
        {
            case SyntaxKind.SubNewStatement:
                return (blockStatement as SubNewStatementSyntax)?.NewKeyword;

            case SyntaxKind.FunctionStatement:
            case SyntaxKind.SubStatement:
                return (blockStatement as MethodStatementSyntax)?.Identifier;

            default:
                return null;
        }
    }

    public static string GetIdentifierText(this MethodBlockSyntax method)
        => method.SubOrFunctionStatement.Identifier.ValueText;

    public static SeparatedSyntaxList<ParameterSyntax>? GetParameters(this MethodBlockSyntax method)
        => method.BlockStatement?.ParameterList?.Parameters;

    public static ExpressionSyntax Get(this ArgumentListSyntax argumentList, int index) =>
        argumentList != null && argumentList.Arguments.Count > index
            ? argumentList.Arguments[index].GetExpression().RemoveParentheses()
            : null;

    /// <summary>
    /// Returns argument expressions for given parameter.
    ///
    /// There can be zero, one or more results based on parameter type (Optional or ParamArray/params).
    /// </summary>
    public static ImmutableArray<SyntaxNode> ArgumentValuesForParameter(SemanticModel semanticModel, ArgumentListSyntax argumentList, string parameterName) =>
        argumentList != null
            && new VisualBasicMethodParameterLookup(argumentList, semanticModel).TryGetSyntax(parameterName, out var expressions)
                ? expressions
                : ImmutableArray<SyntaxNode>.Empty;
}
