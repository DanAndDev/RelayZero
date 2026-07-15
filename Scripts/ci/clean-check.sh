#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/../.."
pwsh -NoProfile -File Scripts/Invoke-CleanCheck.ps1
