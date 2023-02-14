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

using static SonarAnalyzer.Helpers.KnownAssembly.Predicates;

namespace SonarAnalyzer.Helpers;

public sealed partial class KnownAssembly
{
    private readonly Func<IEnumerable<AssemblyIdentity>, bool> predicate;

    public static KnownAssembly XUnit_Assert { get; } = new(
        And(NameIs("xunit.assert").Or(NameIs("xunit").And(VersionLowerThen("2.0"))),
            PublicKeyTokenIs("8d05b1bb7a6fdb6c")));

    internal KnownAssembly(Func<AssemblyIdentity, bool> predicate, params Func<AssemblyIdentity, bool>[] or)
        : this(predicate is null || or.Any(x => x is null)
              ? throw new ArgumentNullException(nameof(predicate), "All predicates must be non-null.")
              : identities => identities.Any(identitiy => predicate(identitiy) || or.Any(orPredicate => orPredicate(identitiy))))
    {
    }

    internal KnownAssembly(Func<IEnumerable<AssemblyIdentity>, bool> predicate) =>
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    public bool IsReferencedBy(Compilation compilation) =>
        predicate(compilation.ReferencedAssemblyNames);

    internal static Func<AssemblyIdentity, bool> And(Func<AssemblyIdentity, bool> left, Func<AssemblyIdentity, bool> right) =>
        KnownAssemblyExtensions.And(left, right);
}
