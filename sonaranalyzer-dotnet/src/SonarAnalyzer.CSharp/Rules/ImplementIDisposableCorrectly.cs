/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class ImplementIDisposableCorrectly : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3881";
        private const string MessageFormat = "Fix this implementation of 'IDisposable' to conform to the dispose pattern.";

        private static readonly ISet<SyntaxKind> notAllowedDisposeModifiers = new HashSet<SyntaxKind>
        {
            SyntaxKind.VirtualKeyword,
            SyntaxKind.ProtectedKeyword
        };

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var classDeclaration = (ClassDeclarationSyntax)c.Node;

                var checker = new DisposableChecker(classDeclaration, c.SemanticModel);
                var locations = checker.GetIssueLocations();
                if (locations.Any())
                {
                    c.ReportDiagnostic(Diagnostic.Create(rule, classDeclaration.Identifier.GetLocation(),
                        locations.ToAdditionalLocations(),
                        locations.ToProperties()));
                }
            },
            SyntaxKind.ClassDeclaration);
        }

        private class DisposableChecker
        {
            private readonly ClassDeclarationSyntax classDeclaration;
            private readonly SemanticModel semanticModel;
            private readonly List<SecondaryLocation> secondaryLocations = new List<SecondaryLocation>();
            private readonly INamedTypeSymbol classSymbol;

            public DisposableChecker(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
            {
                this.classDeclaration = classDeclaration;
                this.semanticModel = semanticModel;
                this.classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            }

            public IEnumerable<SecondaryLocation> GetIssueLocations()
            {
                if (classSymbol == null || classSymbol.IsSealed)
                {
                    return Enumerable.Empty<SecondaryLocation>();
                }

                if (classSymbol.BaseType.Implements(KnownType.System_IDisposable))
                {
                    var idisposableInterfaceSyntax = classDeclaration.BaseList?.Types
                        .FirstOrDefault(IsOrImplementsIDisposable);

                    if (idisposableInterfaceSyntax != null)
                    {
                        AddSecondaryLocation(idisposableInterfaceSyntax.GetLocation(),
                            $"Remove 'IDisposable' from the list of interfaces implemented by '{classSymbol.Name}'"
                                + " and override the base class 'Dispose' implementation instead.");
                    }

                    if (FindMethodDeclarationsRecursive(classSymbol.BaseType, IsVirtualDisposeBool).Any())
                    {
                        VerifyDisposeOverride(FindMethodDeclarations(classSymbol, IsVirtualDisposeBool)
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault());
                    }

                    return secondaryLocations;
                }

                if (classSymbol.Implements(KnownType.System_IDisposable))
                {
                    if (!FindMethodDeclarations(classSymbol, IsVirtualDisposeBool).Any())
                    {
                        AddSecondaryLocation(classDeclaration.Identifier.GetLocation(),
                            $"Provide 'protected' overridable implementation of 'Dispose(bool)' on '{classSymbol.Name}'"
                                + " or mark the type as 'sealed'.");
                    }

                    var destructor = FindMethodDeclarations(classSymbol, SymbolHelper.IsDestructor)
                        .OfType<DestructorDeclarationSyntax>()
                        .FirstOrDefault();

                    VerifyDestructor(destructor);

                    var disposeMethod = FindMethodDeclarations(classSymbol, KnownMethods.IsIDisposableDispose)
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault();

                    VerifyDispose(disposeMethod, hasDestructor: destructor != null);
                }

                return secondaryLocations;
            }

            private void AddSecondaryLocation(Location location, string message)
            {
                secondaryLocations.Add(new SecondaryLocation(location, message));
            }

            private void VerifyDestructor(DestructorDeclarationSyntax destructorSyntax)
            {
                if (destructorSyntax?.Body == null)
                {
                    return;
                }

                if (!HasStatementsCount(destructorSyntax.Body, 1) ||
                    !CallsVirtualDispose(destructorSyntax.Body, argumentValue: "false"))
                {
                    AddSecondaryLocation(destructorSyntax.Identifier.GetLocation(),
                        $"Modify '{classSymbol.Name}.~{classSymbol.Name}()' so that it calls 'Dispose(false)' and then returns.");
                }
            }

            private void VerifyDisposeOverride(MethodDeclarationSyntax disposeMethod)
            {
                if (disposeMethod?.Body == null)
                {
                    return;
                }

                var parameterName = disposeMethod.ParameterList.Parameters.Single().Identifier.ToString();

                if (!CallsVirtualDispose(disposeMethod.Body, argumentValue: parameterName))
                {
                    AddSecondaryLocation(disposeMethod.Identifier.GetLocation(),
                        $"Modify 'Dispose({parameterName})' so that it calls 'base.Dispose({parameterName})'.");
                }
            }

            private void VerifyDispose(MethodDeclarationSyntax disposeMethod, bool hasDestructor)
            {
                if (disposeMethod?.Body == null)
                {
                    return;
                }

                var expectedStatementsCount = hasDestructor
                    ? 2  //// Dispose(true); GC.SuppressFinalize(this);
                    : 1; //// Dispose(true);

                if (!HasStatementsCount(disposeMethod.Body, expectedStatementsCount) ||
                    !CallsVirtualDispose(disposeMethod.Body, argumentValue: "true") ||
                    (!CallsSuppressFinalize(disposeMethod.Body) && hasDestructor))
                {
                    AddSecondaryLocation(disposeMethod.Identifier.GetLocation(),
                        $"'{classSymbol.Name}.Dispose()' should contain only a call to 'Dispose(true)'"
                            + " and if the class contains a finalizer, call to 'GC.SuppressFinalize(this)'.");
                }

                var disposeMethodSymbol = semanticModel.GetDeclaredSymbol(disposeMethod);
                if (disposeMethodSymbol == null)
                {
                    return;
                }

                if (disposeMethodSymbol.IsAbstract ||
                    disposeMethodSymbol.IsVirtual)
                {
                    var modifier = disposeMethod.Modifiers
                        .FirstOrDefault(m => m.IsAnyKind(notAllowedDisposeModifiers));

                    AddSecondaryLocation(modifier.GetLocation(), $"'{classSymbol.Name}.Dispose()' should not be 'virtual' or 'abstract'.");
                }

                if (disposeMethodSymbol.ExplicitInterfaceImplementations.Any())
                {
                    AddSecondaryLocation(disposeMethod.Identifier.GetLocation(), $"'{classSymbol.Name}.Dispose()' should be 'public'.");
                }
            }

            private static bool IsVirtualDisposeBool(IMethodSymbol method)
            {
                return method.Name == nameof(IDisposable.Dispose) &&
                    (method.IsVirtual || method.IsOverride) &&
                    method.DeclaredAccessibility == Accessibility.Protected &&
                    method.Parameters.Length == 1 &&
                    method.Parameters.Any(p => p.Type.Is(KnownType.System_Boolean));
            }

            private bool IsOrImplementsIDisposable(BaseTypeSyntax baseType)
            {
                return baseType?.Type != null &&
                    (semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol)
                        .Is(KnownType.System_IDisposable);
            }

            private static bool HasArgumentValues(InvocationExpressionSyntax invocation, params string[] arguments)
            {
                return invocation.HasExactlyNArguments(arguments.Length) &&
                       invocation.ArgumentList.Arguments
                            .Select((a, index) => a.Expression.ToString() == arguments[index])
                            .All(matching => matching);
            }

            private static bool HasStatementsCount(BlockSyntax methodBody, int expectedStatementsCount)
            {
                return methodBody.ChildNodes().Count() == expectedStatementsCount;
            }

            private bool CallsSuppressFinalize(BlockSyntax methodBody)
            {
                return ContainsMethodInvocation(methodBody,
                    method => HasArgumentValues(method, "this"),
                    KnownMethods.IsGcSuppressFinalize);
            }

            private bool CallsVirtualDispose(BlockSyntax methodBody, string argumentValue)
            {
                return ContainsMethodInvocation(methodBody,
                    method => HasArgumentValues(method, argumentValue),
                    IsVirtualDisposeBool);
            }

            private static IEnumerable<SyntaxNode> FindMethodDeclarations(INamedTypeSymbol typeSymbol, Func<IMethodSymbol, bool> predicate)
            {
                return typeSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(predicate)
                    .SelectMany(m => m.DeclaringSyntaxReferences)
                    .Select(r => r.GetSyntax());
            }

            private static IEnumerable<SyntaxNode> FindMethodDeclarationsRecursive(INamedTypeSymbol typeSymbol, Func<IMethodSymbol, bool> predicate)
            {
                return typeSymbol.GetSelfAndBaseTypes()
                    .SelectMany(t => t.GetMembers())
                    .OfType<IMethodSymbol>()
                    .Where(predicate)
                    .SelectMany(m => m.DeclaringSyntaxReferences)
                    .Select(r => r.GetSyntax());
            }

            private bool ContainsMethodInvocation(BlockSyntax block,
                Func<InvocationExpressionSyntax, bool> syntaxPredicate, Func<IMethodSymbol, bool> symbolPredicate)
            {
                return block.ChildNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .Select(e => e.Expression)
                    .OfType<InvocationExpressionSyntax>()
                    .Where(syntaxPredicate)
                    .Select(e => semanticModel.GetSymbolInfo(e.Expression).Symbol)
                    .OfType<IMethodSymbol>()
                    .Any(symbolPredicate);
            }
        }
    }
}
