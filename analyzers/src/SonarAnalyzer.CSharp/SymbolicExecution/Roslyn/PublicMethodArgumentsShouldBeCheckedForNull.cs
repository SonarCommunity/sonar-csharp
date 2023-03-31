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
using static Microsoft.CodeAnalysis.Accessibility;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static StyleCop.Analyzers.Lightup.SyntaxKindEx;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.RuleChecks.CSharp;

public class PublicMethodArgumentsShouldBeCheckedForNull : SymbolicRuleCheck
{
    private const string DiagnosticId = "S3900";
    private const string MessageFormat = "{0}";

    private static readonly OperationKind[] DereferenceOperations = new[]
    {
        OperationKindEx.Invocation,
        OperationKindEx.FieldReference,
        OperationKindEx.PropertyReference,
        OperationKindEx.EventReference,
        OperationKindEx.Await,
        OperationKindEx.ArrayElementReference,
        OperationKindEx.MethodReference
    };

    internal static readonly DiagnosticDescriptor S3900 = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    protected override DiagnosticDescriptor Rule => S3900;

    public override bool ShouldExecute()
    {
        return (IsRelevantMethod() || IsRelevantPropertyAccessor())
            && IsAccessibleFromOtherAssemblies();

        bool IsRelevantMethod() =>
            Node is BaseMethodDeclarationSyntax { } method && MethodDereferencesArguments(method);

        bool IsRelevantPropertyAccessor() =>
            Node is AccessorDeclarationSyntax { } accessor
            && (!accessor.Keyword.IsKind(GetKeyword) || accessor.Parent.Parent is IndexerDeclarationSyntax);

        bool IsAccessibleFromOtherAssemblies() =>
            SemanticModel.GetDeclaredSymbol(Node).GetEffectiveAccessibility() is Public or Protected or ProtectedOrInternal;

        static bool MethodDereferencesArguments(BaseMethodDeclarationSyntax method)
        {
            var argumentNames = method.ParameterList.Parameters
                                    .Where(x => !x.Modifiers.AnyOfKind(OutKeyword))
                                    .Select(x => x.GetName())
                                    .ToHashSet();

            if (argumentNames.Any())
            {
                var walker = new ArgumentDereferenceWalker(argumentNames);
                walker.SafeVisit(method);
                return walker.DereferencesMethodArguments;
            }
            else
            {
                return false;
            }
        }
    }

    protected override ProgramState PreProcessSimple(SymbolicContext context)
    {
        var operation = context.Operation.Instance;
        if (operation.Kind == OperationKindEx.ParameterReference
            && operation.ToParameterReference().Parameter is var parameter
            && !parameter.Type.IsValueType
            && IsParameterDereferenced(context.Operation)
            && NullableStateIsNotKnownForParameter(parameter)
            && !parameter.HasAttribute(KnownType.Microsoft_AspNetCore_Mvc_FromServicesAttribute))
        {
            var message = SemanticModel.GetDeclaredSymbol(Node).IsConstructor()
                ? "Refactor this constructor to avoid using members of parameter '{0}' because it could be null."
                : "Refactor this method to add validation of parameter '{0}' before using it.";
            ReportIssue(operation, string.Format(message, operation.Syntax), context);
        }

        return context.State;

        bool NullableStateIsNotKnownForParameter(IParameterSymbol symbol) =>
            context.State[symbol] is null || !context.State[symbol].HasConstraint<ObjectConstraint>();
    }

    private static bool IsParameterDereferenced(IOperationWrapperSonar operation) =>
        operation.Parent is { } parent
        && (parent.IsAnyKind(DereferenceOperations)
            || (parent.Kind == OperationKindEx.Conversion && IsParameterDereferenced(parent.ToSonar())));

    private sealed class ArgumentDereferenceWalker : SafeCSharpSyntaxWalker
    {
        private readonly ISet<string> argumentNames;

        public bool DereferencesMethodArguments { get; private set; }

        public ArgumentDereferenceWalker(ISet<string> argumentNames) =>
            this.argumentNames = argumentNames;

        public override void Visit(SyntaxNode node)
        {
            if (!DereferencesMethodArguments && !node.IsAnyKind(LocalFunctionStatement, SimpleLambdaExpression, ParenthesizedLambdaExpression))
            {
                base.Visit(node);
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node) =>
            DereferencesMethodArguments |=
                argumentNames.Contains(node.GetName())
                && node.Ancestors().Any(x => x.IsAnyKind(
                    AwaitExpression,
                    ElementAccessExpression,
                    ForEachStatement,
                    ThrowStatement,
                    SimpleMemberAccessExpression));
    }
}
