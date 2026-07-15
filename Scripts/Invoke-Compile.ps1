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
    Invoke-RelayZeroCommand -FilePath 'dotnet' -Arguments @('build', 'Backend/RelayZero.Backend.sln', '--configuration', $Configuration, '--no-restore')

    if (-not $SkipUnity) {
        $logDir = Join-Path $root 'Logs'
        New-RelayZeroDirectory -Path $logDir
        $unityExe = Get-RelayZeroUnityExecutable
        $unityProject = Get-RelayZeroUnityProjectPath
        $logFile = Join-Path $logDir 'RZ-01.2-unity-compile.log'
        Invoke-RelayZeroCommand -FilePath $unityExe -Arguments @('-batchmode', '-quit', '-projectPath', $unityProject, '-logFile', $logFile)
    }
}
finally {
    Pop-Location
}
