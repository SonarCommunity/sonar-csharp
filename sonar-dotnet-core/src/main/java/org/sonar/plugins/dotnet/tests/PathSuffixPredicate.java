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

import org.sonar.api.batch.fs.FilePredicate;
import org.sonar.api.batch.fs.InputFile;

class PathSuffixPredicate implements FilePredicate {
  private final String pathSuffix;

  PathSuffixPredicate(String pathSuffix) {
    this.pathSuffix = pathSuffix;
  }

  @Override
  public boolean apply(InputFile inputFile) {
    return inputFile.uri().getPath().endsWith(pathSuffix);
  }

  // visible for testing
  String getPathSuffix() {
    return pathSuffix;
  }
}
