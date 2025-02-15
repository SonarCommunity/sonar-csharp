﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

namespace SonarAnalyzer.CSharp.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseConstantLoggingTemplate : SonarDiagnosticAnalyzer
{
    private const string DiagnosticId = "S2629";
    private const string MessageFormat = "{0}";
    private const string OnUsingStringInterpolation = "Don't use string interpolation in logging message templates.";
    private const string OnUsingStringFormat = "Don't use String.Format in logging message templates.";
    private const string OnUsingStringConcatenation = "Don't use string concatenation in logging message templates.";

    private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

    private static readonly ImmutableDictionary<SyntaxKind, string> Messages = new Dictionary<SyntaxKind, string>
    {
        {SyntaxKind.AddExpression, OnUsingStringConcatenation},
        {SyntaxKind.InterpolatedStringExpression, OnUsingStringInterpolation},
        {SyntaxKind.InvocationExpression, OnUsingStringFormat},
    }.ToImmutableDictionary();

    private static readonly ImmutableArray<KnownType> LoggerTypes = ImmutableArray.Create(
        KnownType.Castle_Core_Logging_ILogger,
        KnownType.log4net_ILog,
        KnownType.log4net_Util_ILogExtensions,
        KnownType.Microsoft_Extensions_Logging_LoggerExtensions,
        KnownType.NLog_ILogger,
        KnownType.NLog_ILoggerBase,
        KnownType.NLog_ILoggerExtensions,
        KnownType.Serilog_ILogger,
        KnownType.Serilog_Log);

    private static readonly ImmutableHashSet<string> LoggerMethodNames = ImmutableHashSet.Create(
        "ConditionalDebug",
        "ConditionalTrace",
        "Debug",
        "DebugFormat",
        "Error",
        "ErrorFormat",
        "Fatal",
        "FatalFormat",
        "Info",
        "InfoFormat",
        "Information",
        "Log",
        "LogCritical",
        "LogDebug",
        "LogError",
        "LogFormat",
        "LogInformation",
        "LogTrace",
        "LogWarning",
        "Trace",
        "TraceFormat",
        "Verbose",
        "Warn",
        "WarnFormat",
        "Warning");

    private static readonly ImmutableHashSet<string> LogMessageParameterNames = ImmutableHashSet.Create(
        "format",
        "message",
        "messageTemplate");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterNodeAction(c =>
        {
            var invocation = (InvocationExpressionSyntax)c.Node;
            if (LoggerMethodNames.Contains(invocation.GetName())
                && c.Model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                && !IsLog4NetExceptionMethod(method)
                && LoggerTypes.Any(x => x.Matches(method.ContainingType))
                && method.Parameters.FirstOrDefault(x => LogMessageParameterNames.Contains(x.Name)) is { } messageParameter
                && ArgumentValue(invocation, method, messageParameter) is { } argumentValue
                && InvalidSyntaxNode(argumentValue, c.Model) is { } invalidNode)
            {
                c.ReportIssue(Rule, invalidNode, Messages[invalidNode.Kind()]);
            }
        },
        SyntaxKind.InvocationExpression);

    private static CSharpSyntaxNode ArgumentValue(InvocationExpressionSyntax invocation, IMethodSymbol method, IParameterSymbol parameter)
    {
        if (invocation.ArgumentList.Arguments.FirstOrDefault(x => x.NameColon?.GetName() == parameter.Name) is { } argument)
        {
            return argument.Expression;
        }
        else
        {
            var paramIndex = method.Parameters.IndexOf(parameter);
            return invocation.ArgumentList.Arguments[paramIndex].Expression;
        }
    }

    private static bool IsLog4NetExceptionMethod(IMethodSymbol method) =>
        method.ContainingType.Is(KnownType.log4net_ILog) && method.Parameters.Any(x => x.Type.Is(KnownType.System_Exception));

    private static SyntaxNode InvalidSyntaxNode(SyntaxNode messageArgument, SemanticModel model) =>
        messageArgument.DescendantNodesAndSelf().FirstOrDefault(x =>
            (x as InterpolatedStringExpressionSyntax is { } interpolatedString && !interpolatedString.HasConstantValue(model))
            || (x is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } concatenation && !AllMembersAreConstantStrings(concatenation, model))
            || IsStringFormatInvocation(x, model));

    private static bool AllMembersAreConstantStrings(BinaryExpressionSyntax addExpression, SemanticModel model) =>
        IsConstantStringOrConcatenation(addExpression.Left, model) && IsConstantStringOrConcatenation(addExpression.Right, model);

    private static bool IsConstantStringOrConcatenation(SyntaxNode node, SemanticModel model) =>
        node.Kind() == SyntaxKind.StringLiteralExpression
        || (node as InterpolatedStringExpressionSyntax is { } interpolatedString && interpolatedString.HasConstantValue(model))
        || (node.Kind() == SyntaxKind.IdentifierName && model.GetSymbolInfo(node).Symbol is IFieldSymbol { HasConstantValue: true } or ILocalSymbol { HasConstantValue: true})
        || (node is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } concatenation
            && AllMembersAreConstantStrings(concatenation, model));

    private static bool IsStringFormatInvocation(SyntaxNode node, SemanticModel model) =>
        node is InvocationExpressionSyntax invocation
        && node.GetName() == "Format"
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
        && KnownType.System_String.Matches(method.ContainingType);
}
