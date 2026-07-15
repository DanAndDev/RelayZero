#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/../.."
dotnet test Backend/RelayZero.Backend.sln --configuration Release --no-build

if [ "${SKIP_UNITY:-0}" != "1" ]; then
  : "${UNITY_EXE:?Set UNITY_EXE to run Unity tests in CI.}"
  mkdir -p Logs TestResults
  "$UNITY_EXE" -batchmode -projectPath Unity -runTests -testPlatform editmode -testResults TestResults/UnityEditModeResults.xml -logFile Logs/RZ-01.2-unity-tests.log
fi
