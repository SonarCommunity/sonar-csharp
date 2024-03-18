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

using System.CodeDom.Compiler;

namespace SonarAnalyzer.Extensions
{
    [GeneratedCode("Copied and converted from Roslyn", "5a1cc5f83e4baba57f0355a685a5d1f487bfac66")]
    internal static class ExpressionSyntaxExtensions
    {
        // Copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L362
        public static bool IsWrittenTo(this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression == null)
                return false;

            if (expression.IsOnlyWrittenTo())
                return true;

            if (expression.IsRightSideOfDot())
                expression = expression.Parent as ExpressionSyntax;

            if (expression != null)
            {
                if (expression.IsInRefContext(semanticModel, cancellationToken))
                    return true;

                if (expression.Parent is AssignmentStatementSyntax)
                {
                    var assignmentStatement = (AssignmentStatementSyntax)expression.Parent;
                    if (expression == assignmentStatement.Left)
                        return true;
                }

                if (expression.IsChildNode<NamedFieldInitializerSyntax>(n => n.Name))
                    return true;

                // Extension method with a 'ref' parameter can write to the value it is called on.
                if (expression.Parent is MemberAccessExpressionSyntax)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)expression.Parent;
                    if (memberAccess.Expression == expression)
                    {
                        var method = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol as IMethodSymbol;
                        if (method != null)
                        {
                            if (method.MethodKind == MethodKind.ReducedExtension && method.ReducedFrom.Parameters.Length > 0 && method.ReducedFrom.Parameters.First().RefKind == RefKind.Ref)
                                return true;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        // copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L325
        public static bool IsOnlyWrittenTo(this ExpressionSyntax expression)
        {
            if (expression.IsRightSideOfDot())
                expression = expression.Parent as ExpressionSyntax;

            if (expression != null)
            {
                // Sonar: IsInOutContext deleted because not relevant for VB
                if (expression.IsParentKind(SyntaxKind.SimpleAssignmentStatement))
                {
                    var assignmentStatement = (AssignmentStatementSyntax)expression.Parent;
                    if (expression == assignmentStatement.Left)
                        return true;
                }

                if (expression.IsParentKind(SyntaxKind.NameColonEquals) && expression.Parent.IsParentKind(SyntaxKind.SimpleArgument))

                    // <C(Prop:=1)>
                    // this is only a write to Prop
                    return true;

                if (expression.IsChildNode<NamedFieldInitializerSyntax>(n => n.Name))
                    return true;

                return false;
            }

            return false;
        }

        // Copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L73
        public static bool IsRightSideOfDot(this ExpressionSyntax expression)
        {
            return expression.IsSimpleMemberAccessExpressionName() || expression.IsRightSideOfQualifiedName();
        }

        // Copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L56
        public static bool IsSimpleMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) && ((MemberAccessExpressionSyntax)expression.Parent).Name == expression;
        }

        // Copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L78
        public static bool IsRightSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Right == expression;
        }

        // Copied and converted from
        // https://github.com/dotnet/roslyn/blob/5a1cc5f83e4baba57f0355a685a5d1f487bfac66/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/VisualBasic/Extensions/ExpressionSyntaxExtensions.vb#L277
        public static bool IsInRefContext(this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var simpleArgument = expression?.Parent as SimpleArgumentSyntax;

            if (simpleArgument == null)
                return false;
            else if (simpleArgument.IsNamed)
            {
                var info = semanticModel.GetSymbolInfo(simpleArgument.NameColonEquals.Name, cancellationToken);

                var parameter = info.Symbol as IParameterSymbol;
                return parameter != null && parameter.RefKind != RefKind.None;
            }
            else
            {
                var argumentList = simpleArgument.Parent as ArgumentListSyntax;

                if (argumentList != null)
                {
                    var parent = argumentList.Parent;
                    var index = argumentList.Arguments.IndexOf(simpleArgument);

                    var info = semanticModel.GetSymbolInfo(parent, cancellationToken);
                    var symbol = info.Symbol;

                    if (symbol is IMethodSymbol)
                    {
                        var method = (IMethodSymbol)symbol;
                        if (index < method.Parameters.Length)
                            return method.Parameters[index].RefKind != RefKind.None;
                    }
                    else if (symbol is IPropertySymbol)
                    {
                        var prop = (IPropertySymbol)symbol;
                        if (index < prop.Parameters.Length)
                            return prop.Parameters[index].RefKind != RefKind.None;
                    }
                }
            }

            return false;
        }
    }
}
