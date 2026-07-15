[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RelayZeroRepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-RelayZeroUnityProjectPath {
    $root = Get-RelayZeroRepoRoot
    return Join-Path $root 'Unity'
}

function Get-RelayZeroUnityExecutable {
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE)) {
        if (Test-Path -LiteralPath $env:UNITY_EXE) {
            return (Resolve-Path $env:UNITY_EXE).Path
        }

        throw "UNITY_EXE points to a missing file: $env:UNITY_EXE"
    }

    $projectVersionPath = Join-Path (Get-RelayZeroUnityProjectPath) 'ProjectSettings/ProjectVersion.txt'
    $projectVersion = Get-Content -LiteralPath $projectVersionPath -Raw
    if ($projectVersion -notmatch 'm_EditorVersion:\s*(?<version>\S+)') {
        throw "Unable to read Unity editor version from $projectVersionPath"
    }

    $unityExe = Join-Path "C:/Program Files/Unity/Hub/Editor/$($Matches.version)" 'Editor/Unity.exe'
    if (Test-Path -LiteralPath $unityExe) {
        return $unityExe
    }

    throw "Unity editor $($Matches.version) was not found. Set UNITY_EXE to the Unity executable path."
}

function Invoke-RelayZeroCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,

        [Parameter()]
        [string[]] $Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $($LASTEXITCODE): $FilePath $($Arguments -join ' ')"
    }
}

function New-RelayZeroDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}
