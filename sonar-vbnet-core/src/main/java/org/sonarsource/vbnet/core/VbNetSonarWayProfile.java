/*
 * SonarSource :: VB.NET :: Core
 * Copyright (C) 2012-2024 SonarSource SA
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
package org.sonarsource.vbnet.core;

import org.sonarsource.dotnet.shared.plugins.AbstractSonarWayProfile;
import org.sonarsource.dotnet.shared.plugins.PluginMetadata;
import org.sonarsource.dotnet.shared.plugins.RoslynRules;

public class VbNetSonarWayProfile extends AbstractSonarWayProfile {
  public  VbNetSonarWayProfile(PluginMetadata metadata, RoslynRules roslynRules) {
    super(metadata, roslynRules);
  }
}
