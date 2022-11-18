﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
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

using System.Globalization;

namespace SonarAnalyzer.Common
{
    public class Distribution
    {
        internal ImmutableArray<int> Ranges { private set; get; }

        public IList<int> Values { private set; get; }

        public Distribution(IEnumerable<int> ranges)
        {
            if (ranges == null)
            {
                throw new ArgumentNullException(nameof(ranges));
            }

            Ranges = ranges.OrderBy(i => i).ToImmutableArray();
            Values = new int[Ranges.Length];
        }

        public Distribution Add(int value)
        {
            var i = Ranges.Length - 1;

            while (i > 0 && value < Ranges[i])
            {
                i--;
            }

            Values[i]++;

            return this;
        }

        public override string ToString()
        {
            return string.Join(";",
                Ranges.Zip(Values, (r, v) => string.Format(CultureInfo.InvariantCulture, "{0}={1}", r, v)));
        }
    }
}
