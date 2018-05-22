/*
 * SonarC#
 * Copyright (C) 2014-2018 SonarSource SA
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
package org.sonar.plugins.csharp;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.HashSet;
import java.util.Set;
import org.sonar.api.server.profile.BuiltInQualityProfilesDefinition;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonarsource.analyzer.commons.BuiltInQualityProfileJsonLoader;

public class CSharpSonarWayProfile implements BuiltInQualityProfilesDefinition {
  private static final Logger LOG = Loggers.get(CSharpSonarWayProfile.class);

  @Override
  public void define(Context context) {
    NewBuiltInQualityProfile sonarWay = context.createBuiltInQualityProfile("Sonar way", CSharpPlugin.LANGUAGE_KEY);
    BuiltInQualityProfileJsonLoader.load(sonarWay, CSharpPlugin.REPOSITORY_KEY, "org/sonar/plugins/csharp/Sonar_way_profile.json");
    getSecurityRuleKeys().forEach(key -> sonarWay.activateRule(CSharpPlugin.REPOSITORY_KEY, key));
    sonarWay.done();
  }

  private static Set<String> getSecurityRuleKeys() {
    try {
      Class<?> csRulesClass = Class.forName("com.sonar.plugins.security.api.CsRules");
      Method getRuleKeysMethod = csRulesClass.getMethod("getRuleKeys");
      return (Set<String>) getRuleKeysMethod.invoke(null);
    } catch (ClassNotFoundException e) {
      LOG.debug("com.sonar.plugins.security.api.CsRules is not found, no security rules added to Sonar way cs profile: " + e.getMessage());
    } catch (NoSuchMethodException e) {
      LOG.debug("com.sonar.plugins.security.api.CsRules#getRuleKeys is not found, no security rules added to Sonar way cs profile: " + e.getMessage());
    } catch (IllegalAccessException e) {
      LOG.debug("[IllegalAccessException] no security rules added to Sonar way cs profile: " + e.getMessage());
    } catch (InvocationTargetException e) {
      LOG.debug("[InvocationTargetException] no security rules added to Sonar way cs profile: " + e.getMessage());
    }

    return new HashSet<>();
  }
}
