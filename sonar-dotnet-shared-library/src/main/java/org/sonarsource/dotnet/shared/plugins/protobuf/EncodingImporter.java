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
package org.sonarsource.dotnet.shared.plugins.protobuf;

import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonarsource.dotnet.protobuf.SonarAnalyzer;
import org.sonarsource.dotnet.protobuf.SonarAnalyzer.EncodingInfo;

import java.nio.charset.Charset;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;

public class EncodingImporter extends RawProtobufImporter<EncodingInfo> {

  private static final Logger LOG = Loggers.get(EncodingImporter.class);

  private final Map<String, Charset> encodingPerPath = new HashMap<>();

  EncodingImporter() {
    super(SonarAnalyzer.EncodingInfo.parser());
  }

  @Override
  void consume(EncodingInfo message) {
    String roslynEncoding = message.getEncoding();
    Charset charset = null;
    if (!roslynEncoding.isEmpty()) {
      try {
        charset = Charset.forName(roslynEncoding);
      } catch (Exception e) {
        LOG.warn("Unrecognized encoding {} for file {}", roslynEncoding, message.getFilePath(), e);
      }
    }
    encodingPerPath.put(message.getFilePath(), charset);
  }

  public Map<Path, Charset> getEncodingPerPath() {
    return encodingPerPath.entrySet()
        .stream()
        .collect(Collectors.toMap(entry -> Paths.get(entry.getKey()),
                                  Map.Entry::getValue));
  }

}
