#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/../.."
Scripts/ci/restore.sh
Scripts/ci/compile.sh
Scripts/ci/test.sh
Scripts/ci/clean-check.sh
