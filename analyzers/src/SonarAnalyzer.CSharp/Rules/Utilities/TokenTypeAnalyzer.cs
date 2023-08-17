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

using SonarAnalyzer.Protobuf;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenTypeAnalyzer : TokenTypeAnalyzerBase<SyntaxKind>
    {
        protected override ILanguageFacade<SyntaxKind> Language { get; } = CSharpFacade.Instance;

        protected override TokenClassifierBase GetTokenClassifier(SemanticModel semanticModel, bool skipIdentifierTokens) =>
            new TokenClassifier(semanticModel, skipIdentifierTokens);

        protected override TriviaClassifierBase GetTriviaClassifier() =>
            new TriviaClassifier();

        internal sealed class TokenClassifier : TokenClassifierBase
        {
            private static readonly SyntaxKind[] StringLiteralTokens =
            {
                SyntaxKind.StringLiteralToken,
                SyntaxKind.CharacterLiteralToken,
                SyntaxKindEx.SingleLineRawStringLiteralToken,
                SyntaxKindEx.MultiLineRawStringLiteralToken,
                SyntaxKindEx.Utf8StringLiteralToken,
                SyntaxKindEx.Utf8SingleLineRawStringLiteralToken,
                SyntaxKindEx.Utf8MultiLineRawStringLiteralToken,
                SyntaxKind.InterpolatedStringStartToken,
                SyntaxKind.InterpolatedVerbatimStringStartToken,
                SyntaxKindEx.InterpolatedSingleLineRawStringStartToken,
                SyntaxKindEx.InterpolatedMultiLineRawStringStartToken,
                SyntaxKind.InterpolatedStringTextToken,
                SyntaxKind.InterpolatedStringEndToken,
                SyntaxKindEx.InterpolatedRawStringEndToken,
            };

            public TokenClassifier(SemanticModel semanticModel, bool skipIdentifiers) : base(semanticModel, skipIdentifiers) { }

            protected override SyntaxNode GetBindableParent(SyntaxToken token) =>
                token.GetBindableParent();

            protected override bool IsIdentifier(SyntaxToken token) =>
                token.IsKind(SyntaxKind.IdentifierToken);

            protected override bool IsKeyword(SyntaxToken token) =>
                SyntaxFacts.IsKeywordKind(token.Kind());

            protected override bool IsNumericLiteral(SyntaxToken token) =>
                token.IsKind(SyntaxKind.NumericLiteralToken);

            protected override bool IsStringLiteral(SyntaxToken token) =>
                token.IsAnyKind(StringLiteralTokens);

            protected override TokenTypeInfo.Types.TokenInfo ClassifyIdentifier(SyntaxToken token) =>
                // Based on <Kind Name="IdentifierToken"/> in SonarAnalyzer.CFG/ShimLayer\Syntax.xml
                token.Parent switch
                {
                    SimpleNameSyntax x when token == x.Identifier && ClassifySimpleName(x) is { } tokenType => TokenInfo(token, tokenType),
                    FromClauseSyntax x when token == x.Identifier => null,
                    LetClauseSyntax x when token == x.Identifier => null,
                    JoinClauseSyntax x when token == x.Identifier => null,
                    JoinIntoClauseSyntax x when token == x.Identifier => null,
                    QueryContinuationSyntax x when token == x.Identifier => null,
                    VariableDeclaratorSyntax x when token == x.Identifier => null,
                    LabeledStatementSyntax x when token == x.Identifier => null,
                    ForEachStatementSyntax x when token == x.Identifier => null,
                    CatchDeclarationSyntax x when token == x.Identifier => null,
                    ExternAliasDirectiveSyntax x when token == x.Identifier => null,
                    EnumMemberDeclarationSyntax x when token == x.Identifier => null,
                    MethodDeclarationSyntax x when token == x.Identifier => null,
                    PropertyDeclarationSyntax x when token == x.Identifier => null,
                    EventDeclarationSyntax x when token == x.Identifier => null,
                    AccessorDeclarationSyntax x when token == x.Keyword => null,
                    ParameterSyntax x when token == x.Identifier => null,
                    var x when FunctionPointerUnmanagedCallingConventionSyntaxWrapper.IsInstance(x) && token == ((FunctionPointerUnmanagedCallingConventionSyntaxWrapper)x).Name => null,
                    var x when TupleElementSyntaxWrapper.IsInstance(x) && token == ((TupleElementSyntaxWrapper)x).Identifier => null,
                    var x when LocalFunctionStatementSyntaxWrapper.IsInstance(x) && token == ((LocalFunctionStatementSyntaxWrapper)x).Identifier => null,
                    var x when SingleVariableDesignationSyntaxWrapper.IsInstance(x) && token == ((SingleVariableDesignationSyntaxWrapper)x).Identifier => null,
                    TypeParameterSyntax x when token == x.Identifier => TokenInfo(token, TokenType.TypeName),
                    BaseTypeDeclarationSyntax x when token == x.Identifier => TokenInfo(token, TokenType.TypeName),
                    DelegateDeclarationSyntax x when token == x.Identifier => TokenInfo(token, TokenType.TypeName),
                    ConstructorDeclarationSyntax x when token == x.Identifier => TokenInfo(token, TokenType.TypeName),
                    DestructorDeclarationSyntax x when token == x.Identifier => TokenInfo(token, TokenType.TypeName),
                    AttributeTargetSpecifierSyntax x when token == x.Identifier => TokenInfo(token, TokenType.Keyword), // for unknown target specifier [unknown: Obsolete]
                    _ => base.ClassifyIdentifier(token),
                };

            private TokenType? ClassifySimpleName(SimpleNameSyntax x) =>
                IsInTypeContext(x)
                    ? ClassifySimpleNameType(x)
                    : ClassifySimpleNameExpression(x);

            private TokenType? ClassifySimpleNameExpression(SimpleNameSyntax name) =>
                name.Parent is MemberAccessExpressionSyntax
                    ? ClassifyMemberAccess(name)
                    : ClassifySimpleNameExpressionSpecialContext(name, name);

            /// <summary>
            /// The <paramref name="name"/> is likely not referring a type, but there are some <paramref name="context"/> and
            /// special cases where it still might bind to a type or is treated as a keyword. The <paramref name="context"/>
            /// is the member access of the <paramref name="name"/>. e.g. for A.B.C <paramref name="name"/> may
            /// refer to "B" and <paramref name="context"/> would be the parent member access expression A.B and recursively A.B.C.
            /// </summary>
            private TokenType? ClassifySimpleNameExpressionSpecialContext(SyntaxNode context, SimpleNameSyntax name) =>
                context.Parent switch
                {
                    // some identifier can be bound to a type or a constant:
                    CaseSwitchLabelSyntax => ClassifyIdentifierByModel(name), // case i:
                    { } parent when NameIsRightOfIsExpression(name, parent) => ClassifyIdentifierByModel(name), // is i
                    { RawKind: (int)SyntaxKindEx.ConstantPattern } => ClassifyIdentifierByModel(name), // is { X: i }
                    // nameof(i) can be bound to a type or a member
                    ArgumentSyntax x when IsNameOf(x) => ClassifyIdentifierByModel(name),
                    // walk up memberaccess to detect cases like above
                    MemberAccessExpressionSyntax x => ClassifySimpleNameExpressionSpecialContext(x, name),
                    _ => ClassifySimpleNameExpressionSpecialNames(name)
                };

            private bool IsNameOf(ArgumentSyntax argument)
                => argument is
                {
                    Parent: ArgumentListSyntax
                    {
                        Arguments.Count: 1,
                        Parent: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } }
                    }
                };

            private bool NameIsRightOfIsExpression(NameSyntax name, SyntaxNode binary)
                => binary is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression, Right: { } x } && x == name;

            /// <summary>
            /// Some expression identifier are classified differently, like "value" in a setter.
            /// </summary>
            private TokenType ClassifySimpleNameExpressionSpecialNames(SimpleNameSyntax name) =>
                // "value" in a setter is a classified as keyword
                IsValueParameterOfSetter(name)
                    ? TokenType.Keyword
                    : TokenType.UnknownTokentype;

            private bool IsValueParameterOfSetter(SimpleNameSyntax simpleName)
                => simpleName is IdentifierNameSyntax { Identifier.Text: "value", Parent: not MemberAccessExpressionSyntax }
                    && SemanticModel.GetSymbolInfo(simpleName).Symbol is IParameterSymbol
                    {
                        ContainingSymbol: IMethodSymbol
                        {
                            MethodKind: MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
                        }
                    };

            private TokenType? ClassifyMemberAccess(SimpleNameSyntax name) =>
                // Most right hand side of a member access?
                name is
                {
                    Parent: MemberAccessExpressionSyntax
                    {
                        Parent: not MemberAccessExpressionSyntax, // Topmost in a memberaccess tree
                        Name: { } parentName // Right hand side
                    } parent
                } && parentName == name
                    ? ClassifySimpleNameExpressionSpecialContext(parent, name)
                    : ClassifyIdentifierByModel(name);

            private TokenType ClassifyIdentifierByModel(SimpleNameSyntax x) =>
                SemanticModel.GetSymbolInfo(x).Symbol is INamedTypeSymbol
                    ? TokenType.TypeName
                    : TokenType.UnknownTokentype;

            private TokenType ClassifyAliasDeclarationByModel(UsingDirectiveSyntax usingDirective) =>
                SemanticModel.GetDeclaredSymbol(usingDirective) is { Target: INamedTypeSymbol }
                    ? TokenType.TypeName
                    : TokenType.UnknownTokentype;

            private TokenType? ClassifySimpleNameType(SimpleNameSyntax name) =>
                name is GenericNameSyntax
                    ? TokenType.TypeName
                    : ClassifySimpleNameTypeSpecialContext(name, name);

            private TokenType? ClassifySimpleNameTypeSpecialContext(SyntaxNode context, SimpleNameSyntax name) =>
                context.Parent switch
                {
                    // namespace X; or namespace X { } -> always unknown
                    NamespaceDeclarationSyntax or { RawKind: (int)SyntaxKindEx.FileScopedNamespaceDeclaration } => TokenType.UnknownTokentype,
                    // using System; -> normal using
                    UsingDirectiveSyntax { Alias: null, StaticKeyword.RawKind: (int)SyntaxKind.None } => TokenType.UnknownTokentype,
                    // using Alias = System; -> "System" can be a type or a namespace
                    UsingDirectiveSyntax { Alias: not null } => ClassifyIdentifierByModel(name),
                    // using Alias = System; -> "Alias" can be a type or a namespace
                    NameEqualsSyntax { Parent: UsingDirectiveSyntax { Alias.Name: { } aliasName } usingDirective } when aliasName == name => ClassifyAliasDeclarationByModel(usingDirective),
                    // using static System.Math; -> most right hand side must be a type
                    UsingDirectiveSyntax
                    {
                        StaticKeyword.RawKind: (int)SyntaxKind.StaticKeyword, Name: QualifiedNameSyntax { Right: SimpleNameSyntax x }
                    } => x == name ? TokenType.TypeName : ClassifyIdentifierByModel(name),
                    // Walk up classified names (to detect namespace and using context)
                    QualifiedNameSyntax parent => ClassifySimpleNameTypeSpecialContext(parent, name),
                    // We are in a "normal" type context like a declaration
                    _ => ClassifySimpleNameTypeInTypeContext(name),
                };

            private TokenType ClassifySimpleNameTypeInTypeContext(SimpleNameSyntax name) =>
                name switch
                {
                    // unqualified types called "var" or "dynamic" are classified as keywords.
                    { Parent: not QualifiedNameSyntax } => name is { Identifier.Text: "var" or "dynamic" }
                        ? TokenType.Keyword
                        : TokenType.TypeName,
                    { Parent: QualifiedNameSyntax { Parent: { } parentOfTopMostQualifiedName, Right: { } right } topMostQualifiedName } when
                        right == name // On the right hand side?
                        && parentOfTopMostQualifiedName is not QualifiedNameSyntax // Is this the most right hand side?
                                                                                   // This is a type, except on the right side of "is" where it might also be a constant like Int32.MaxValue
                        && !NameIsRightOfIsExpression(topMostQualifiedName, parentOfTopMostQualifiedName) => TokenType.TypeName,
                    // We are somewhere in a qualified name. It probably is a namespace but could also be the outer type of a nested type.
                    _ => ClassifyIdentifierByModel(name),
                };

            private static bool IsInTypeContext(SimpleNameSyntax name) =>
                // Based on Syntax.xml search for Type="TypeSyntax" and Type="NameSyntax"
                name.Parent switch
                {
                    QualifiedNameSyntax => true,
                    AliasQualifiedNameSyntax x => x.Name == name,
                    NullableTypeSyntax x => x.ElementType == name,
                    BaseTypeSyntax x => x.Type == name,
                    BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } x => x.Right == name,
                    ArrayTypeSyntax x => x.ElementType == name,
                    TypeArgumentListSyntax => true,
                    RefValueExpressionSyntax x => x.Type == name,
                    DefaultExpressionSyntax x => x.Type == name,
                    ParameterSyntax x => x.Type == name,
                    TypeOfExpressionSyntax x => x.Type == name,
                    SizeOfExpressionSyntax x => x.Type == name,
                    CastExpressionSyntax x => x.Type == name,
                    ObjectCreationExpressionSyntax x => x.Type == name,
                    StackAllocArrayCreationExpressionSyntax x => x.Type == name,
                    FromClauseSyntax x => x.Type == name,
                    JoinClauseSyntax x => x.Type == name,
                    VariableDeclarationSyntax x => x.Type == name,
                    ForEachStatementSyntax x => x.Type == name,
                    CatchDeclarationSyntax x => x.Type == name,
                    DelegateDeclarationSyntax x => x.ReturnType == name,
                    TypeConstraintSyntax x => x.Type == name,
                    TypeParameterConstraintClauseSyntax x => x.Name == name,
                    MethodDeclarationSyntax x => x.ReturnType == name,
                    OperatorDeclarationSyntax x => x.ReturnType == name,
                    ConversionOperatorDeclarationSyntax x => x.Type == name,
                    BasePropertyDeclarationSyntax x => x.Type == name,
                    PointerTypeSyntax x => x.ElementType == name,
                    AttributeSyntax x => x.Name == name,
                    ExplicitInterfaceSpecifierSyntax x => x.Name == name,
                    UsingDirectiveSyntax x => x.Name == name,
                    NameEqualsSyntax { Parent: UsingDirectiveSyntax { Alias.Name: { } x } } => x == name,
                    var x when BaseParameterSyntaxWrapper.IsInstance(x) => ((BaseParameterSyntaxWrapper)x).Type == name,
                    var x when DeclarationPatternSyntaxWrapper.IsInstance(x) => ((DeclarationPatternSyntaxWrapper)x).Type == name,
                    var x when RecursivePatternSyntaxWrapper.IsInstance(x) => ((RecursivePatternSyntaxWrapper)x).Type == name,
                    var x when TypePatternSyntaxWrapper.IsInstance(x) => ((TypePatternSyntaxWrapper)x).Type == name,
                    var x when LocalFunctionStatementSyntaxWrapper.IsInstance(x) => ((LocalFunctionStatementSyntaxWrapper)x).ReturnType == name,
                    var x when DeclarationExpressionSyntaxWrapper.IsInstance(x) => ((DeclarationExpressionSyntaxWrapper)x).Type == name,
                    var x when ParenthesizedLambdaExpressionSyntaxWrapper.IsInstance(x) => ((ParenthesizedLambdaExpressionSyntaxWrapper)x).ReturnType == name,
                    var x when BaseNamespaceDeclarationSyntaxWrapper.IsInstance(x) => ((BaseNamespaceDeclarationSyntaxWrapper)x).Name == name,
                    _ => false,
                };
        }

        internal sealed class TriviaClassifier : TriviaClassifierBase
        {
            private static readonly SyntaxKind[] RegularCommentToken =
            {
                SyntaxKind.SingleLineCommentTrivia,
                SyntaxKind.MultiLineCommentTrivia,
            };

            private static readonly SyntaxKind[] DocCommentToken =
            {
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                SyntaxKind.MultiLineDocumentationCommentTrivia,
            };

            protected override bool IsRegularComment(SyntaxTrivia trivia) =>
                trivia.IsAnyKind(RegularCommentToken);

            protected override bool IsDocComment(SyntaxTrivia trivia) =>
                trivia.IsAnyKind(DocCommentToken);
        }
    }
}
