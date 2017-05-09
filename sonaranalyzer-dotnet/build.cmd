@echo off
setlocal ENABLEDELAYEDEXPANSION

PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\build.ps1 -analyze -test -package -githubRepo $env:GITHUB_REPO -githubToken $env:GITHUB_TOKEN -githubPullRequest $env:PULL_REQUEST -isPullRequest $env:IS_PULLREQUEST -sonarQubeUrl $env:SONAR_HOST_URL -sonarQubeToken $env:SONAR_TOKEN -certificatePath $env:CERT_PATH"
echo From Cmd.exe: build.ps1 exited with exit code !errorlevel!
exit !errorlevel!