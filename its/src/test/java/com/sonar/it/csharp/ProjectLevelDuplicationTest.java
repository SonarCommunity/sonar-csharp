/*
 * SonarSource :: C# :: ITs :: Plugin
 * Copyright (C) 2011-2023 SonarSource SA
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
package com.sonar.it.csharp;

import com.sonar.it.shared.TestUtils;
import com.sonar.orchestrator.build.BuildResult;
import java.util.List;
import java.util.stream.Collectors;
import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.TemporaryFolder;
import org.sonarqube.ws.Issues;

import static com.sonar.it.csharp.Tests.ORCHESTRATOR;
import static com.sonar.it.csharp.Tests.getComponent;
import static com.sonar.it.csharp.Tests.getIssues;
import static org.assertj.core.api.Assertions.assertThat;

public class ProjectLevelDuplicationTest {

  @Rule
  public TemporaryFolder temp = TestUtils.createTempFolder();

  @Before
  public void init() {
    TestUtils.reset(ORCHESTRATOR);
  }

  @Test
  public void containsOnlyOneProjectLevelIssue() throws Exception {
    BuildResult buildResult = Tests.analyzeProject(temp, "ProjectLevelDuplication", null);

    assertThat(getComponent("ProjectLevelDuplication")).isNotNull();

    List<Issues.Issue> projectLevelIssues = getIssues("ProjectLevelDuplication")
      .stream()
      .filter(x -> x.getRule().equals("csharpsquid:S3904"))
      .collect(Collectors.toList());
    assertThat(projectLevelIssues).hasSize(1);
  }
}
