﻿namespace Tests.Diagnostics
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSonarQubeAnalyzer.Diagnostics;

    [TestClass]
    public class MethodNameTest
    {
        [TestMethod]
        public void MethodName()
        {
            var diagnostic = new MethodName();
            diagnostic.Convention = "^[A-Z][a-zA-Z0-9]+$";
            Verifier.Verify(@"TestCases\MethodName.cs", diagnostic);
        }
    }
}