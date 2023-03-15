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

using System.Runtime.CompilerServices;

namespace SonarAnalyzer.Helpers
{
    public static class HashCode    // Replacement for System.HashCode that is available from .NET Standard 2.1
    {
        private const uint Seed = 374761393U;
        private const uint PreMultiplier = 3266489917U;
        private const uint PostMultiplier = 668265263U;
        private const int RotateOffset = 17;

        public static int DictionaryContentHash<TKey, TValue>(IDictionary<TKey, TValue> dictionary) =>
            dictionary.Aggregate(0, (seed, kvp) => seed ^ Combine(kvp.Key, kvp.Value));

        public static int EnumerableContentHash<TValue>(IEnumerable<TValue> enumerable) =>
            enumerable.Aggregate(0, (seed, x) => Combine(seed, x));

        public static int Combine<T1, T2>(T1 a, T2 b) =>
            (int)Seed.AddHash(a?.GetHashCode()).AddHash(b?.GetHashCode());

        public static int Combine<T1, T2, T3>(T1 a, T2 b, T3 c) =>
            (int)Combine(a, b).AddHash(c?.GetHashCode());

        public static int Combine<T1, T2, T3, T4>(T1 a, T2 b, T3 c, T4 d) =>
            (int)Combine(a, b, c).AddHash(d?.GetHashCode());

        public static int Combine<T1, T2, T3, T4, T5>(T1 a, T2 b, T3 c, T4 d, T5 e) =>
            (int)Combine(a, b, c, d).AddHash(e?.GetHashCode());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddHash(this int hash, int? value) =>
            ((uint)hash).AddHash(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddHash(this uint hash, int? value) =>
            RotateLeft(hash + (uint)(value ?? 0) * PreMultiplier) * PostMultiplier;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint value) =>
            (value << RotateOffset) | (value >> (sizeof(int) - RotateOffset));
    }
}
