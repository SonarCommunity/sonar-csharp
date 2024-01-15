﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
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

namespace SonarAnalyzer.Helpers
{
    /// <summary>
    /// Collects all symbol usages from a class declaration. Ignores symbols whose names are not present
    /// in the knownSymbolNames collection for performance reasons.
    /// </summary>
    internal class CSharpSymbolUsageCollector : SafeCSharpSyntaxWalker
    {
        [Flags]
        private enum SymbolAccess
        {
            None = 0,
            Read = 1,
            Write = 2,
            ReadWrite = Read | Write
        }

        private static readonly ISet<SyntaxKind> IncrementKinds = new HashSet<SyntaxKind>
        {
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.PreIncrementExpression,
            SyntaxKind.PostDecrementExpression,
            SyntaxKind.PreDecrementExpression
        };

        private readonly Compilation compilation;
        private readonly HashSet<string> knownSymbolNames;
        private SemanticModel semanticModel;

        public ISet<ISymbol> UsedSymbols { get; } = new HashSet<ISymbol>();
        public IDictionary<ISymbol, SymbolUsage> FieldSymbolUsages { get; } = new Dictionary<ISymbol, SymbolUsage>();
        public HashSet<string> DebuggerDisplayValues { get; } = new();
        public Dictionary<IPropertySymbol, AccessorAccess> PropertyAccess { get; } = new();

        public CSharpSymbolUsageCollector(Compilation compilation, IEnumerable<ISymbol> knownSymbols)
        {
            this.compilation = compilation;
            knownSymbolNames = knownSymbols.Select(GetName).ToHashSet();
        }

        public override void Visit(SyntaxNode node)
        {
            semanticModel = node.EnsureCorrectSemanticModelOrDefault(semanticModel ?? compilation.GetSemanticModel(node.SyntaxTree));
            if (semanticModel != null)
            {
                if (node.IsKind(SyntaxKindEx.ImplicitObjectCreationExpression)
                    && knownSymbolNames.Contains(ObjectCreationFactory.Create(node).TypeAsString(semanticModel)))
                {
                    UsedSymbols.UnionWith(GetSymbols(node));
                }
                else if (node.IsKind(SyntaxKindEx.LocalFunctionStatement)
                         && ((LocalFunctionStatementSyntaxWrapper)node) is { AttributeLists: { Count: > 0 } }
                         && semanticModel.GetDeclaredSymbol(node) is IMethodSymbol localFunctionSymbol)
                {
                    UsedSymbols.UnionWith(localFunctionSymbol
                                            .GetAttributes()
                                            .Where(a => knownSymbolNames.Contains(a.AttributeClass.Name))
                                            .Select(a => a.AttributeClass));
                }
                else if (node.IsKind(SyntaxKindEx.PrimaryConstructorBaseType)
                         && knownSymbolNames.Contains(((PrimaryConstructorBaseTypeSyntaxWrapper)node).Type.GetName()))
                {
                    UsedSymbols.UnionWith(GetSymbols(node));
                }
                base.Visit(node);
            }
        }

        // TupleExpression "(a, b) = qix"
        // ParenthesizedVariableDesignation "var (a, b) = quix" inside a DeclarationExpression
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var leftTupleCount = GetTupleCount(node.Left);
            if (leftTupleCount != 0)
            {
                var assignmentRight = node.Right;
                var namedTypeSymbol = semanticModel.GetSymbolInfo(assignmentRight).Symbol?.GetSymbolType();
                if (namedTypeSymbol != null)
                {
                    var deconstructors = namedTypeSymbol.GetMembers("Deconstruct");
                    if (deconstructors.Length == 1)
                    {
                        UsedSymbols.Add(deconstructors.First());
                    }
                    else if (deconstructors.Length > 1 && FindDeconstructor(deconstructors, leftTupleCount) is {} deconstructor)
                    {
                        UsedSymbols.Add(deconstructor);
                    }
                }
            }
            base.VisitAssignmentExpression(node);

            static int GetTupleCount(ExpressionSyntax assignmentLeft)
            {
                var result = 0;
                if (TupleExpressionSyntaxWrapper.IsInstance(assignmentLeft))
                {
                    result = ((TupleExpressionSyntaxWrapper)assignmentLeft).Arguments.Count;
                }
                else if (DeclarationExpressionSyntaxWrapper.IsInstance(assignmentLeft)
                         && (DeclarationExpressionSyntaxWrapper)assignmentLeft is { } leftDeclaration
                         && ParenthesizedVariableDesignationSyntaxWrapper.IsInstance(leftDeclaration.Designation))
                {
                    result = ((ParenthesizedVariableDesignationSyntaxWrapper)leftDeclaration.Designation).Variables.Count;
                }
                return result;
            }

            static ISymbol FindDeconstructor(IEnumerable<ISymbol> deconstructors, int numberOfArguments) =>
                deconstructors.FirstOrDefault(m => m.GetParameters().Count() == numberOfArguments && m.DeclaredAccessibility.IsAccessibleOutsideTheType());
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null
                && symbol.ContainingType.Is(KnownType.System_Diagnostics_DebuggerDisplayAttribute)
                && node.ArgumentList != null)
            {
                var arguments = node.ArgumentList.Arguments
                    .Where(IsValueNameOrType)
                    .Select(a => semanticModel.GetConstantValue(a.Expression))
                    .Where(o => o.HasValue)
                    .Select(o => o.Value)
                    .OfType<string>();

                DebuggerDisplayValues.UnionWith(arguments);
            }
            base.VisitAttribute(node);

            static bool IsValueNameOrType(AttributeArgumentSyntax a) =>
                a.NameColon == null  // Value
                || a.NameColon.Name.Identifier.ValueText == "Value"
                || a.NameColon.Name.Identifier.ValueText == "Name"
                || a.NameColon.Name.Identifier.ValueText == "Type";
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (IsKnownIdentifier(node.Identifier))
            {
                var symbols = GetSymbols(node);
                TryStoreFieldAccess(node, symbols);
                UsedSymbols.UnionWith(symbols);
                TryStorePropertyAccess(node, symbols);
            }
            base.VisitIdentifierName(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (knownSymbolNames.Contains(node.Type.GetName()))
            {
                UsedSymbols.UnionWith(GetSymbols(node));
            }
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (IsKnownIdentifier(node.Identifier))
            {
                UsedSymbols.UnionWith(GetSymbols(node));
            }
            base.VisitGenericName(node);
        }

        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (node.Expression.IsKind(SyntaxKind.ThisExpression) || knownSymbolNames.Contains(node.Expression.GetIdentifier()?.ValueText))
            {
                var symbols = GetSymbols(node);
                UsedSymbols.UnionWith(symbols);
                TryStorePropertyAccess(node, symbols);
            }
            base.VisitElementAccessExpression(node);
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            // In this case (":base()") we cannot check at the syntax level if the constructor name is in the list
            // of known names so we have to check for symbols.
            UsedSymbols.UnionWith(GetSymbols(node));
            base.VisitConstructorInitializer(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            // We are visiting a ctor with no initializer and the compiler will automatically
            // call the default constructor of the type if declared, or the base type if the
            // current type does not declare a default constructor.
            if (node.Initializer == null && IsKnownIdentifier(node.Identifier))
            {
                var constructor = semanticModel.GetDeclaredSymbol(node);
                var implicitlyCalledConstructor = GetImplicitlyCalledConstructor(constructor);
                if (implicitlyCalledConstructor != null)
                {
                    UsedSymbols.Add(implicitlyCalledConstructor);
                }
            }
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (IsKnownIdentifier(node.Identifier))
            {
                var usage = GetFieldSymbolUsage(semanticModel.GetDeclaredSymbol(node));
                usage.Declaration = node;
                if (node.Initializer != null)
                {
                    usage.Initializer = node;
                }
            }
            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null && IsKnownIdentifier(node.Identifier))
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                UsedSymbols.Add(symbol);
                StorePropertyAccess(symbol, AccessorAccess.Set);
            }
            base.VisitPropertyDeclaration(node);
        }

        private SymbolAccess ParentAccessType(SyntaxNode node) =>
            node.Parent switch
            {
                // (node)
                ParenthesizedExpressionSyntax parenthesizedExpression => ParentAccessType(parenthesizedExpression),
                // node;
                ExpressionStatementSyntax _ => SymbolAccess.None,
                // node(_) : <unexpected>
                InvocationExpressionSyntax invocation => node == invocation.Expression ? SymbolAccess.Read : SymbolAccess.None,
                // _.node : node._
                MemberAccessExpressionSyntax memberAccess => node == memberAccess.Name ? ParentAccessType(memberAccess) : SymbolAccess.Read,
                // _?.node : node?._
                MemberBindingExpressionSyntax memberBinding => node == memberBinding.Name ? ParentAccessType(memberBinding) : SymbolAccess.Read,
                // node ??= _ : _ ??= node
                AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKindEx.CoalesceAssignmentExpression) =>
                    node == assignment.Left ? SymbolAccess.ReadWrite : SymbolAccess.Read,
                // Ignoring distinction assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression) between
                // "node = _" and "node += _" both are considered as Write and rely on the parent to know if its read.
                //  node = _ : _ = node
                AssignmentExpressionSyntax assignment => node == assignment.Left ? SymbolAccess.Write | ParentAccessType(assignment) : SymbolAccess.Read,
                // Invocation(node), Invocation(out node), Invocation(ref node)
                ArgumentSyntax argument => ArgumentAccessType(argument),
                // node++
                ExpressionSyntax expressionSyntax when expressionSyntax.IsAnyKind(IncrementKinds) => SymbolAccess.Write | ParentAccessType(expressionSyntax),
                // => node
                ArrowExpressionClauseSyntax arrowExpressionClause when arrowExpressionClause.Parent is MethodDeclarationSyntax arrowMethod =>
                        arrowMethod.ReturnType != null && arrowMethod.ReturnType.IsKnownType(KnownType.Void, semanticModel)
                            ? SymbolAccess.None
                            : SymbolAccess.Read,
                _ => SymbolAccess.Read
            };

        private static SymbolAccess ArgumentAccessType(ArgumentSyntax argument) =>
            argument.RefOrOutKeyword.Kind() switch
            {
                // out Type node : out node
                SyntaxKind.OutKeyword => SymbolAccess.Write,
                // ref node
                SyntaxKind.RefKeyword => SymbolAccess.ReadWrite,
                _ => SymbolAccess.Read
            };

        /// <summary>
        /// Given a node, it tries to get the symbol or the candidate symbols (if the compiler cannot find the symbol,
        /// .e.g when the code cannot compile).
        /// </summary>
        /// <returns>List of symbols</returns>
        private ImmutableArray<ISymbol> GetSymbols<TSyntaxNode>(TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            return new[] { symbolInfo.Symbol }
                .Concat(symbolInfo.CandidateSymbols)
                .Select(GetOriginalDefinition)
                .WhereNotNull()
                .ToImmutableArray();

            static ISymbol GetOriginalDefinition(ISymbol candidateSymbol) =>
                candidateSymbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.ReducedExtension
                    ? methodSymbol.ReducedFrom?.OriginalDefinition
                    : candidateSymbol?.OriginalDefinition;
        }

        private void TryStorePropertyAccess(ExpressionSyntax node, IEnumerable<ISymbol> identifierSymbols)
        {
            var propertySymbols = identifierSymbols.OfType<IPropertySymbol>().ToList();
            if (propertySymbols.Any())
            {
                var access = EvaluatePropertyAccesses(node);
                foreach (var propertySymbol in propertySymbols)
                {
                    StorePropertyAccess(propertySymbol, access);
                }
            }
        }

        private void StorePropertyAccess(IPropertySymbol propertySymbol, AccessorAccess access)
        {
            if (PropertyAccess.ContainsKey(propertySymbol))
            {
                PropertyAccess[propertySymbol] |= access;
            }
            else
            {
                PropertyAccess[propertySymbol] = access;
            }
        }

        private AccessorAccess EvaluatePropertyAccesses(ExpressionSyntax node)
        {
            var topmostSyntax = GetTopmostSyntaxWithTheSameSymbol(node);
            if (topmostSyntax.Parent is AssignmentExpressionSyntax assignmentExpression)
            {
                if (assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    // Prop = value --> set
                    // value = Prop --> get
                    return assignmentExpression.Left == topmostSyntax ? AccessorAccess.Set : AccessorAccess.Get;
                }
                else
                {
                    // Prop += value --> get/set
                    return AccessorAccess.Both;
                }
            }
            else if (topmostSyntax.Parent is ArgumentSyntax argument && argument.IsInTupleAssignmentTarget())
            {
                return AccessorAccess.Set;
            }
            else if (node.IsInNameOfArgument(semanticModel))
            {
                // nameof(Prop) --> get/set
                return AccessorAccess.Both;
            }
            else
            {
                // Prop++ --> get/set
                return topmostSyntax.Parent.IsAnyKind(IncrementKinds) ? AccessorAccess.Both : AccessorAccess.Get;
            }
        }

        private bool IsKnownIdentifier(SyntaxToken identifier) =>
            knownSymbolNames.Contains(identifier.ValueText);

        private void TryStoreFieldAccess(IdentifierNameSyntax node, IEnumerable<ISymbol> symbols)
        {
            var access = ParentAccessType(node);
            var fieldSymbolUsagesList = GetFieldSymbolUsagesList(symbols);
            if (HasFlag(access, SymbolAccess.Read))
            {
                fieldSymbolUsagesList.ForEach(usage => usage.Readings.Add(node));
            }

            if (HasFlag(access, SymbolAccess.Write))
            {
                fieldSymbolUsagesList.ForEach(usage => usage.Writings.Add(node));
            }

            static bool HasFlag(SymbolAccess symbolAccess, SymbolAccess flag) => (symbolAccess & flag) != 0;
        }

        private List<SymbolUsage> GetFieldSymbolUsagesList(IEnumerable<ISymbol> symbols) =>
            symbols.Select(GetFieldSymbolUsage).ToList();

        private SymbolUsage GetFieldSymbolUsage(ISymbol symbol) =>
            FieldSymbolUsages.GetOrAdd(symbol, s => new SymbolUsage(s));

        private static SyntaxNode GetTopmostSyntaxWithTheSameSymbol(SyntaxNode identifier) =>
            // All of the cases below could be parts of invocation or other expressions
            identifier.Parent switch
            {
                // this.identifier or a.identifier or ((a)).identifier, but not identifier.other
                MemberAccessExpressionSyntax memberAccess when memberAccess.Name == identifier => memberAccess.GetSelfOrTopParenthesizedExpression(),
                // this?.identifier or a?.identifier or ((a))?.identifier, but not identifier?.other
                MemberBindingExpressionSyntax memberBinding when memberBinding.Name == identifier => memberBinding.Parent.GetSelfOrTopParenthesizedExpression(),
                // identifier or ((identifier))
                _ => identifier.GetSelfOrTopParenthesizedExpression()
            };

        private static IMethodSymbol GetImplicitlyCalledConstructor(IMethodSymbol constructor) =>
            // In case there is no other explicitly called constructor in a constructor declaration
            // the compiler will automatically put a call to the current class' default constructor,
            // or if the declaration is the default constructor or there is no default constructor,
            // the compiler will put a call the base class' default constructor.
            IsDefaultConstructor(constructor)
                ? GetDefaultConstructor(constructor.ContainingType.BaseType)
                : GetDefaultConstructor(constructor.ContainingType) ?? GetDefaultConstructor(constructor.ContainingType.BaseType);

        private static IMethodSymbol GetDefaultConstructor(INamedTypeSymbol namedType) =>
            // See https://github.com/SonarSource/sonar-dotnet/issues/3155
            namedType != null && namedType.InstanceConstructors != null
                ? namedType.InstanceConstructors.FirstOrDefault(IsDefaultConstructor)
                : null;

        private static bool IsDefaultConstructor(IMethodSymbol constructor) =>
            constructor.Parameters.Length == 0;

        private static string GetName(ISymbol symbol) =>
            symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;
    }
}
