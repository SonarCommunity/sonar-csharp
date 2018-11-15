﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2018 SonarSource SA
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

using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class UsingCookiesBase<TSyntaxKind> : SonarDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected const string DiagnosticId = "S2255";
        protected const string MessageFormat = "Make sure that this cookie is used safely.";

        protected PropertyAccessTracker<TSyntaxKind> PropertyAccessTracker { get; set; }

        protected ObjectCreationTracker<TSyntaxKind> ObjectCreationTracker { get; set; }

        protected ElementAccessTracker<TSyntaxKind> ElementAccessTracker { get; set; }

        protected InvocationTracker<TSyntaxKind> InvocationTracker { get; set; }

        protected override void Initialize(SonarAnalysisContext context)
        {
            PropertyAccessTracker.Track(context,
                PropertyAccessTracker.MatchSimpleNames(
                    new MethodSignature(KnownType.System_Web_HttpRequestBase, "Cookies"),
                    new MethodSignature(KnownType.System_Web_HttpCookie, "Value"),
                    new MethodSignature(KnownType.System_Web_HttpCookie, "Values")));

            ObjectCreationTracker.Track(context,
                ObjectCreationTracker.MatchConstructors(KnownType.System_Web_HttpCookie),
                ObjectCreationTracker.ArgumentAtIndexIs(1, KnownType.System_String));

            ElementAccessTracker.Track(context,
                ElementAccessTracker.MatchIndexersOn(KnownType.System_Web_HttpCookie),
                ElementAccessTracker.WithArguments(KnownType.System_String));

            ElementAccessTracker.Track(context,
                ElementAccessTracker.MatchIndexersOn(KnownType.Microsoft_AspNetCore_Http_IHeaderDictionary),
                ElementAccessTracker.IndexerIsString("Set-Cookie"));

            ElementAccessTracker.Track(context,
                ElementAccessTracker.MatchIndexersOn(
                    KnownType.Microsoft_AspNetCore_Http_IRequestCookieCollection,
                    KnownType.Microsoft_AspNetCore_Http_IResponseCookies));

            InvocationTracker.Track(context,
                InvocationTracker.MatchSimpleNames(
                    new MethodSignature(KnownType.Microsoft_AspNetCore_Http_IRequestCookieCollection, "TryGetValue"),
                    new MethodSignature(KnownType.Microsoft_AspNetCore_Http_IResponseCookies, "Append")));

            InvocationTracker.Track(context,
                InvocationTracker.MatchSimpleNames(
                    new MethodSignature(KnownType.System_Collections_Generic_IDictionary_TKey_TValue, "Add"),
                    new MethodSignature(KnownType.System_Collections_Generic_IDictionary_TKey_TValue_VB, "Add")),
                InvocationTracker.ParameterAtIndexIsString(0, "Set-Cookie"),
                IsIHeadersDictionary(),
                InvocationTracker.HasParameters(2));
        }

        private static InvocationCondition IsIHeadersDictionary() =>
            (context) =>
            {
                var containingType = context.InvokedMethodSymbol.Value.ContainingType;
                // We already checked if ContainingType is IDictionary, but be defensive and check TypeArguments.Count
                return containingType.TypeArguments.Length == 2
                    && containingType.TypeArguments[0].Is(KnownType.System_String)
                    && containingType.TypeArguments[1].Is(KnownType.Microsoft_Extensions_Primitives_StringValues);
            };
    }
}
