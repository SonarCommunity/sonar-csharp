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

using SonarAnalyzer.Common.Walkers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RequestsWithExcessiveLength : RequestsWithExcessiveLengthBase<SyntaxKind, AttributeSyntax>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

        public RequestsWithExcessiveLength() : this(Common.AnalyzerConfiguration.Hotspot) { }

        internal RequestsWithExcessiveLength(IAnalyzerConfiguration analyzerConfiguration) : base(analyzerConfiguration) { }

        protected override void Initialize(SonarParametrizedAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var methodDeclaration = (MethodDeclarationSyntax)c.Node;
                    var body = methodDeclaration.GetBodyOrExpressionBody();

                    if (body is not null
                        && body.DescendantNodes()
                               .OfType<InvocationExpressionSyntax>()
                               .Any(x => x.IsMethodInvocation(KnownType.Microsoft_AspNetCore_Components_Forms_IBrowserFile, "OpenReadStream", c.SemanticModel)))
                    {
                        var walker = new StreamReadSizeCheck(c.SemanticModel, FileUploadSizeLimit);
                        if (walker.SafeVisit(body))
                        {
                            foreach (var location in walker.Locations)
                            {
                                c.ReportIssue(Rule, location);
                            }
                        }
                    }
                }, SyntaxKind.MethodDeclaration);

            base.Initialize(context);
        }

        protected override AttributeSyntax IsInvalidRequestFormLimits(AttributeSyntax attribute, SemanticModel semanticModel) =>
            IsRequestFormLimits(attribute.Name.ToString())
            && attribute.ArgumentList?.Arguments.FirstOrDefault(arg => IsMultipartBodyLengthLimit(arg)) is { } firstArgument
            && semanticModel.GetConstantValue(firstArgument.Expression) is { HasValue: true } constantValue
            && constantValue.Value is int intValue
            && intValue > FileUploadSizeLimit
            && attribute.IsKnownType(KnownType.Microsoft_AspNetCore_Mvc_RequestFormLimitsAttribute, semanticModel)
                ? attribute
                : null;

        protected override AttributeSyntax IsInvalidRequestSizeLimit(AttributeSyntax attribute, SemanticModel semanticModel) =>
            IsRequestSizeLimit(attribute.Name.ToString())
            && attribute.ArgumentList?.Arguments.FirstOrDefault() is { } firstArgument
            && semanticModel.GetConstantValue(firstArgument.Expression) is { HasValue: true } constantValue
            && constantValue.Value is int intValue
            && intValue > FileUploadSizeLimit
            && attribute.IsKnownType(KnownType.Microsoft_AspNetCore_Mvc_RequestSizeLimitAttribute, semanticModel)
                ? attribute
                : null;

        protected override SyntaxNode GetMethodLocalFunctionOrClassDeclaration(AttributeSyntax attribute) =>
            attribute.FirstAncestorOrSelf<SyntaxNode>(node => node is MemberDeclarationSyntax || LocalFunctionStatementSyntaxWrapper.IsInstance(node));

        protected override string AttributeName(AttributeSyntax attribute) =>
            attribute.Name.ToString();

        private static bool IsMultipartBodyLengthLimit(AttributeArgumentSyntax argument) =>
            argument.NameEquals is { } nameEquals
            && nameEquals.Name.Identifier.ValueText.Equals(MultipartBodyLengthLimit);

        private sealed class StreamReadSizeCheck(SemanticModel model, int fileUploadSizeLimit) : SafeCSharpSyntaxWalker
        {
            private const int GetMultipleFilesMaximumFileCount = 10; // Default value for `maximumFileCount` in `InputFileChangeEventArgs.GetMultipleFiles` is 10
            private int numberOfFiles = 1;

            public List<Location> Locations { get; } = new();

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.IsMethodInvocation(KnownType.Microsoft_AspNetCore_Components_Forms_InputFileChangeEventArgs, "GetMultipleFiles", model))
                {
                    numberOfFiles = node.ArgumentList.Arguments.FirstOrDefault() is { } firstArgument
                                    && model.GetConstantValue(firstArgument.Expression) is { HasValue: true } constantValue
                                    && Convert.ToInt32(constantValue.Value) is var count
                                        ? count
                                        : GetMultipleFilesMaximumFileCount;
                }
                if (node.IsMethodInvocation(KnownType.Microsoft_AspNetCore_Components_Forms_IBrowserFile, "OpenReadStream", model))
                {
                    var size = OpenReadStreamInvocationSize(node, model);
                    if (numberOfFiles * size > fileUploadSizeLimit)
                    {
                        Locations.Add(node.GetLocation());
                    }
                }

                base.VisitInvocationExpression(node);
            }

            private static long OpenReadStreamInvocationSize(InvocationExpressionSyntax invocation, SemanticModel model) =>
                invocation.ArgumentList.Arguments.FirstOrDefault() is { } firstArgument
                && model.GetConstantValue(firstArgument.Expression) is { HasValue: true } constantValue
                && Convert.ToInt64(constantValue.Value) is var size
                    ? size
                    : 500 * 1024; // Default `maxAllowedSize` in `IBrowserFile.OpenReadStream` is 500 KB
        }
    }
}
