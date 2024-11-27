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

namespace SonarAnalyzer.Core.Trackers;

public class PropertyAccessContext : SyntaxBaseContext
{
    public string PropertyName { get; }
    public Lazy<IPropertySymbol> PropertySymbol { get; }

    public PropertyAccessContext(SonarSyntaxNodeReportingContext context, string propertyName) : base(context)
    {
        PropertyName = propertyName;
        PropertySymbol = new Lazy<IPropertySymbol>(() => context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IPropertySymbol);
    }

    public PropertyAccessContext(SyntaxNode node, SemanticModel model, string propertyName) : base(node, model)
    {
        PropertyName = propertyName;
        PropertySymbol = new Lazy<IPropertySymbol>(() => model.GetSymbolInfo(node).Symbol as IPropertySymbol);
    }
}
