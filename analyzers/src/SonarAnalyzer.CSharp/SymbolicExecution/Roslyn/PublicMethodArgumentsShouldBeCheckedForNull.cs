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

using SonarAnalyzer.SymbolicExecution.Constraints;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks.CSharp;

public class PublicMethodArgumentsShouldBeCheckedForNull : SymbolicRuleCheck
{
    private const string DiagnosticId = "S3900";
    private const string MessageFormat = "{0}";

    internal static readonly DiagnosticDescriptor S3900 = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    protected override DiagnosticDescriptor Rule => S3900;

    public override bool ShouldExecute()
    {
        return IsAccessibleFromOtherAssemblies(Node)
               && (IsSupportedMethod(Node) || IsSupportedPropertyAccessor(Node));

        static bool IsSupportedMethod(SyntaxNode node) =>
            node is BaseMethodDeclarationSyntax { ParameterList.Parameters.Count: > 0 } method
            && MethodDereferencesArguments(method);

        static bool IsSupportedPropertyAccessor(SyntaxNode node) =>
            node is AccessorDeclarationSyntax { RawKind: (int)SyntaxKind.SetAccessorDeclaration or (int)SyntaxKindEx.InitAccessorDeclaration }
            && IsPropertyAccessorAccessibleFromOtherAssemblies(Modifiers(node));

        static SyntaxTokenList Modifiers(SyntaxNode node) =>
            node switch
            {
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                MemberDeclarationSyntax member => member.Modifiers(),
                _ => default
            };

        static bool IsAccessibleFromOtherAssemblies(SyntaxNode node) =>
            node.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault() is { } containingMember
            && node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault() is { } containingType
            && IsMemberAccessibleFromOtherAssemblies(Modifiers(containingMember), containingType)
            && IsTypeAccessibleFromOtherAssemblies(containingType.Modifiers);

        static bool IsMemberAccessibleFromOtherAssemblies(SyntaxTokenList modifiers, BaseTypeDeclarationSyntax containingType) =>
            modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword))
            || (modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword)) && !modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword)))
            || (containingType is InterfaceDeclarationSyntax && HasNoDeclaredAccessabilityModifier(modifiers));

        static bool IsPropertyAccessorAccessibleFromOtherAssemblies(SyntaxTokenList modifiers) =>
            modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword))
            || (modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword)) && !modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword)))
            || HasNoDeclaredAccessabilityModifier(modifiers);

        static bool IsTypeAccessibleFromOtherAssemblies(SyntaxTokenList modifiers) =>
            modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword))
            || (modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword)) && !modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword)));

        static bool HasNoDeclaredAccessabilityModifier(SyntaxTokenList modifiers) =>
            !modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword)
                                || x.IsKind(SyntaxKind.ProtectedKeyword)
                                || x.IsKind(SyntaxKind.InternalKeyword)
                                || x.IsKind(SyntaxKind.PublicKeyword));

        static bool MethodDereferencesArguments(BaseMethodDeclarationSyntax method)
        {
            var argumentNames = method.ParameterList.Parameters.Select(x => x.Identifier.ValueText).ToArray();
            var walker = new ArgumentDereferenceWalker(argumentNames);
            walker.SafeVisit(method);
            return walker.DereferencesMethodArguments;
        }
    }

    private sealed class ArgumentDereferenceWalker : SafeCSharpSyntaxWalker
    {
        private readonly string[] argumentNames;

        public bool DereferencesMethodArguments { get; private set; }

        public ArgumentDereferenceWalker(string[] argumentNames) =>
            this.argumentNames = argumentNames;

        public override void Visit(SyntaxNode node)
        {
            if (!DereferencesMethodArguments)
            {
                base.Visit(node);
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node) =>
            DereferencesMethodArguments |=
                argumentNames.Contains(node.Identifier.ValueText)
                && node.Ancestors().Any(x => x.IsAnyKind(
                    SyntaxKind.AwaitExpression,
                    SyntaxKind.ElementAccessExpression,
                    SyntaxKind.ForEachStatement,
                    SyntaxKind.ThrowStatement,
                    SyntaxKind.SimpleMemberAccessExpression));
    }
}
