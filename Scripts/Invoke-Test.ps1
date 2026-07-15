[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipUnity
)

. (Join-Path $PSScriptRoot 'Shared.ps1')

$root = Get-RelayZeroRepoRoot
Push-Location $root
try {
    Invoke-RelayZeroCommand -FilePath 'dotnet' -Arguments @('test', 'Backend/RelayZero.Backend.sln', '--configuration', $Configuration, '--no-build')

    if (-not $SkipUnity) {
        $testResultsDir = Join-Path $root 'TestResults'
        $logDir = Join-Path $root 'Logs'
        New-RelayZeroDirectory -Path $testResultsDir
        New-RelayZeroDirectory -Path $logDir

        $unityExe = Get-RelayZeroUnityExecutable
        $unityProject = Get-RelayZeroUnityProjectPath
        $logFile = Join-Path $logDir 'RZ-01.2-unity-tests.log'
        $resultsFile = Join-Path $testResultsDir 'UnityEditModeResults.xml'
        Invoke-RelayZeroCommand -FilePath $unityExe -Arguments @('-batchmode', '-projectPath', $unityProject, '-runTests', '-testPlatform', 'editmode', '-testResults', $resultsFile, '-logFile', $logFile)
    }
}
finally {
    Pop-Location
}
