#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore >/dev/null 2>&1 || true

dotnet format --verify-no-changes
