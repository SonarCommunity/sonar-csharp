/*
 * SonarSource :: .NET :: Core
 * Copyright (C) 2014-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */
package org.sonar.plugins.dotnet.tests;

import org.junit.Test;

import static org.assertj.core.api.Assertions.assertThat;

public class BranchPointTest {
  @Test
  public void givenBranchPointData_getUniqueKey_containsFilePathStartLineAndOffsets(){
    assertThat(new BranchPoint("path", 1, 2, 3, 4, 5, "coverageIdentifier").getUniqueKey()).isEqualTo("path-1-2-3-4");
  }
}
