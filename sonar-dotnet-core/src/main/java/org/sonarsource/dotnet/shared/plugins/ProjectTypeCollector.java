/*
 * SonarSource :: .NET :: Core
 * Copyright (C) 2014-2025 SonarSource SA
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
package org.sonarsource.dotnet.shared.plugins;

import java.util.Optional;
import org.sonar.api.scanner.ScannerSide;

import static org.sonarsource.dotnet.shared.StringUtils.pluralize;

/**
 * Collects information about what types of files are in each project (MAIN, TEST, both or none).
 * The invoker should make sure that:
 * - no duplicates are added (i.e. call twice for same information)
 * - it is called by the Scanner for MSBuild
 */
@ScannerSide
public class ProjectTypeCollector {
  private static final String PROJECT = "project";

  // Each field holds the number of modules (MSBuild projects) based on the type of files inside.
  // modules that have only MAIN files
  private int onlyMain = 0;
  // modules that have only TEST files
  private int onlyTest = 0;
  // modules that have both MAIN and TEST files
  private int mixed = 0;
  // modules that have no files
  private int noFiles = 0;

  public void addProjectInfo(boolean hasMainFiles, boolean hasTestFiles) {
    if (hasMainFiles && hasTestFiles) {
      mixed++;
    } else if (hasMainFiles) {
      onlyMain++;
    } else if (hasTestFiles) {
      onlyTest++;
    } else {
      noFiles++;
    }
  }

  public boolean hasProjects() {
    return countProjects() > 0;
  }

  public Optional<String> getSummary(String languageName) {
    int projectsCount = countProjects();
    if (projectsCount == 0) {
      return Optional.empty();
    }
    StringBuilder stringBuilder = new StringBuilder(String.format("Found %d MSBuild %s %s:", projectsCount, languageName, pluralize(PROJECT, projectsCount)));
    if (onlyMain > 0) {
      stringBuilder.append(String.format(" %d MAIN %s.", onlyMain, pluralize(PROJECT, onlyMain)));
    }
    if (onlyTest > 0) {
      stringBuilder.append(String.format(" %d TEST %s.", onlyTest, pluralize(PROJECT, onlyTest)));
    }
    if (mixed > 0) {
      stringBuilder.append(String.format(" %d with both MAIN and TEST files.", mixed));
    }
    if (noFiles > 0) {
      stringBuilder.append(String.format(" %d with no MAIN nor TEST files.", noFiles));
    }

    return Optional.of(stringBuilder.toString());
  }

  private int countProjects() {
    return onlyMain + onlyTest + mixed + noFiles;
  }
}
