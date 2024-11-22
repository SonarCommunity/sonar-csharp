﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2024 SonarSource SA
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

using SonarAnalyzer.Core.Trackers;
using SonarAnalyzer.VisualBasic.Core.Trackers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class JwtSigned : JwtSignedBase<SyntaxKind, InvocationExpressionSyntax>
    {
        protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

        public JwtSigned() : base(AnalyzerConfiguration.AlwaysEnabled) { }

        protected override BuilderPatternCondition<SyntaxKind, InvocationExpressionSyntax> CreateBuilderPatternCondition() =>
            new VisualBasicBuilderPatternCondition(JwtBuilderConstructorIsSafe, JwtBuilderDescriptors(
                invocation =>
                    invocation.ArgumentList?.Arguments.Count != 1
                    || !invocation.ArgumentList.Arguments.Single().GetExpression().RemoveParentheses().IsKind(SyntaxKind.FalseLiteralExpression)));
    }
}
