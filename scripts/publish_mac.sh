#!/usr/bin/env bash
set -euo pipefail

# Configuration
PROJECT="Yugo.Game/Yugo.Game.csproj"
APP_NAME="Yugo"
RID="osx-arm64" 
PUBLISH_DIR="publish/mac_raw"
APP_BUNDLE="publish/mac/$APP_NAME.app"

echo "Publishing Yugo for macOS ($RID)..."

# Clean old publish
rm -rf "publish/mac"
rm -rf "$PUBLISH_DIR"

# Publish binary - Not using SingleFile for macOS bundles as it can cause dylib loading issues
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishReadyToRun=true \
    -o "$PUBLISH_DIR"

# Create Bundle Structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Move all published files to MacOS folder
cp -r "$PUBLISH_DIR/"* "$APP_BUNDLE/Contents/MacOS/"

# Copy Icon if exists
if [ -f "Yugo.Game/Icon.icns" ]; then
    cp "Yugo.Game/Icon.icns" "$APP_BUNDLE/Contents/Resources/Icon.icns"
fi

# Create Info.plist
cat > "$APP_BUNDLE/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.microsoft.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>Icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.yugo.game</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>
EOF

# CRITICAL: Fix permissions
chmod -R +x "$APP_BUNDLE/Contents/MacOS/"

# Fix dylib paths if codesign is available
if command -v codesign &> /dev/null; then
    echo "Signing and fixing dylibs..."
    # Sign all dylibs first
    find "$APP_BUNDLE/Contents/MacOS/" -name "*.dylib" -exec codesign --force --sign - {} \;
    # Sign the main app
    codesign --force --deep --sign - "$APP_BUNDLE"
else
    echo "codesign not found, skipping signing"
fi

echo "Done! macOS App Bundle available in $APP_BUNDLE"