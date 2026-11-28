#!/usr/bin/env bash
# Builds HomeApps.Desktop for Linux x64 (self-contained) into dist/ as a tar.gz.
# Usage: scripts/package-linux.sh <version>
set -euo pipefail

VERSION="${1:?usage: package-linux.sh <version>}"
VERSION="${VERSION#v}"
RID="linux-x64"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIR="$ROOT/dist/build-$RID/HomeApps.Desktop"

rm -rf "$ROOT/dist/build-$RID"
mkdir -p "$DIR"
dotnet publish "$ROOT/src/HomeApps.Desktop/HomeApps.Desktop.csproj" -c Release -r "$RID" --self-contained true \
  -p:Version="$VERSION" -o "$DIR"
chmod +x "$DIR/HomeApps.Desktop"

TAR="$ROOT/dist/HomeApps.Desktop-$VERSION-$RID.tar.gz"
tar -czf "$TAR" -C "$ROOT/dist/build-$RID" HomeApps.Desktop
echo "$TAR"
