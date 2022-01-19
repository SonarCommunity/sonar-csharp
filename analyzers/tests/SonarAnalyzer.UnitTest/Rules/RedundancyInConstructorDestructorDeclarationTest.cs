﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class RedundancyInConstructorDestructorDeclarationTest
    {
        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\RedundancyInConstructorDestructorDeclaration.cs", new RedundancyInConstructorDestructorDeclaration());

#if NET
        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Library(@"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp9.cs", new RedundancyInConstructorDestructorDeclaration());

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp10.cs", new RedundancyInConstructorDestructorDeclaration());

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CodeFix_CSharp9() =>
            OldVerifier.VerifyCodeFix<RedundancyInConstructorDestructorDeclarationCodeFix>(
                @"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp9.cs",
                @"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp9.Fixed.cs",
                new RedundancyInConstructorDestructorDeclaration(),
                RedundancyInConstructorDestructorDeclarationCodeFix.TitleRemoveBaseCall,
                ParseOptionsHelper.FromCSharp9);

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CodeFix_CSharp10() =>
            OldVerifier.VerifyCodeFix<RedundancyInConstructorDestructorDeclarationCodeFix>(
                @"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp10.cs",
                @"TestCases\RedundancyInConstructorDestructorDeclaration.CSharp10.Fixed.cs",
                new RedundancyInConstructorDestructorDeclaration(),
                RedundancyInConstructorDestructorDeclarationCodeFix.TitleRemoveConstructor,
                ParseOptionsHelper.FromCSharp10);
#endif

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CodeFix_BaseCall() =>
            OldVerifier.VerifyCodeFix<RedundancyInConstructorDestructorDeclarationCodeFix>(
                @"TestCases\RedundancyInConstructorDestructorDeclaration.cs",
                @"TestCases\RedundancyInConstructorDestructorDeclaration.BaseCall.Fixed.cs",
                new RedundancyInConstructorDestructorDeclaration(),
                RedundancyInConstructorDestructorDeclarationCodeFix.TitleRemoveBaseCall);

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CodeFix_Constructor() =>
            OldVerifier.VerifyCodeFix<RedundancyInConstructorDestructorDeclarationCodeFix>(
                @"TestCases\RedundancyInConstructorDestructorDeclaration.cs",
                @"TestCases\RedundancyInConstructorDestructorDeclaration.Constructor.Fixed.cs",
                new RedundancyInConstructorDestructorDeclaration(),
                RedundancyInConstructorDestructorDeclarationCodeFix.TitleRemoveConstructor);

        [TestMethod]
        public void RedundancyInConstructorDestructorDeclaration_CodeFix_Destructor() =>
            OldVerifier.VerifyCodeFix<RedundancyInConstructorDestructorDeclarationCodeFix>(
                @"TestCases\RedundancyInConstructorDestructorDeclaration.cs",
                @"TestCases\RedundancyInConstructorDestructorDeclaration.Destructor.Fixed.cs",
                new RedundancyInConstructorDestructorDeclaration(),
                RedundancyInConstructorDestructorDeclarationCodeFix.TitleRemoveDestructor);
    }
}
