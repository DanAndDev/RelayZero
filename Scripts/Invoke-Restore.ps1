[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'Shared.ps1')

$root = Get-RelayZeroRepoRoot
Push-Location $root
try {
    Invoke-RelayZeroCommand -FilePath 'dotnet' -Arguments @('--info')
    Invoke-RelayZeroCommand -FilePath 'dotnet' -Arguments @('restore', 'Backend/RelayZero.Backend.sln')
}
finally {
    Pop-Location
}
