#!/usr/bin/env bash
set -euo pipefail

# Configuration
PROJECT="src/Yugo.Game/Yugo.Game.csproj"
OUTPUT="publish/windows"
RID="win-x64"

echo "Publishing Yugo for Windows ($RID)..."

# Clean old publish
rm -rf "$OUTPUT"

# Publish
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=true \
    -o "$OUTPUT"

echo "Done! Windows version available in $OUTPUT"