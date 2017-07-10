﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
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

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace SonarAnalyzer.Helpers
{
    public static class KnownMethods
    {
        public static bool IsMainMethod(this IMethodSymbol methodSymbol)
        {
            // Based on Microsoft definition: https://msdn.microsoft.com/en-us/library/1y814bzs.aspx
            return methodSymbol != null &&
                methodSymbol.IsStatic &&
                (methodSymbol.ReturnsVoid || methodSymbol.ReturnType.Is(KnownType.System_Int32)) &&
                methodSymbol.Name.Equals("Main", StringComparison.Ordinal) &&
                (methodSymbol.Parameters.Length == 0 ||
                    (methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].IsType(KnownType.System_String_Array)));
        }

        public static bool IsObjectEquals(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                (methodSymbol.IsOverride || methodSymbol.IsInType(KnownType.System_Object)) &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Name == nameof(object.Equals) &&
                methodSymbol.Parameters.Length == 1 &&
                methodSymbol.Parameters[0].Type.Is(KnownType.System_Object) &&
                methodSymbol.ReturnType.Is(KnownType.System_Boolean);
        }

        public static bool IsStaticObjectEquals(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                !methodSymbol.IsOverride &&
                methodSymbol.IsStatic &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Name == nameof(object.Equals) &&
                methodSymbol.IsInType(KnownType.System_Object) &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Parameters[0].Type.Is(KnownType.System_Object) &&
                methodSymbol.Parameters[1].Type.Is(KnownType.System_Object) &&
                methodSymbol.ReturnType.Is(KnownType.System_Boolean);
        }

        public static bool IsObjectGetHashCode(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                (methodSymbol.IsOverride || methodSymbol.IsInType(KnownType.System_Object)) &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Name == nameof(object.GetHashCode) &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.ReturnType.Is(KnownType.System_Int32);
        }

        public static bool IsObjectToString(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                (methodSymbol.IsOverride || methodSymbol.IsInType(KnownType.System_Object)) &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Name == nameof(object.ToString) &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.ReturnType.Is(KnownType.System_String);
        }

        public static bool IsIDisposableDispose(this IMethodSymbol methodSymbol)
        {
            const string explicitName = "System.IDisposable.Dispose";
            return methodSymbol != null &&
                methodSymbol.ReturnsVoid &&
                methodSymbol.Parameters.Length == 0 &&
                (methodSymbol.Name == nameof(IDisposable.Dispose) ||
                 methodSymbol.Name == explicitName);
        }

        public static bool IsIEquatableEquals(this IMethodSymbol methodSymbol)
        {
            const string explicitName = "System.IEquatable.Equals";
            return methodSymbol != null &&
                methodSymbol.Parameters.Length == 1 &&
                methodSymbol.ReturnType.Is(KnownType.System_Boolean) &&
                (methodSymbol.Name == nameof(object.Equals) ||
                methodSymbol.Name == explicitName);
        }

        public static bool IsGetObjectData(this IMethodSymbol methodSymbol)
        {
            const string explicitName = "System.Runtime.Serialization.ISerializable.GetObjectData";
            return methodSymbol != null &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Parameters[0].Type.Is(KnownType.System_Runtime_Serialization_SerializationInfo) &&
                methodSymbol.Parameters[1].Type.Is(KnownType.System_Runtime_Serialization_StreamingContext) &&
                methodSymbol.ReturnsVoid &&
                (methodSymbol.Name == nameof(ISerializable.GetObjectData) ||
                methodSymbol.Name == explicitName);
        }

        public static bool IsSerializationConstructor(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.Constructor &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Parameters[0].Type.Is(KnownType.System_Runtime_Serialization_SerializationInfo) &&
                methodSymbol.Parameters[1].Type.Is(KnownType.System_Runtime_Serialization_StreamingContext);
        }

        public static bool IsArrayClone(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.Name == nameof(Array.Clone) &&
                methodSymbol.ContainingType.Is(KnownType.System_Array);
        }

        public static bool IsEnumerableToList(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.ReducedExtension &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.Name == nameof(Enumerable.ToList) &&
                methodSymbol.ContainingType.Is(KnownType.System_Linq_Enumerable);
        }

        public static bool IsEnumerableCount(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.ReducedExtension &&
                methodSymbol.Name == nameof(Enumerable.Count) &&
                methodSymbol.ContainingType.Is(KnownType.System_Linq_Enumerable);
        }

        public static bool IsEnumerableToArray(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.ReducedExtension &&
                methodSymbol.Parameters.Length == 0 &&
                methodSymbol.Name == nameof(Enumerable.ToArray) &&
                methodSymbol.ContainingType.Is(KnownType.System_Linq_Enumerable);
        }

        public static bool IsGcSuppressFinalize(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.Parameters.Length == 1 &&
                methodSymbol.Name == nameof(GC.SuppressFinalize) &&
                methodSymbol.ContainingType.Is(KnownType.System_GC);
        }

        public static bool IsDebugAssert(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.Name == nameof(Debug.Assert) &&
                methodSymbol.ContainingType.Is(KnownType.System_Diagnostics_Debug);
        }

        public static bool IsOperatorBinaryPlus(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.UserDefinedOperator &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Name == "op_Addition";
        }

        public static bool IsOperatorBinaryMinus(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.UserDefinedOperator &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Name == "op_Subtraction";
        }

        public static bool IsOperatorEquals(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.UserDefinedOperator &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Name == "op_Equality";
        }

        public static bool IsOperatorNotEquals(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.MethodKind == MethodKind.UserDefinedOperator &&
                methodSymbol.Parameters.Length == 2 &&
                methodSymbol.Name == "op_Inequality";
        }

        public static bool IsConsoleWriteLine(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.IsInType(KnownType.System_Console) &&
                methodSymbol.Name == nameof(Console.WriteLine);
        }

        public static bool IsConsoleWrite(this IMethodSymbol methodSymbol)
        {
            return methodSymbol != null &&
                methodSymbol.IsInType(KnownType.System_Console) &&
                methodSymbol.Name == nameof(Console.Write);
        }
    }
}