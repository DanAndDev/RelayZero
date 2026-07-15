#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/../.."
dotnet --info
dotnet restore Backend/RelayZero.Backend.sln
