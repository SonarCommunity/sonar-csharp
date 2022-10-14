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

using System.Diagnostics;

namespace SonarAnalyzer.UnitTest.Helpers
{
    /// <summary>
    /// Some of the tests cover exceptions in which we have asserts as well, we want to ignore those asserts during tests
    /// </summary>
    public sealed class AssertIgnoreScope : IDisposable
    {
        private readonly DefaultTraceListener listener;

        public AssertIgnoreScope()
        {
            listener = Trace.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
            if (listener != null)
            {
                Trace.Listeners.Remove(listener);
            }
        }

        public void Dispose()
        {
            if (listener != null)
            {
                Trace.Listeners.Add(listener);
            }
        }
    }
}
