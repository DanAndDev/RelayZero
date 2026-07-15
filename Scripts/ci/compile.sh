#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/../.."
dotnet build Backend/RelayZero.Backend.sln --configuration Release --no-restore

if [ "${SKIP_UNITY:-0}" != "1" ]; then
  : "${UNITY_EXE:?Set UNITY_EXE to run Unity compile checks in CI.}"
  mkdir -p Logs
  "$UNITY_EXE" -batchmode -quit -projectPath Unity -logFile Logs/RZ-01.2-unity-compile.log
fi
