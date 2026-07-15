[CmdletBinding()]
param(
    [switch] $SkipUnity
)

. (Join-Path $PSScriptRoot 'Shared.ps1')

& (Join-Path $PSScriptRoot 'Invoke-Restore.ps1')
& (Join-Path $PSScriptRoot 'Invoke-Compile.ps1') -Configuration Release -SkipUnity:$SkipUnity
& (Join-Path $PSScriptRoot 'Invoke-Test.ps1') -Configuration Release -SkipUnity:$SkipUnity
& (Join-Path $PSScriptRoot 'Invoke-CleanCheck.ps1')
