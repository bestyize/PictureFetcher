#!/usr/bin/env bash
# Usage: bash packaging/macos/package.sh <osx-arm64|osx-x64> <version-label> <publish-dir>
set -euo pipefail
rid="${1:?missing RID}"
label="${2:?missing release label}"
publish_dir="${3:?missing publish directory}"
case "$rid" in osx-arm64|osx-x64) ;; *) echo "Unsupported macOS RID: $rid" >&2; exit 2;; esac

mkdir -p release-assets
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT
bundle="$work_dir/PictureFetcher.app"
mkdir -p "$bundle/Contents/MacOS"
cp -R "$publish_dir/." "$bundle/Contents/MacOS/"
cp packaging/macos/Info.plist "$bundle/Contents/Info.plist"
chmod +x "$bundle/Contents/MacOS/PictureFetcher"
plutil -lint "$bundle/Contents/Info.plist"

# Ad-hoc signing is required for Apple Silicon to run locally, but does NOT
# establish trusted publisher identity or pass Gatekeeper notarization.
codesign --force --deep --sign - "$bundle"
codesign --verify --deep --strict --verbose=2 "$bundle"

# Native macOS installation experience: drag .app onto Applications alias.
mkdir -p "$work_dir/dmg-root"
mv "$bundle" "$work_dir/dmg-root/PictureFetcher.app"
ln -s /Applications "$work_dir/dmg-root/Applications"
hdiutil create -quiet -volname PictureFetcher -srcfolder "$work_dir/dmg-root" -format UDZO -ov \
  "release-assets/PictureFetcher-$label-$rid.dmg"
du -h "release-assets/PictureFetcher-$label-$rid.dmg"
