﻿namespace Tests.Diagnostics
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSonarQubeAnalyzer.Diagnostics;
    using System.Collections.Immutable;

    [TestClass]
    public class MagicNumberTest
    {
        [TestMethod]
        public void MagicNumber()
        {
            var diagnostic = new MagicNumber();
            diagnostic.Exceptions = ImmutableHashSet.Create("0", "1", "0x0", "0x00", ".0", ".1", "0.0", "1.0");
            Verifier.Verify(@"TestCases\MagicNumber.cs", diagnostic);
        }
    }
}