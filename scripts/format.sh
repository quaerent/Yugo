#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore >/dev/null 2>&1 || true

cd "$(dirname "$0")/.."
dotnet csharpier format .
