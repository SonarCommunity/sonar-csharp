/*
 * SonarSource :: C# :: ITs :: Plugin
 * Copyright (C) 2011-2016 SonarSource SA
 * mailto:contact AT sonarsource DOT com
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
import com.sonar.orchestrator.build.SonarRunner;
import com.sonar.orchestrator.build.SonarScanner;

import java.io.File;
import org.junit.BeforeClass;
import org.junit.ClassRule;
import org.junit.Test;
import org.sonarqube.ws.WsMeasures.Measure;

import static com.sonar.it.csharp.Tests.getComponent;
import static com.sonar.it.csharp.Tests.getMeasure;
import static com.sonar.it.csharp.Tests.getMeasureAsInt;
import static org.apache.commons.lang.StringUtils.countMatches;
import static org.fest.assertions.Assertions.assertThat;

public class MetricsTest {

  private static final String PROJECT = "MetricsTest";
  private static final String DIRECTORY = "MetricsTest:foo";
  private static final String FILE = "MetricsTest:foo/Class1.cs";

  @ClassRule
  public static final Orchestrator orchestrator = Tests.ORCHESTRATOR;

  @BeforeClass
  public static void init() {
    orchestrator.resetData();

    SonarScanner build = Tests.createSonarScannerBuild()
      .setProjectDir(new File("projects/MetricsTest/"))
      .setProjectKey("MetricsTest")
      .setProjectName("MetricsTest")
      .setProjectVersion("1.0")
      .setSourceDirs(".")
      .setProperty("sonar.sourceEncoding", "UTF-8")
      .setProperty("sonar.verbose", "true")
      .setProfile("no_rule");
    orchestrator.executeBuild(build);
  }

  @Test
  public void projectIsAnalyzed() {
    assertThat(getComponent(PROJECT).getName()).isEqualTo("MetricsTest");
    assertThat(getComponent(DIRECTORY).getName()).isEqualTo("foo");
    assertThat(getComponent(FILE).getName()).isEqualTo("Class1.cs");
  }

  /* Files */

  @Test
  public void filesAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("files")).isEqualTo(3);
  }

  @Test
  public void filesAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("files")).isEqualTo(2);
  }

  @Test
  public void filesAtFileLevel() {
    assertThat(getFileMeasureAsInt("files")).isEqualTo(1);
  }

  /* Statements */

  @Test
  public void statementsAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("statements")).isEqualTo(12);
  }

  @Test
  public void statementsAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("statements")).isEqualTo(8);
  }

  @Test
  public void statementsAtFileLevel() {
    assertThat(getFileMeasureAsInt("statements")).isEqualTo(4);
  }

  /* Complexity */

  @Test
  public void complexityAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("complexity")).isEqualTo(6);
  }

  @Test
  public void complexityAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("complexity")).isEqualTo(4);
  }

  @Test
  public void complexityAtFileLevel() {
    assertThat(getFileMeasureAsInt("complexity")).isEqualTo(2);
  }

  @Test
  public void complexityInClassesAtFileLevel() {
    assertThat(getFileMeasureAsInt("complexity_in_classes")).isEqualTo(2);
  }

  @Test
  public void complexityInFunctionsAtFileLevel() {
    assertThat(getFileMeasureAsInt("complexity_in_functions")).isEqualTo(2);
  }

  /* Lines */

  @Test
  public void linesAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("lines")).isEqualTo(99);
  }

  @Test
  public void linesAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("lines")).isEqualTo(66);
  }

  @Test
  public void linesAtFileLevel() {
    assertThat(getFileMeasureAsInt("lines")).isEqualTo(33);
  }

  /* Lines of code */

  @Test
  public void linesOfCodeAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("ncloc")).isEqualTo(81);
  }

  @Test
  public void linesOfCodeAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("ncloc")).isEqualTo(54);
  }

  @Test
  public void linesOfCodeAtFileLevel() {
    assertThat(getFileMeasureAsInt("ncloc")).isEqualTo(27);
  }

  /* Comment lines */

  @Test
  public void commentLinesAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("comment_lines")).isEqualTo(6);
  }

  @Test
  public void commentLinesAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("comment_lines")).isEqualTo(4);
  }

  @Test
  public void commentLinesAtFileLevel() {
    assertThat(getFileMeasureAsInt("comment_lines")).isEqualTo(2);
  }

  /* Functions */

  @Test
  public void functionsAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("functions")).isEqualTo(6);
  }

  @Test
  public void functionsAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("functions")).isEqualTo(4);
  }

  @Test
  public void functionsAtFileLevel() {
    assertThat(getFileMeasureAsInt("functions")).isEqualTo(2);
  }

  /* Classes */

  @Test
  public void classesAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("classes")).isEqualTo(6);
  }

  @Test
  public void classesAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("classes")).isEqualTo(4);
  }

  @Test
  public void classesAtFileLevel() {
    assertThat(getFileMeasureAsInt("classes")).isEqualTo(2);
  }

  /* Public API */

  @Test
  public void publicApiAtProjectLevel() {
    assertThat(getProjectMeasureAsInt("public_api")).isEqualTo(6);
    assertThat(getProjectMeasureAsInt("public_undocumented_api")).isEqualTo(3);
  }

  @Test
  public void publicApiAtDirectoryLevel() {
    assertThat(getDirectoryMeasureAsInt("public_api")).isEqualTo(4);
    assertThat(getDirectoryMeasureAsInt("public_undocumented_api")).isEqualTo(2);
  }

  @Test
  public void publicApiAtFileLevel() {
    assertThat(getFileMeasureAsInt("public_api")).isEqualTo(2);
    assertThat(getFileMeasureAsInt("public_undocumented_api")).isEqualTo(1);
  }

  /* Complexity distribution */

  @Test
  public void complexityDistributionAtProjectLevel() {
    assertThat(getProjectMeasure("function_complexity_distribution").getValue()).isEqualTo("1=6;2=0;4=0;6=0;8=0;10=0;12=0");
    assertThat(getDirectoryMeasure("file_complexity_distribution").getValue()).isEqualTo("0=2;5=0;10=0;20=0;30=0;60=0;90=0");
  }

  @Test
  public void complexityDistributionAtDirectoryLevel() {
    assertThat(getDirectoryMeasure("function_complexity_distribution").getValue()).isEqualTo("1=4;2=0;4=0;6=0;8=0;10=0;12=0");
    assertThat(getDirectoryMeasure("file_complexity_distribution").getValue()).isEqualTo("0=2;5=0;10=0;20=0;30=0;60=0;90=0");
  }

  @Test
  public void complexityDistributionAtFileLevel() {
    assertThat(getFileMeasureAsInt("function_complexity_distribution")).isNull();
    assertThat(getFileMeasureAsInt("file_complexity_distribution")).isNull();
  }

  @Test
  public void linesOfCodeByLine() {
    String value = getFileMeasure("ncloc_data").getValue();

    assertThat(value).contains("1=1");
    assertThat(value).contains("2=1");
    assertThat(value).contains("3=1");
    assertThat(value).contains("4=1");
    assertThat(value).contains("5=1");

    assertThat(value).contains("9=1");
    assertThat(value).contains("10=1");

    assertThat(value).contains("12=1");
    assertThat(value).contains("13=1");
    assertThat(value).contains("14=1");
    assertThat(value).contains("15=1");
    assertThat(value).contains("16=1");
    assertThat(value).contains("17=1");
    assertThat(value).contains("18=1");
    assertThat(value).contains("19=1");
    assertThat(value).contains("20=1");
    assertThat(value).contains("21=1");

    assertThat(value).contains("23=1");
    assertThat(value).contains("24=1");
    assertThat(value).contains("25=1");
    assertThat(value).contains("26=1");
    assertThat(value).contains("27=1");
    assertThat(value).contains("28=1");
    assertThat(value).contains("29=1");
    assertThat(value).contains("30=1");
    assertThat(value).contains("31=1");
    assertThat(value).contains("32=1");

    assertThat(value.length()).isEqualTo(128); // No other line
  }

  @Test
  public void commentsByLine() {
    assertThat(getFileMeasure("comment_lines_data").getValue()).contains("7=1");
    assertThat(getFileMeasure("comment_lines_data").getValue()).contains("11=1");
    assertThat(countMatches(getFileMeasure("comment_lines_data").getValue(), "=1")).isEqualTo(2);
  }

  /* Helper methods */

  private Measure getProjectMeasure(String metricKey) {
    return getMeasure(PROJECT, metricKey);
  }

  private Integer getProjectMeasureAsInt(String metricKey) {
    return getMeasureAsInt(PROJECT, metricKey);
  }

  private Measure getDirectoryMeasure(String metricKey) {
    return getMeasure(DIRECTORY, metricKey);
  }

  private Integer getDirectoryMeasureAsInt(String metricKey) {
    return getMeasureAsInt(DIRECTORY, metricKey);
  }

  private Measure getFileMeasure(String metricKey) {
    return getMeasure(FILE, metricKey);
  }

  private Integer getFileMeasureAsInt(String metricKey) {
    return getMeasureAsInt(FILE, metricKey);
  }

}
