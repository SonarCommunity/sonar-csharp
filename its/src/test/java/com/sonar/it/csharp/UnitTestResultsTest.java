/*
 * SonarSource :: C# :: ITs :: Plugin
 * Copyright (C) 2011-2017 SonarSource SA
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

import com.sonar.orchestrator.Orchestrator;
import java.io.IOException;
import java.nio.file.Path;
import org.junit.Before;
import org.junit.ClassRule;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.TemporaryFolder;

import static com.sonar.it.csharp.Tests.getMeasure;
import static com.sonar.it.csharp.Tests.getMeasureAsInt;
import static org.assertj.core.api.Assertions.assertThat;

public class UnitTestResultsTest {

  @ClassRule
  public static final Orchestrator orchestrator = Tests.ORCHESTRATOR;

  @Rule
  public TemporaryFolder temp = new TemporaryFolder();

  @Before
  public void init() throws Exception {
    orchestrator.resetData();
  }

  @Test
  public void should_not_import_unit_test_results_without_report() throws Exception {
    analyzeTestProject();

    assertThat(getMeasure("UnitTestResultsTest", "tests")).isNull();
    assertThat(getMeasure("UnitTestResultsTest", "test_errors")).isNull();
    assertThat(getMeasure("UnitTestResultsTest", "test_failures")).isNull();
    assertThat(getMeasure("UnitTestResultsTest", "skipped_tests")).isNull();
  }

  @Test
  public void vstest() throws Exception {
    analyzeTestProject("sonar.cs.vstest.reportsPaths", "reports/vstest.trx");

    assertThat(getMeasureAsInt("UnitTestResultsTest", "tests")).isEqualTo(32);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "test_errors")).isEqualTo(1);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "test_failures")).isEqualTo(10);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "skipped_tests")).isEqualTo(7);
  }

  @Test
  public void nunit() throws Exception {
    analyzeTestProject("sonar.cs.nunit.reportsPaths", "reports/nunit.xml");

    assertThat(getMeasureAsInt("UnitTestResultsTest", "tests")).isEqualTo(196);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "test_errors")).isEqualTo(30);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "test_failures")).isEqualTo(20);
    assertThat(getMeasureAsInt("UnitTestResultsTest", "skipped_tests")).isEqualTo(7);
  }

  @Test
  public void should_support_wildcard_patterns() throws Exception {
    analyzeTestProject("sonar.cs.vstest.reportsPaths", "reports/*.trx");

    assertThat(getMeasureAsInt("UnitTestResultsTest", "tests")).isEqualTo(32);
  }

  private void analyzeTestProject(String... keyValues) throws IOException {
    Path projectDir = Tests.projectDir(temp, "UnitTestResultsTest");
    orchestrator.executeBuild(Tests.newScanner(projectDir)
      .addArgument("begin")
      .setProjectKey("UnitTestResultsTest")
      .setProjectName("UnitTestResultsTest")
      .setProjectVersion("1.0")
      .setProfile("no_rule")
      .setProperties(keyValues));

    Tests.runMSBuild(orchestrator, projectDir, "/t:Rebuild");

    orchestrator.executeBuild(Tests.newScanner(projectDir)
      .addArgument("end"));
  }
}
