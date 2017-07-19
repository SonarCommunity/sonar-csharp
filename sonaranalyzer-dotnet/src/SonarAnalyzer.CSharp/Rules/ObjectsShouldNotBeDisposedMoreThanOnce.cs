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
using SonarAnalyzer.Helpers.FlowAnalysis.Common;
using SonarAnalyzer.Helpers.FlowAnalysis.CSharp;
using ExplodedGraph = SonarAnalyzer.Helpers.FlowAnalysis.CSharp.ExplodedGraph;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class ObjectsShouldNotBeDisposedMoreThanOnce : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3966";
        private const string MessageFormat = "Refactor this code to make sure '{0}' is disposed only once.";

        private static readonly ISet<KnownType> typesDisposingUnderlyingStream = new HashSet<KnownType>
        {
            KnownType.System_IO_StreamReader,
            KnownType.System_IO_StreamWriter
        };

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterExplodedGraphBasedAnalysis(CheckForMultipleDispose);
        }

        private static void CheckForMultipleDispose(ExplodedGraph explodedGraph, SyntaxNodeAnalysisContext context)
        {
            var objectDisposedCheck = new ObjectDisposedPointerCheck(explodedGraph);
            explodedGraph.AddExplodedGraphCheck(objectDisposedCheck);

            EventHandler<ObjectDisposedEventArgs> memberAccessedHandler =
                (sender, args) =>
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(rule, args.Location, args.Name));
                };

            objectDisposedCheck.ObjectDisposed += memberAccessedHandler;

            try
            {
                explodedGraph.Walk();
            }
            finally
            {
                objectDisposedCheck.ObjectDisposed -= memberAccessedHandler;
            }
        }

        internal class ObjectDisposedEventArgs : EventArgs
        {
            public string Name { get; }
            public Location Location { get; }

            public ObjectDisposedEventArgs(string name, Location location)
            {
                Name = name;
                Location = location;
            }
        }

        internal sealed class ObjectDisposedPointerCheck : ExplodedGraphCheck
        {
            public event EventHandler<ObjectDisposedEventArgs> ObjectDisposed;

            public ObjectDisposedPointerCheck(ExplodedGraph explodedGraph)
                : base(explodedGraph)
            {
            }

            public override ProgramState PreProcessUsingStatement(ProgramPoint programPoint, ProgramState programState)
            {
                var newProgramState = programState;

                var usingFinalizer = (UsingEndBlock)programPoint.Block;

                var disposables = usingFinalizer.Identifiers
                    .Select(i =>
                    new
                    {
                        SyntaxNode = i.Parent,
                        Symbol = semanticModel.GetDeclaredSymbol(i.Parent)
                            ?? semanticModel.GetSymbolInfo(i.Parent).Symbol
                    });

                foreach (var disposable in disposables)
                {
                    newProgramState = ProcessDisposableSymbol(newProgramState, disposable.SyntaxNode, disposable.Symbol);
                }

                newProgramState = ProcessStreamDisposingTypes(newProgramState,
                    (SyntaxNode)usingFinalizer.UsingStatement.Expression ?? usingFinalizer.UsingStatement.Declaration);

                return newProgramState;
            }

            public override ProgramState PreProcessInstruction(ProgramPoint programPoint, ProgramState programState)
            {
                var instruction = programPoint.Block.Instructions[programPoint.Offset] as InvocationExpressionSyntax;

                return instruction == null
                    ? programState
                    : VisitInvocationExpression(instruction, programState);
            }

            private ProgramState VisitInvocationExpression(InvocationExpressionSyntax instruction, ProgramState programState)
            {
                var newProgramState = programState;

                var disposeMethodSymbol = semanticModel.GetSymbolInfo(instruction).Symbol as IMethodSymbol;
                if (disposeMethodSymbol.IsIDisposableDispose())
                {
                    var disposable = ((MemberAccessExpressionSyntax)instruction.Expression).Expression;
                    var disposableSymbol = semanticModel.GetSymbolInfo(disposable).Symbol;

                    newProgramState = ProcessDisposableSymbol(newProgramState, disposable, disposableSymbol);
                }

                return newProgramState;
            }

            private ProgramState ProcessStreamDisposingTypes(ProgramState programState, SyntaxNode usingExpression)
            {
                var newProgramState = programState;

                var arguments = usingExpression.DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(this.IsStreamDisposingType)
                    .Select(FirstArgumentOrDefault)
                    .WhereNotNull();

                foreach (var argument in arguments)
                {
                    var streamSymbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol;
                    newProgramState = ProcessDisposableSymbol(newProgramState, argument.Expression, streamSymbol);
                }

                return newProgramState;
            }

            private ProgramState ProcessDisposableSymbol(ProgramState programState, SyntaxNode instruction, ISymbol disposableSymbol)
            {
                if (disposableSymbol == null) // DisposableSymbol is null when we invoke an array element
                {
                    return programState;
                }

                if (disposableSymbol.HasConstraint(DisposableConstraint.Disposed, programState))
                {
                    ObjectDisposed?.Invoke(this, new ObjectDisposedEventArgs(disposableSymbol.Name, instruction.GetLocation()));
                    return programState;
                }

                // We should not replace Null constraint because having Disposed constraint
                // implies having NotNull constraint, which is incorrect.
                if (disposableSymbol.HasConstraint(ObjectConstraint.Null, programState))
                {
                    return programState;
                }

                return disposableSymbol.SetConstraint(DisposableConstraint.Disposed, programState);
            }

            private static ArgumentSyntax FirstArgumentOrDefault(ObjectCreationExpressionSyntax objectCreation) =>
                objectCreation.ArgumentList?.Arguments.FirstOrDefault();

            private bool IsStreamDisposingType(ObjectCreationExpressionSyntax objectCreation) =>
                semanticModel.GetSymbolInfo(objectCreation.Type)
                    .Symbol
                    .GetSymbolType()
                    .DerivesOrImplementsAny(typesDisposingUnderlyingStream);
        }
    }
}
