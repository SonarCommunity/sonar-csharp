/*
 * SonarSource :: .NET :: Shared library
 * Copyright (C) 2014-2020 SonarSource SA
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
package org.sonar.plugins.dotnet.tests;

import java.util.Optional;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.sonar.api.batch.fs.FilePredicates;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.scanner.ScannerSide;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;

@ScannerSide
public class ScannerFileService implements FileService {
  private static final Logger LOG = Loggers.get(ScannerFileService.class);
  private static final Pattern DETERMINISTIC_SOURCE_PATH_PREFIX = Pattern.compile("^(/_\\d*/)");
  private FileSystem fileSystem;
  private String languageKey;

  public ScannerFileService(String languageKey, FileSystem fileSystem) {
    this.languageKey = languageKey;
    this.fileSystem = fileSystem;
  }

  public boolean isSupportedAbsolute(String absolutePath) {
    FilePredicates fp = fileSystem.predicates();
    return fileSystem.hasFiles(
      fp.and(
        fp.hasAbsolutePath(absolutePath),
        fp.hasLanguage(languageKey)));
  }

  public Optional<InputFile> getFileByRelativePath(String filePath) {
    Matcher matcher = DETERMINISTIC_SOURCE_PATH_PREFIX.matcher(filePath.replace('\\', '/'));
    if (matcher.find()) {
      String relativePath = matcher.replaceFirst("");
      Iterable<InputFile> files = fileSystem.inputFiles(fileSystem.predicates().hasLanguage(languageKey));
      int count = 0;
      InputFile foundFile = null;
      for (InputFile file : files) {
        String path = file.uri().getPath();
        if (path.endsWith(relativePath)) {
          count++;
          foundFile = file;
        }
      }
      if (count == 1) {
        LOG.trace("Found indexed file '{}' for '{}' (normalized to '{}').", foundFile.uri().getPath(), filePath, relativePath);
        return Optional.of(foundFile);
      } else {
        LOG.debug("Found {} indexed files for '{}' (normalized to '{}'). Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.",
          count, filePath, relativePath);
        return Optional.empty();
      }
    }
    LOG.debug("Did not find deterministic source path in '{}'. Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.", filePath);
    return Optional.empty();
  }
}
