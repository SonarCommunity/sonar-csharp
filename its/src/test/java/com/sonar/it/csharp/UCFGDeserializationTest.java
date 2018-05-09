/*
 * SonarSource :: C# :: ITs :: Plugin
 * Copyright (C) 2011-2018 SonarSource SA
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
import com.sonar.orchestrator.OrchestratorBuilder;
import com.sonar.orchestrator.build.BuildResult;
import com.sonar.orchestrator.build.ScannerForMSBuild;
import com.sonar.orchestrator.locator.FileLocation;
import com.sonar.orchestrator.locator.MavenLocation;
import com.sonar.orchestrator.util.Command;
import com.sonar.orchestrator.util.CommandExecutor;
import com.sonar.orchestrator.util.StreamConsumer;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import org.junit.BeforeClass;
import org.junit.Test;
import org.sonar.ucfg.UCFG;
import org.sonar.ucfg.UCFGtoProtobuf;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.fail;

public class UCFGDeserializationTest {
  private static final String DOT_NET_CORE_SIMPLCOMMERCE = "dotNetCore-SimplCommerce";
  private static final String SECURITY_PROFILE = "securityProfile";

  private static Orchestrator orchestrator;

  @BeforeClass
  public static void initializeOrchestrator() throws Exception {
    // Versions of SonarQube and plugins support aliases:
    // - "DEV" for the latest build of master that passed QA
    // - "DEV[1.0]" for the latest build that passed QA of series 1.0.x
    // - "LATEST_RELEASE" for the latest release
    // - "LATEST_RELEASE[1.0]" for latest release of series 1.0.x
    // The SonarQube alias "LTS" has been dropped. An alternative is "LATEST_RELEASE[6.7]".
    // The term "latest" refers to the highest version number, not the most recently published version.
    OrchestratorBuilder builder = Orchestrator.builderEnv()
      .setSonarVersion(Optional.ofNullable(System.getProperty("sonar.runtimeVersion")).orElse("LATEST_RELEASE[6.7]"))
      .addPlugin(MavenLocation.of("org.sonarsource.dotnet", "sonar-csharp-plugin", "7.2.0.5239"));

    orchestrator = builder.build();
    orchestrator.start();
    createQP();
  }

  private static void createQP() throws IOException {
    String cSharpProfile = profile("cs", "csharpsquid", "S3649");
    loadProfile(cSharpProfile);
  }

  private static void loadProfile(String javaProfile) throws IOException {
    File file = File.createTempFile("profile", ".xml");
    Files.write(file.toPath(), javaProfile.getBytes());
    orchestrator.getServer().restoreProfile(FileLocation.of(file));
    file.delete();
  }


  private static String profile(String language, String repositoryKey, String ... ruleKeys) {
    StringBuilder sb = new StringBuilder()
      .append("<profile>")
      .append("<name>").append(SECURITY_PROFILE).append("</name>")
      .append("<language>").append(language).append("</language>")
      .append("<rules>");
    Arrays.stream(ruleKeys).forEach(ruleKey -> {
      sb.append("<rule>")
        .append("<repositoryKey>").append(repositoryKey).append("</repositoryKey>")
        .append("<key>").append(ruleKey).append("</key>")
        .append("<priority>INFO</priority>")
        .append("</rule>");
    });
    return sb
      .append("</rules>")
      .append("</profile>")
      .toString();
  }


  @Test
  public void read_ucfgs() {
    File projectLocation = new File("projects/SimplCommerce");

    orchestrator.executeBuild(getScannerForMSBuild(projectLocation)
      .addArgument("begin")
      .setDebugLogs(true)
      .setProjectKey(DOT_NET_CORE_SIMPLCOMMERCE)
      .setProjectName(DOT_NET_CORE_SIMPLCOMMERCE)
      .setProjectVersion("1.0"));

    executeDotNetCore(projectLocation, "build");

    orchestrator.executeBuild(getScannerForMSBuild(projectLocation).addArgument("end"));

    List<UCFG> ucfgs = readUcfgs(projectLocation);
    assertThat(ucfgs).hasSize(15);
  }

  private static List<UCFG> readUcfgs(File projectLocation) {
    File csharpDir = new File(projectLocation, ".sonarqube/out/ucfg_cs");
    List<UCFG> result = new ArrayList<>();
    if (csharpDir.isDirectory()) {
      try {
        for (File file : csharpDir.listFiles()) {
          result.add(UCFGtoProtobuf.fromProtobufFile(file));
        }
      } catch (Exception | Error ioe) {
        fail("An error occured while deserializing ucfgs : ", ioe);
      }
    } else {
      fail("Did not find ucfgs directory at " + csharpDir.getAbsolutePath());
    }
    return result;
  }

  private static ScannerForMSBuild getScannerForMSBuild(File projectDir) {
    return ScannerForMSBuild.create()
      .setScannerVersion("4.2.0.1214")
      .setUseDotNetCore(true)
      .setProjectDir(projectDir);
  }

  private void executeDotNetCore(File projectLocation, String... arguments) {
    BuildResult result = new BuildResult();
    StreamConsumer.Pipe writer = new StreamConsumer.Pipe(result.getLogsWriter());
    int status = CommandExecutor.create().execute(Command.create("dotnet")
      .addArguments(arguments)
      .setDirectory(projectLocation), writer, 10 * 60 * 1000);
    result.addStatus(status);

    assertThat(result.isSuccess()).isTrue();
  }
}
