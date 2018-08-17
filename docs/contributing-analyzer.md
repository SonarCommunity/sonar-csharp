# Building, Testing and Debugging the Analyzer

All C# and VB .NET analyzers present in SonarLint for Visual Studio, in the SonarQube SonarC# Plugin or in the SonarQube SonarVB plugin are being developed here. These analyzers rely on Roslyn 1.3.2 API.

## Working with the code

1. Clone [this repository](https://github.com/SonarSource/sonar-csharp.git)
1. Download sub-modules `git submodule update --init --recursive`
1. Run `.\scripts\build\dev-build.ps1 -restore -build -test -metadata`

## Developing with Visual Studio 2017

1. [Visual Studio 2017 Version 15.7](https://www.visualstudio.com/vs/preview/)
    - Ensure C#, VB, MSBuild, .NET Core and Visual Studio Extensibility are included in the selected work loads
    - Ensure Visual Studio is on Version "15.7" or greater
1. [.NET Core SDK 2.1.300](https://www.microsoft.com/net/download/core) (the current previews are: [Windows x64 installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.300-rtm-008866/dotnet-sdk-2.1.300-rtm-008866-win-x64.exe), [Windows x86 installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.300-rtm-008866/dotnet-sdk-2.1.300-rtm-008866-win-x86.exe))
1. Open `SonarAnalyzer.sln` in the `sonaranalyzer-dotnet` subfolder

## Running Tests

### Unit Tests

You can run the Unit Tests via the Test Explorer of Visual Studio or using `.\scripts\build\dev-build.ps1 test`

### Integration Tests

To run the ITs you will need to follow this pattern:

1. Open the `Developer Command Prompt for VS2017` from the start menu
1. Go to `PATH_TO_CLONED_REPOSITORY/sonaranalyzer-dotnet/its`
1. Run `powershell`
1. Run `.\regression-test.ps1`

Note: you can run a single rule using the -ruleId parameter (e.g. `.\regression-test.ps1 -ruleId S1234`)

If the script ends with `SUCCESS: No differences were found!` (or exit code 0), this means the changes you have made haven't impacted any rule.

If the script ends with `ERROR: There are differences between the actual and the expected issues.` (or exit code 1),
the changes you have made have impacted one or many issues raised by the rules.
You can visualize the differences using:

1. `cd actual`
1. `git diff --cached`

Please review all added/removed/updated issue to confirm they are wanted. When this is done run `.\update-expected.ps1` to update the list of expected issues.

### Manual Tests

From Visual Studio, make sure `SonarAnalyzer.Vsix.csproj` is selected as startup project. And then do the following:

1. Make sure SonarLint for Visual Studio is uninstalled
2. Hit `F5` to launch the experimental instance of Visual Studio
3. Open one of the following solutions from the experimental instance:
    - [Akka.NET](akka.net/src/Akka.sln)
    - [Nancy](Nancy/src/Nancy.sln)
    - [Ember-MM](Ember-MM/Ember%20Media%20Manager.sln)
4. Turn on your new rule in [Validation Ruleset](ValidationRuleset.ruleset), review the results, improve, and setup the regression test once you are satisfied.
    - Note: the solutions have been pre-configured to use this ruleset on all their projects.

## Contributing

Please see [Contributing Code](../CONTRIBUTING.md) for details on
contributing changes back to the code.
