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

import java.util.Arrays;
import java.util.Collections;
import java.util.Optional;
import org.junit.Rule;
import org.junit.Test;
import org.mockito.ArgumentCaptor;
import org.sonar.api.batch.fs.FilePredicate;
import org.sonar.api.batch.fs.FilePredicates;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.fs.internal.TestInputFileBuilder;
import org.sonar.api.utils.log.LogTester;
import org.sonar.api.utils.log.LoggerLevel;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

public class ScannerFileServiceTest {
  @Rule
  public LogTester logTester = new LogTester();

  @Test
  public void isSupportedAbsolute_passes_correct_argument() {
    // arrange
    FileSystem fs = mock(FileSystem.class);
    FilePredicates filePredicates = mock(FilePredicates.class);

    ArgumentCaptor<String> argumentCaptor = ArgumentCaptor.forClass(String.class);
    when(filePredicates.hasAbsolutePath(argumentCaptor.capture())).thenReturn(mock(FilePredicate.class));
    when(fs.predicates()).thenReturn(filePredicates);
    when(fs.hasFiles(any())).thenReturn(true);

    // act
    ScannerFileService sut = new ScannerFileService("key", fs);
    sut.isSupportedAbsolute("/_/some/path/file.cs");

    // assert
    assertThat(argumentCaptor.getValue()).isEqualTo("/_/some/path/file.cs");
    assertThat(logTester.logs()).isEmpty();
  }

  @Test
  public void isSupportedAbsolute_returns_fileSystem_result_when_true() {
    FileSystem fs = createFileSystemForHasFiles(true);

    ScannerFileService sut = new ScannerFileService("key", fs);
    boolean result = sut.isSupportedAbsolute("/_/some/path/file.cs");

    assertThat(result).isTrue();
    assertThat(logTester.logs()).isEmpty();
  }

  @Test
  public void isSupportedAbsolute_returns_fileSystem_result_when_false() {
    FileSystem fs = createFileSystemForHasFiles(false);

    ScannerFileService sut = new ScannerFileService("key", fs);
    boolean result = sut.isSupportedAbsolute("/_/some/path/file.cs");

    assertThat(result).isFalse();
    assertThat(logTester.logs()).isEmpty();
  }

  @Test
  public void getFileByRelativePath_retrieves_all_files() {
    // arrange
    FileSystem fs = mock(FileSystem.class);
    FilePredicates filePredicates = mock(FilePredicates.class);
    FilePredicate languageKeyMock = mock(FilePredicate.class);

    ArgumentCaptor<FilePredicate> argumentCaptor = ArgumentCaptor.forClass(FilePredicate.class);

    when(fs.inputFiles(argumentCaptor.capture())).thenReturn(Collections.emptyList());

    when(filePredicates.hasLanguage("key")).thenReturn(languageKeyMock);
    when(fs.predicates()).thenReturn(filePredicates);

    // act
    ScannerFileService sut = new ScannerFileService("key", fs);
    sut.getFileByRelativePath("/_/foo");

    // assert
    assertThat(argumentCaptor.getValue()).isEqualTo(languageKeyMock);
  }

  @Test
  public void getFileByRelativePath_when_no_indexed_files_returns_empty() {
    FileSystem fs = createFileSystemForInputFiles(Collections.emptyList());

    // act
    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("/_/some/path/file.cs");

    // assert
    assertThat(result).isEmpty();
    assertThat(logTester.logs(LoggerLevel.DEBUG)).containsExactly("Found 0 indexed files for '/_/some/path/file.cs' (normalized to 'some/path/file.cs'). Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.");
  }

  @Test
  public void getFileByRelativePath_when_indexed_files_do_not_match_returns_empty() {
    FileSystem fs = createFileSystemForInputFiles(Arrays.asList(mockInput("another/path/file.cs"), mockInput("some/file.cs")));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("/_/some/path/file.cs");

    assertThat(result).isEmpty();
    assertThat(logTester.logs(LoggerLevel.DEBUG)).containsExactly("Found 0 indexed files for '/_/some/path/file.cs' (normalized to 'some/path/file.cs'). Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.");
  }

  @Test
  public void getFileByRelativePath_when_multiple_indexed_files_match_returns_empty() {
    FileSystem fs = createFileSystemForInputFiles(Arrays.asList(mockInput("root1/some/path/file.cs"), mockInput("root2/some/path/file.cs")));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("/_/some/path/file.cs");

    assertThat(result).isEmpty();
    assertThat(logTester.logs(LoggerLevel.DEBUG)).containsExactly("Found 2 indexed files for '/_/some/path/file.cs' (normalized to 'some/path/file.cs'). Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.");
  }

  @Test
  public void getFileByRelativePath_when_single_indexed_files_match_returns_file() {
    InputFile expectedResult = mockInput("root/some/path/file.cs");
    FileSystem fs = createFileSystemForInputFiles(Arrays.asList(
      mockInput("one"),
      mockInput("two"),
      expectedResult,
      mockInput("four")));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("/_/path/file.cs");

    assertThat(result).hasValue(expectedResult);
    assertThat(logTester.logs(LoggerLevel.TRACE)).hasSize(1);
    assertThat(logTester.logs(LoggerLevel.TRACE).get(0))
      .startsWith("Found indexed file ")
      .endsWith("/sonar-dotnet-shared-library/mod/root/some/path/file.cs' for '/_/path/file.cs' (normalized to 'path/file.cs').");
  }

  @Test
  public void getFileByRelativePath_with_various_deterministic_source_path_when_match_returns_file() {
    InputFile expectedResult = mockInput("root/some/path/file.cs");
    FileSystem fs = createFileSystemForInputFiles(Collections.singletonList(expectedResult));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("\\_\\some\\path\\file.cs");

    assertThat(result).hasValue(expectedResult);
    assertThat(logTester.logs(LoggerLevel.TRACE)).hasSize(1);
    assertThat(logTester.logs(LoggerLevel.TRACE).get(0))
      .startsWith("Found indexed file ")
      .endsWith("root/some/path/file.cs' for '\\_\\some\\path\\file.cs' (normalized to 'some/path/file.cs').");

    result = sut.getFileByRelativePath("\\_1\\some\\path\\file.cs");
    assertThat(result).hasValue(expectedResult);

    result = sut.getFileByRelativePath("\\_1234\\some\\path\\file.cs");
    assertThat(result).hasValue(expectedResult);

    result = sut.getFileByRelativePath("/_/some/path/file.cs");
    assertThat(result).hasValue(expectedResult);

    result = sut.getFileByRelativePath("/_1/some/path/file.cs");
    assertThat(result).hasValue(expectedResult);

    result = sut.getFileByRelativePath("\\_9999\\some/path/file.cs");
    assertThat(result).hasValue(expectedResult);
  }

  @Test
  public void getFileByRelativePath_with_with_windows_style_paths_returns_empty() {
    InputFile expectedResult = mockInput("root/some/path/file.cs");
    FileSystem fs = createFileSystemForInputFiles(Collections.singletonList(expectedResult));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("C:\\_\\some\\path\\file.cs");

    assertThat(result).isEmpty();
    assertThat(logTester.logs(LoggerLevel.TRACE)).isEmpty();
    assertThat(logTester.logs(LoggerLevel.DEBUG)).hasSize(1);
    assertThat(logTester.logs(LoggerLevel.DEBUG).get(0)).isEqualTo("Did not find deterministic source path in 'C:\\_\\some\\path\\file.cs'." +
      " Will skip this coverage entry. Verify sonar.sources in .sonarqube\\out\\sonar-project.properties.");
  }

  @Test
  public void getFileByRelativePath_with_no_deterministic_source_path_when_single_indexed_files_match_returns_empty() {
    FileSystem fs = createFileSystemForInputFiles(Collections.singletonList(mock(InputFile.class)));

    ScannerFileService sut = new ScannerFileService("key", fs);
    Optional<InputFile> result = sut.getFileByRelativePath("some/path/file.cs");

    assertThat(result).isEmpty();
    assertThat(logTester.logs(LoggerLevel.TRACE)).isEmpty();
  }

  private FileSystem createFileSystemForInputFiles(Iterable<InputFile> inputFilesResult) {
    FileSystem fs = mock(FileSystem.class);
    when(fs.predicates()).thenReturn(mock(FilePredicates.class));
    when(fs.inputFiles(any())).thenReturn(inputFilesResult);
    return fs;
  }

  private FileSystem createFileSystemForHasFiles(boolean result) {
    FileSystem fs = mock(FileSystem.class);
    when(fs.hasFiles(any())).thenReturn(result);
    when(fs.predicates()).thenReturn(mock(FilePredicates.class));
    return fs;
  }

  private InputFile mockInput(String path) {
    return new TestInputFileBuilder("mod", path).setLanguage("cs").build();
  }

}
