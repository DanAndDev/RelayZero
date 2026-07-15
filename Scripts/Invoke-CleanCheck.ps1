[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'Shared.ps1')

$root = Get-RelayZeroRepoRoot
$failures = New-Object System.Collections.Generic.List[string]

function ConvertTo-RelayZeroRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return $Path.Substring($root.Length).TrimStart('\', '/')
}

Push-Location $root
try {
    foreach ($path in @('Assets', 'Packages', 'ProjectSettings', 'Assembly-CSharp.csproj', 'Assembly-CSharp-Editor.csproj', 'RelayZero.sln')) {
        if (Test-Path -LiteralPath (Join-Path $root $path)) {
            $failures.Add("Root Unity artifact exists outside /Unity: $path")
        }
    }

    foreach ($jsonPath in @('global.json', 'eng/toolchain.json', 'Unity/Packages/manifest.json', 'Unity/Packages/packages-lock.json')) {
        try {
            Get-Content -LiteralPath (Join-Path $root $jsonPath) -Raw | ConvertFrom-Json | Out-Null
        }
        catch {
            $failures.Add("Invalid JSON: $jsonPath ($($_.Exception.Message))")
        }
    }

    $secretPatterns = @(
        '*.pfx',
        '*.p12',
        '*.pem',
        '*.key',
        'id_rsa',
        '.env',
        '.env.*'
    )

    foreach ($pattern in $secretPatterns) {
        $matches = Get-ChildItem -Path $root -Recurse -Force -File -Filter $pattern -ErrorAction SilentlyContinue |
            Where-Object {
                $_.FullName -notmatch '\\Unity\\Library\\' -and
                $_.FullName -notmatch '\\Library\\' -and
                $_.Name -notlike '*.example'
            }

        foreach ($match in $matches) {
            $relative = ConvertTo-RelayZeroRelativePath -Path $match.FullName
            $failures.Add("Potential secret/local config file present: $relative")
        }
    }

    $largeFiles = Get-ChildItem -Path $root -Recurse -Force -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Length -gt 50MB -and
            $_.FullName -notmatch '\\Unity\\Library\\' -and
            $_.FullName -notmatch '\\Library\\' -and
            $_.FullName -notmatch '\\.git\\'
        }

    foreach ($file in $largeFiles) {
        $relative = ConvertTo-RelayZeroRelativePath -Path $file.FullName
        $failures.Add("Large file should be reviewed before commit: $relative ($([math]::Round($file.Length / 1MB, 1)) MB)")
    }

    $gitSafeRoot = $root.Replace('\', '/')
    $gitFiles = & git -c safe.directory=$gitSafeRoot -c filter.lfs.process= -c filter.lfs.required=false -c filter.lfs.clean= ls-files
    if ($LASTEXITCODE -ne 0) {
        $failures.Add('git ls-files failed')
    }
    else {
        foreach ($tracked in $gitFiles) {
            if ($tracked -match '^(Library|Temp|Logs|UserSettings|ProfilerCaptures|Unity/Library|Unity/Temp|Unity/Logs|Unity/UserSettings)/') {
                $failures.Add("Generated Unity path is tracked: $tracked")
            }
        }
    }

    if ($failures.Count -gt 0) {
        foreach ($failure in $failures) {
            Write-Error $failure -ErrorAction Continue
        }

        throw "Clean check failed with $($failures.Count) issue(s)."
    }

    Write-Host 'Clean check passed.'
}
finally {
    Pop-Location
}
