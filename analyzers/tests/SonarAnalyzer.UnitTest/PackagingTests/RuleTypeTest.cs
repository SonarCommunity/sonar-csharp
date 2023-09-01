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

extern alias csharp;
extern alias vbnet;

namespace SonarAnalyzer.UnitTest.PackagingTests
{
    [TestClass]
    public class RuleTypeTest
    {
        // Rules that have been deprecated and deleted.
        // When changing this please do not forget to notify the product teams (SQ, SC, SL).
        private static readonly HashSet<string> DeletedRules = new() { "S1145", "S1697", "S2070", "S2255", "S2278", "S2758", "S3693", "S4142", "S4432", "S4787"};

        [TestMethod]
        public void DetectRuleTypeChanges_CS() =>
            DetectTypeChanges(csharp::SonarAnalyzer.RuleCatalog.Rules, RuleTypeMappingCS.Rules, nameof(RuleTypeMappingCS));

        [TestMethod]
        public void DetectRuleTypeChanges_VB() =>
            DetectTypeChanges(vbnet::SonarAnalyzer.RuleCatalog.Rules, RuleTypeMappingVB.Rules, nameof(RuleTypeMappingVB));

        [AssertionMethod]
        private static void DetectTypeChanges(Dictionary<string, RuleDescriptor> rules, IImmutableDictionary<string, string> expectedTypes, string expectedTypesName)
        {
            var items = Enumerable.Range(1, 10000)
                                  .Select(x => "S" + x)
                                  .Select(x => new
                                  {
                                      RuleId = x,
                                      ActualType = rules.ContainsKey(x) ? rules[x].Type : null,
                                      ExpectedType = expectedTypes.GetValueOrDefault(x)
                                  })
                                  .Where(x => x.ActualType != x.ExpectedType)
                                  .ToList();

            var newRules = items.Where(x => x.ExpectedType is null);
            newRules.Should().BeEmpty($"you need to add the rules in {expectedTypesName}");

            items.Should().NotContain(
                x => x.ActualType == null && !DeletedRules.Contains($"S{x.RuleId}"),
                "YOU SHOULD NEVER DELETE RULES! Rules should not be deleted without careful consideration and prior deprecation. We need to notify the product teams (SQ, SC, SL) as well.");

            var changedRules = items.Where(x => x.ActualType != null && x.ExpectedType != null);
            changedRules.Should().BeEmpty($"you need to change the rule types in {expectedTypesName}.");
        }
    }
}
