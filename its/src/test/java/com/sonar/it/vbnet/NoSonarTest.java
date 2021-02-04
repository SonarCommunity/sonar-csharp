/*
 * SonarSource :: C# :: ITs :: Plugin
 * Copyright (C) 2011-2021 SonarSource SA
 * mailto:info AT sonarsource DOT com
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
package com.sonar.it.vbnet;

import com.sonar.it.shared.TestUtils;
import com.sonar.orchestrator.Orchestrator;
import com.sonar.orchestrator.build.ScannerForMSBuild;
import java.nio.file.Path;
import org.junit.BeforeClass;
import org.junit.ClassRule;
import org.junit.Test;
import org.junit.rules.TemporaryFolder;

import static com.sonar.it.vbnet.Tests.ORCHESTRATOR;
import static com.sonar.it.vbnet.Tests.getMeasureAsInt;
import static org.assertj.core.api.Assertions.assertThat;

public class NoSonarTest {

  @ClassRule
  public static final Orchestrator orchestrator = Tests.ORCHESTRATOR;

  @ClassRule
  public static final TemporaryFolder temp = TestUtils.createTempFolder();

  private static final String PROJECT = "VbNoSonarTest";

  @BeforeClass
  public static void init() throws Exception {
    TestUtils.reset(orchestrator);

    // Without setting the testProjectPattern, the VbNoSonarTest project is considered as a Test project :)
    Tests.analyzeProject(temp, PROJECT, "vbnet_class_name", "sonar.msbuild.testProjectPattern", "noTests");
  }

  @Test
  public void filesAtProjectLevel() {
    assertThat(getMeasureAsInt(PROJECT, "violations")).isEqualTo(2);
  }

}
