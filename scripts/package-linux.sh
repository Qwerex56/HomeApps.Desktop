#!/usr/bin/env bash
# S-2f: builds build/linux/jarvis-linux-x86_64.tar.gz: the app with the bridge next to it (docs/app.md "Package").
# Needs .NET 10 and Godot 4.7 with its export templates; GODOT points at another Godot binary.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
godot="${GODOT:-godot}"
out="$root/build/linux"
app="$out/jarvis"
rm -rf "$out"
mkdir -p "$app"

echo "1/3 Most (.NET, linux-x64, self-contained)"
dotnet publish "$root/src/Jarvis.VoiceBridge" -c Release -r linux-x64 --self-contained \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "$app/bridge"
rm -f "$app/bridge/"*.dll "$app/bridge/"*.pdb

echo "2/3 Aplikacja (Godot, preset Linux)"
"$godot" --headless --path "$root/app" --export-release "Linux" "$app/jarvis.x86_64"

echo "3/3 Archiwum"
tar -C "$out" -czf "$out/jarvis-linux-x86_64.tar.gz" jarvis

echo "Gotowe: $out/jarvis-linux-x86_64.tar.gz (rozpakuj np. do ~/.local/share/jarvis)"
