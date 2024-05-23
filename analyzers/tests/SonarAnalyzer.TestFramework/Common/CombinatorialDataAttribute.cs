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

using System.Globalization;
using System.Reflection;

namespace SonarAnalyzer.TestFramework.Common;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DataValuesAttribute : Attribute
{
    public object[] Values { get; }

    public DataValuesAttribute(params object[] values)
    {
        Values = values;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CombinatorialDataAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var valuesPerParameter = methodInfo.GetParameters().Select(p => p.GetCustomAttribute<DataValuesAttribute>()?.Values
            ?? throw new InvalidOperationException("Combinatorial test requires all parameters to have the [DataValues] attribute set")).ToArray();
        var parameterIndices = new int[valuesPerParameter.Length];

        while (true)
        {
            // Create new arguments
            var arg = new object[parameterIndices.Length];
            for (var i = 0; i < parameterIndices.Length; i++)
            {
                arg[i] = valuesPerParameter[i][parameterIndices[i]];
            }

            yield return arg;

            // Increment indices
            for (int i = parameterIndices.Length - 1; i >= 0; i--)
            {
                parameterIndices[i]++;
                if (parameterIndices[i] >= valuesPerParameter[i].Length)
                {
                    parameterIndices[i] = 0;

                    if (i == 0)
                        yield break;
                }
                else
                    break;
            }
        }
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        if (data != null)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", new object[2]
            {
            methodInfo.Name,
            string.Join(",", data)
            });
        }

        return null!;
    }
}
