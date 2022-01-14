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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;
#if NET
using SonarAnalyzer.UnitTest.MetadataReferences;
#endif
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class DisposableMemberInNonDisposableClassTest
    {
        [TestMethod]
        public void DisposableMemberInNonDisposableClass() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\DisposableMemberInNonDisposableClass.cs",
                                    new DisposableMemberInNonDisposableClass(),
                                    ParseOptionsHelper.FromCSharp8);

#if NET
        [TestMethod]
        public void DisposableMemberInNonDisposableClass_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\DisposableMemberInNonDisposableClass.CSharp9.cs", new DisposableMemberInNonDisposableClass());

        [TestMethod]
        public void DisposableMemberInNonDisposableClass_IAsyncDisposable() => // IAsyncDisposable is available only on .Net Core
            OldVerifier.VerifyCSharpAnalyzer(@"
namespace Namespace
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestClass : IAsyncDisposable
    {
        private CancellationTokenSource cancellationTokenSource;

        public Task MethodAsync()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            return Task.Delay(1000, this.cancellationTokenSource.Token);
        }

        public ValueTask DisposeAsync()
        {
            this.cancellationTokenSource?.Dispose();
            return new ValueTask();
        }
    }

    public class C1 // Noncompliant, needs to implement IDisposable or IAsyncDisposable
    {
        private IAsyncDisposable disposable;

        public void Init() => disposable = new AsyncDisposable();
    }

    public class C2 : IAsyncDisposable // Implements IAsyncDisposable
    {
        private IAsyncDisposable disposable;

        public void Init() => disposable = new AsyncDisposable();

        public ValueTask DisposeAsync() => disposable.DisposeAsync();
    }

    public class AsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new ValueTask();
    }
}", new DisposableMemberInNonDisposableClass(), additionalReferences: NetStandardMetadataReference.Netstandard);
#endif
    }
}
