#!/usr/bin/env bash
# Builds HomeApps.Desktop.app (self-contained, ad hoc signed) and zips it into dist/.
# Usage: scripts/package-macos.sh <version> [osx-arm64|osx-x64]   (default rid: this Mac's arch)
set -euo pipefail

VERSION="${1:?usage: package-macos.sh <version> [rid]}"
VERSION="${VERSION#v}"
if [ -n "${2:-}" ]; then RID="$2"; elif [ "$(uname -m)" = "arm64" ]; then RID="osx-arm64"; else RID="osx-x64"; fi

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/dist/build-$RID"
APP="$OUT/HomeApps.Desktop.app"
NUMERIC="${VERSION%%[-+]*}"

rm -rf "$OUT"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

dotnet publish "$ROOT/src/HomeApps.Desktop/HomeApps.Desktop.csproj" -c Release -r "$RID" --self-contained true \
  -p:Version="$VERSION" -o "$APP/Contents/MacOS"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>HomeApps Desktop</string>
  <key>CFBundleDisplayName</key><string>HomeApps Desktop</string>
  <key>CFBundleIdentifier</key><string>pl.homeapps.desktop</string>
  <key>CFBundleExecutable</key><string>HomeApps.Desktop</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleVersion</key><string>$NUMERIC</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>LSUIElement</key><true/>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSMicrophoneUsageDescription</key><string>JARVIS listens for the wake word and your commands.</string>
</dict>
</plist>
PLIST

chmod +x "$APP/Contents/MacOS/HomeApps.Desktop"
codesign --force --deep --sign - "$APP"
codesign --verify --deep --strict "$APP"

mkdir -p "$ROOT/dist"
ZIP="$ROOT/dist/HomeApps.Desktop-$VERSION-$RID.zip"
rm -f "$ZIP"
ditto -c -k --keepParent "$APP" "$ZIP"
echo "$ZIP"
