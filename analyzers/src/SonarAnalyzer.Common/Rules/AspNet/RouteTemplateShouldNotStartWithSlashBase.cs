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

using System.Collections.Concurrent;

namespace SonarAnalyzer.Rules;

public abstract class RouteTemplateShouldNotStartWithSlashBase<TSyntaxKind>() : SonarDiagnosticAnalyzer<TSyntaxKind>(DiagnosticId)
    where TSyntaxKind : struct
{
    private const string DiagnosticId = "S6931";
    private const string MessageOnlyActions = "Change the paths of the actions of this controller to be relative and adapt the controller route accordingly.";
    private const string MessageActionsAndController = "Change the paths of the actions of this controller to be relative and add a controller route with the common prefix.";

    protected override string MessageFormat => "{0}";

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            if (!compilationStartContext.Compilation.ReferencesControllers())
            {
                return;
            }

            compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
            {
                var symbol = (INamedTypeSymbol)symbolStartContext.Symbol;
                if (symbol.IsControllerType())
                {
                    var controllerActionInfo = new ConcurrentStack<ActionParametersInfo>();
                    symbolStartContext.RegisterSyntaxNodeAction(nodeContext =>
                    {
                        if (nodeContext.SemanticModel.GetDeclaredSymbol(nodeContext.Node) is IMethodSymbol methodSymbol && methodSymbol.IsControllerActionMethod())
                        {
                            controllerActionInfo.Push(new ActionParametersInfo(RouteAttributeTemplateArguments(methodSymbol.GetAttributes())));
                        }
                    }, Language.SyntaxKind.MethodDeclarations);

                    symbolStartContext.RegisterSymbolEndAction(symbolEndContext =>
                        ReportIssues(symbolEndContext, symbol, controllerActionInfo));
                }
            }, SymbolKind.NamedType);
        });

    private void ReportIssues(SonarSymbolReportingContext context, INamedTypeSymbol controllerSymbol, ConcurrentStack<ActionParametersInfo> actions)
    {
        // If one of the following conditions is true, the rule won't raise an issue
        // 1. The controller does not have any actions defined
        // 2. At least one action is not annotated with a route attribute or is annotated with a parameterless attribute
        // 3. There is at least one action with a route template that does not start with '/'
        if (!actions.Any() || actions.Any(x => !x.RouteParameters.Any() || x.RouteParameters.Values.Any(x => !x.StartsWith("/"))))
        {
            return;
        }

        var issueMessage = controllerSymbol.GetAttributes().Any(x => x.AttributeClass.IsAny(KnownType.RouteAttributes) || x.AttributeClass.Is(KnownType.System_Web_Mvc_RoutePrefixAttribute))
            ? MessageOnlyActions
            : MessageActionsAndController;

        var secondaryLocations = actions.SelectMany(x => x.RouteParameters.Keys);
        foreach (var classDeclaration in controllerSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()))
        {
            context.ReportIssue(
                Language.GeneratedCodeRecognizer,
                Diagnostic.Create(Rule, Language.Syntax.NodeIdentifier(classDeclaration)?.GetLocation(), secondaryLocations, issueMessage));
        }
    }

    private static Dictionary<Location, string> RouteAttributeTemplateArguments(ImmutableArray<AttributeData> attributes)
    {
        var templates = new Dictionary<Location, string>();
        foreach (var attribute in attributes)
        {
            if (attribute.GetAttributeRouteTemplate(AspNetMvcHelper.RouteTemplateAttributes) is { } templateParameter)
            {
                templates.Add(attribute.ApplicationSyntaxReference.GetSyntax().GetLocation(), templateParameter);
            }
        }
        return templates;
    }

    private readonly record struct ActionParametersInfo(Dictionary<Location, string> RouteParameters);
}
