#!/usr/bin/env bash
# S-2f: builds build/macos/JARVIS.app for Apple silicon with the bridge inside (docs/app.md "Package").
# Needs .NET 10 and Godot 4.7 with its export templates; GODOT points at another Godot binary.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
godot="${GODOT:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
out="$root/build/macos"
app="$out/JARVIS.app"
rm -rf "$out"
mkdir -p "$out"

echo "1/4 Most (.NET, osx-arm64, self-contained)"
dotnet publish "$root/src/Jarvis.VoiceBridge" -c Release -r osx-arm64 --self-contained \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "$out/bridge"
rm -f "$out/bridge/"*.dll "$out/bridge/"*.pdb

echo "2/4 Aplikacja (Godot, preset macOS)"
"$godot" --headless --path "$root/app" --export-release "macOS" "$app"

echo "3/4 Most do środka, bez ikony w Docku"
mkdir -p "$app/Contents/Resources"
mv "$out/bridge" "$app/Contents/Resources/bridge"
plist="$app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Delete :LSUIElement" "$plist" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Add :LSUIElement bool true" "$plist"
/usr/libexec/PlistBuddy -c "Delete :NSMicrophoneUsageDescription" "$plist" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Add :NSMicrophoneUsageDescription string JARVIS słucha „Hey Jarvis” i Twoich poleceń." "$plist"

echo "4/4 Podpis ad-hoc (bez konta Apple Developer)"
for file in "$app/Contents/Resources/bridge/"*; do
    codesign --force --sign - --timestamp=none "$file"
done
codesign --force --sign - --timestamp=none "$app"
codesign --verify --deep --strict "$app"

echo "Gotowe: $app (przenieś do /Applications)"
