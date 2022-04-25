<#

.SYNOPSIS
This script allows to download metadata for the given language from RSPEC repository.

.DESCRIPTION
This script allows to download metadata for the given language from RSPEC repository:
https://jira.sonarsource.com/browse/RSPEC
If you want to add a rule to both c# and vb.net execute the operation for each language.

Usage: rspec.ps1
    [cs|vbnet]        The language to synchronize
    <rulekey>         The specific rule to synchronize
    <className>       The name to use for the automatically generated classes

NOTES:
- All operations recreate the projects' resources, do not edit manually.

#>
param (
    [Parameter(Mandatory = $true, HelpMessage = "language: cs or vbnet", Position = 0)]
    [ValidateSet("cs", "vbnet")]
    [string]
    $language,
    [Parameter(HelpMessage = "The key of the rule to add/update, e.g. S1234. If omitted will update all existing rules.", Position = 1)]
    [ValidatePattern("^S[0-9]+$")]
    [string]
    $ruleKey,
    [Parameter(HelpMessage = "The name of the rule class.", Position = 2)]
    [string]
    $className,
    [Parameter(HelpMessage = "The branch in the rspec github repo.", Position = 3)]
    [string]
    $rspecBranch
)

Set-StrictMode -version 1.0
$ErrorActionPreference = "Stop"
$RuleTemplateFolder = "${PSScriptRoot}\\rspec-templates"

$rule_api_error = "Could not find the Rule API Jar locally. Please download the latest rule-api from " + `
    "'https://repox.jfrog.io/repox/sonarsource-private-releases/com/sonarsource/rule-api/rule-api/' " +`
    "to a folder and set the %rule_api_path% environment variable with the full path of that folder. For example 'c:\\work\\tools'."
if (-Not $Env:rule_api_path) {
    throw $rule_api_error
}
$rule_api_jars = Get-ChildItem "${Env:rule_api_path}\\rule-api-*.jar" | Sort -desc
if ($rule_api_jars.Length -eq 0) {
    throw $rule_api_error
}
$rule_api_jar = $rule_api_jars[0].FullName
if (-Not (Test-Path $rule_api_jar)) {
    throw $rule_api_error
}

$sonaranalyzerPath = "${PSScriptRoot}\\..\\..\\analyzers"

$projectsMap = @{
    "cs" = "SonarAnalyzer.CSharp";
    "vbnet" = "SonarAnalyzer.VisualBasic";
}

$ruleapiLanguageMap = @{
    "cs" = "c#";
    "vbnet" = "vb.net";
}

$resourceLanguageMap = @{
    "cs" = "cs";
    "vbnet" = "vb";
}

$sonarpediaMap = @{
    "cs" = ".\analyzers\src\SonarAnalyzer.CSharp";
    "vbnet" = ".\analyzers\src\SonarAnalyzer.VisualBasic";
}

# Returns the path to the folder where the RSPEC html and json files for the specified language will be downloaded.
function GetRspecDownloadPath($lang) {
    $rspecFolder = "${sonaranalyzerPath}\\rspec\\${lang}"
    if (-Not (Test-Path $rspecFolder)) {
        New-Item $rspecFolder | Out-Null
    }

    return $rspecFolder
}

# Returns a string array with rule keys for the specified language.
function GetRules($lang) {
    $suffix = $ruleapiLanguageMap.Get_Item($lang)

    $htmlFiles = Get-ChildItem "$(GetRspecDownloadPath $lang)\\*" -Include "*.html"
    foreach ($htmlFile in $htmlFiles) {
        if ($htmlFile -Match "(S\d+)_(${suffix}).html") {
            $Matches[1]
        }
    }
}

function UpdateTestEntry($rule) {
    $ruleType = $rule.type
    if (!$ruleType) {
        return
    }

    $fileToEdit = if ($language -eq "cs") {"CsRuleTypeMapping"} else {"VbRuleTypeMapping"}
    $ruleTypeTestCase = "${sonaranalyzerPath}\\tests\\SonarAnalyzer.UnitTest\\PackagingTests\\$fileToEdit.cs"
    $ruleId = $ruleKey.Substring(1)
    (Get-Content "${ruleTypeTestCase}") -replace "//\[`"$ruleId`"\]", "[`"$ruleId`"] = `"$ruleType`"" |
        Set-Content "${ruleTypeTestCase}" -Encoding UTF8
}

function GetRulesInfo($lang) {
    $rules = GetRules $lang
    $rspecFolder = GetRspecDownloadPath $lang
    $suffix = $ruleapiLanguageMap.Get_Item($lang)

    $newRuleData = @{}
    $resources = New-Object System.Collections.ArrayList
    foreach ($rule in $rules) {
        $json = Get-Content -Raw "${rspecFolder}\\${rule}_${suffix}.json" | ConvertFrom-Json
        if ($rule -eq $ruleKey) {
            $newRuleData = $json
        }
    }

    return $newRuleData
}

function GenerateCsRuleClasses() {
    $csharpRulesFolder         = "${sonaranalyzerPath}\\src\\SonarAnalyzer.CSharp\\Rules"
    $csharpRuleTestsFolder     = "${sonaranalyzerPath}\\tests\\SonarAnalyzer.UnitTest\\Rules"
    $csharpRuleTestCasesFolder = "${sonaranalyzerPath}\\tests\\SonarAnalyzer.UnitTest\\TestCases"

    $filesMap = @{
        "CSharpRuleTemplate.cs"     = "${csharpRulesFolder}\\${className}.cs"
        "CSharpTestTemplate.cs"     = "${csharpRuleTestsFolder}\\${className}Test.cs"
        "CSharpTestCaseTemplate.cs" = "${csharpRuleTestCasesFolder}\\${className}.cs"
    }
    WriteClasses -filesMap $filesMap -classNameToUse $className
}

function GenerateVbRuleClasses($language) {
    $vbRulesFolder         = "${sonaranalyzerPath}\\src\\SonarAnalyzer.VisualBasic\\Rules"
    $csRulesFolder         = "${sonaranalyzerPath}\\src\\SonarAnalyzer.CSharp\\Rules"
    $ruleTestsFolder       = "${sonaranalyzerPath}\\tests\\SonarAnalyzer.UnitTest\\Rules"
    $vbRuleTestCasesFolder = "${sonaranalyzerPath}\\tests\\SonarAnalyzer.UnitTest\\TestCases"

    $csClassName = FindCsName -rulesPath $csRulesFolder

    $filesMap = @{}
    if ($csClassName) {
        $className = $csClassName
        AppendVbTestCase -ruleTestsFolder $ruleTestsFolder
    }
    else {
        $filesMap["VbNetTestTemplate.cs"] = "${ruleTestsFolder}\\${className}Test.cs"
    }

    $filesMap["VbNetRuleTemplate.cs"] = "${vbRulesFolder}\\${className}.cs"
    $filesMap["VbNetTestCaseTemplate.vb"] = "${vbRuleTestCasesFolder}\\${className}.vb"

    WriteClasses -filesMap $filesMap
}

function GenerateBaseClassIfSecondLanguage()
{
    $vbRulesFolder         = "${sonaranalyzerPath}\\src\\SonarAnalyzer.VisualBasic\\Rules"
    $csRulesFolder         = "${sonaranalyzerPath}\\src\\SonarAnalyzer.CSharp\\Rules"
    $commonRulesFolder     = "${sonaranalyzerPath}\\src\\SonarAnalyzer.Common\\Rules"

    $csClassName = FindCsName -rulesPath $csRulesFolder
    $vbClassName = FindCsName -rulesPath $vbRulesFolder

    $csFilePath = "${csRulesFolder}\\${csClassName}.cs"
    $vbFilePath = "${vbRulesFolder}\\${vbClassName}.cs"
    $filesMap = @{}

    if ($csClassName -And $vbClassName) {
        $existingCSClassText = Get-Content -Path "${csRulesFolder}\\${csClassName}.cs" -Raw
        $existingVBClassText = Get-Content -Path "${vbRulesFolder}\\${vbClassName}.cs" -Raw

        $oldCsClass ="    public sealed class ${csClassName} : SonarDiagnosticAnalyzer"
        $newCsClass ="    public sealed class ${csClassName} : ${className}Base<SyntaxKind>"
        $oldVbClass ="    public sealed class ${vbClassName} : SonarDiagnosticAnalyzer"
        $newVbClass ="    public sealed class ${vbClassName} : ${className}Base<SyntaxKind>"
        $existingCSClassText = ReplaceTextInString -oldText $oldCsClass -newText $newCsClass -modifiableString $existingCSClassText
        $existingVBClassText = ReplaceTextInString -oldText $oldVbClass -newText $newVbClass -modifiableString $existingVBClassText

        $diagnosticIdToken = "        private const string DiagnosticId = ""${ruleKey}"";"
        $existingCSClassText = RemoveText -textToRemove $diagnosticIdToken -modifiableString $existingCSClassText
        $existingVBClassText = RemoveText -textToRemove $diagnosticIdToken -modifiableString $existingVBClassText

        $messageFormatToken = "       private const string MessageFormat = """";"
        $existingCSClassText = RemoveText -textToRemove $messageFormatToken -modifiableString $existingCSClassText
        $existingVBClassText = RemoveText -textToRemove $messageFormatToken -modifiableString $existingVBClassText

        $ruleToken = "        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);"
        $existingCSClassText = RemoveText -textToRemove $ruleToken -modifiableString $existingCSClassText
        $existingVBClassText = RemoveText -textToRemove $ruleToken -modifiableString $existingVBClassText

        $supportedDiagToken = "        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);"
        $csLanguageFacadeToken = "        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;"
        $vbLanguageFacadeToken = "        protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;"

        $existingCSClassText = ReplaceTextInString -oldText $supportedDiagToken -newText $csLanguageFacadeToken -modifiableString $existingCSClassText
        $existingVBClassText = ReplaceTextInString -oldText $supportedDiagToken -newText $vbLanguageFacadeToken -modifiableString $existingVBClassText

        $filesMap["CommonBaseClassTemplate.cs"] = "${commonRulesFolder}\\${className}Base.cs"

        Set-Content -NoNewline `
                    -Path $csFilePath `
                    -Value $existingCSClassText `
                    -Encoding UTF8

        Set-Content -NoNewline `
                    -Path $vbFilePath `
                    -Value $existingVBClassText `
                    -Encoding UTF8

        WriteClasses -filesMap $filesMap
    }
}

function ReplaceTextInString($oldText, $newText, $modifiableString)
{
    $idx = $modifiableString.LastIndexOf($oldText)
    if ($idx -gt -1) {
        $modifiableString = $modifiableString.Remove($idx, $oldText.Length).Insert($idx, "$newText")
    }

    return $modifiableString
}

function RemoveText($textToRemove, $modifiableString)
{
    $idx = $modifiableString.LastIndexOf($textToRemove)
    if ($idx -gt -1) {
        $modifiableString = $modifiableString.Remove($idx, $textToRemove.Length + 2)
    }

    return $modifiableString
}

function AppendVbTestCase($ruleTestsFolder) {
    $existingClassText = Get-Content -Path "${ruleTestsFolder}\\${csClassName}Test.cs" -Raw
    $snippetText = Get-Content -Path "${RuleTemplateFolder}\\VbNetTestSnippet.cs" -Raw

    $usingToken = "using SonarAnalyzer.Rules.CSharp;"
    $csUsingToken = "using CS = SonarAnalyzer.Rules.CSharp;"
    $vbNetUsingToken = "using VB = SonarAnalyzer.Rules.VisualBasic;"
    $namespaceToken = "namespace SonarAnalyzer.UnitTest.Rules"
    $token = "    }"
    $usingTokenIdx = $existingClassText.IndexOf($usingToken)

    $newText = $existingClassText
    if ($usingTokenIdx -gt -1) {
        $newText = $existingClassText.Remove($usingTokenIdx, $usingToken.Length + 1)
    }

    $idx = $newText.LastIndexOf($token)

    if ($idx -gt -1) {
        $newText = $newText.Remove($idx, $token.Length).Insert($idx, "`r`n${snippetText}`r`n${token}")
    }
    else {
        $newText = "${$newText}`r`n${snippetText}"
    }

    $namespaceTokenIdx = $newText.IndexOf($namespaceToken);

    if ($namespaceTokenIdx -gt -1) {
        $newText = $newText.Insert($namespaceTokenIdx - 1, "${csUsingToken}`r`n${vbNetUsingToken}`r`n")
    }

    $newText = $newText.Replace("new ${csClassName}", "new CS.${csClassName}")

    $replaced = ReplaceTokens -text $newText

    Set-Content -NoNewline `
                -Path "${ruleTestsFolder}\\${csClassName}Test.cs" `
                -Value $replaced `
                -Encoding UTF8
}

function FindCsName($rulesPath) {
    $csRuleFiles = Get-ChildItem -Path $rulesPath -Filter "*.cs"

    $quotedRuleKey = "`"$ruleKey`";"
    foreach ($csRuleFile in $csRuleFiles) {
        $content = $null = Get-Content -Path $csRuleFile.FullName -Raw
        if ($content -match $quotedRuleKey) {
            return $csRuleFile.BaseName
        }
    }
    return $null
}

function WriteClasses($filesMap) {
    $filesMap.GetEnumerator() | ForEach-Object {
        $fileContent = Get-Content "${RuleTemplateFolder}\\$($_.Key)" -Raw
        $replaced = ReplaceTokens -text $fileContent

        Set-Content -NoNewline -Path $_.Value -Value $replaced -Encoding UTF8
    }
}

function ReplaceTokens($text) {
    return $text `
        -replace '\$DiagnosticClassName\$', $className `
        -replace '\$DiagnosticId\$', $ruleKey
}


### SCRIPT START ###

$sonarpediaFolder = $sonarpediaMap.Get_Item($language)
Write-Host "Will change directory to $sonarpediaFolder to run rule-api (from $rule_api_jar )"
pushd $sonarpediaFolder

if ($ruleKey) {
    if ($rspecBranch) {
        java "-Dline.separator=`n" -jar $rule_api_jar generate -rule $ruleKey -branch $rspecBranch
    }
    else {
        java "-Dline.separator=`n" -jar $rule_api_jar generate -rule $ruleKey
    }
}
else {
    java "-Dline.separator=`n" -jar $rule_api_jar update
}

Write-Host "Ran rule-api, will move back to root"
popd

$csRuleData = GetRulesInfo "cs"
$vbRuleData = GetRulesInfo "vbnet"

if ($className -And $ruleKey) {
    if ($language -eq "cs") {
       GenerateCsRuleClasses
       UpdateTestEntry $csRuleData
       GenerateBaseClassIfSecondLanguage
    }
    elseif ($language -eq "vbnet") {
       GenerateVbRuleClasses
       UpdateTestEntry $vbRuleData
       GenerateBaseClassIfSecondLanguage
    }

    $vsTempFolder = "${sonaranalyzerPath}\\.vs"
    if (Test-Path $vsTempFolder) {
        Remove-Item -Recurse -Force $vsTempFolder
    }
}

# Generate RspecStrings.resx using the new metadata
Invoke-Expression -Command "& { scripts\rspec\rspec2resx.ps1 $language .\analyzers\rspec\$language $sonarpediaFolder\RspecStrings.resx }"
