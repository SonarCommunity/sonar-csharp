/*
 * SonarSource :: .NET :: Shared library
 * Copyright (C) 2014-2017 SonarSource SA
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
package org.sonarsource.dotnet.shared.plugins;

import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Optional;
import org.junit.Test;
import org.sonar.api.batch.sensor.SensorContext;

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoMoreInteractions;
import static org.mockito.Mockito.verifyZeroInteractions;
import static org.mockito.Mockito.when;

public class AbstractPropertiesSensorTest {
  private AbstractConfiguration config = mock(AbstractConfiguration.class);
  private ReportPathCollector reportPathCollector = mock(ReportPathCollector.class);

  AbstractPropertiesSensor underTest = new AbstractPropertiesSensor(config, reportPathCollector, "sensor", "languageKey") {
  };

  @Test
  public void should_collect_properties_from_multiple_modules() {
    Path roslyn1 = Paths.get("roslyn1");
    Path roslyn2 = Paths.get("roslyn2");
    Path proto1 = Paths.get("proto1");
    Path proto2 = Paths.get("proto2");

    when(config.roslynReportPath()).thenReturn(Optional.of(roslyn1));
    when(config.protobufReportPath()).thenReturn(Optional.of(proto1));
    underTest.execute(mock(SensorContext.class));
    verify(reportPathCollector).addProtobufDir(proto1);
    verify(reportPathCollector).addRoslynDir(roslyn1);

    when(config.roslynReportPath()).thenReturn(Optional.of(roslyn2));
    when(config.protobufReportPath()).thenReturn(Optional.of(proto2));
    underTest.execute(mock(SensorContext.class));
    verify(reportPathCollector).addProtobufDir(proto2);
    verify(reportPathCollector).addRoslynDir(roslyn2);

    verifyNoMoreInteractions(reportPathCollector);
  }

  @Test
  public void should_continue_if_report_path_not_present() {
    when(config.roslynReportPath()).thenReturn(Optional.empty());
    when(config.protobufReportPath()).thenReturn(Optional.empty());
    underTest.execute(mock(SensorContext.class));
    verifyZeroInteractions(reportPathCollector);
  }
}
