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
mkdir -p "$bundle/Contents/Resources"
icon_source="$work_dir/picturefetcher-1024.png"
iconset="$work_dir/PictureFetcher.iconset"
mkdir -p "$iconset"
python3 packaging/macos/generate_icon.py "$icon_source"
for size in 16 32 128 256 512; do
  sips -s format png -z "$size" "$size" "$icon_source" \
    --out "$iconset/icon_${size}x${size}.png" >/dev/null
  double_size="$((size * 2))"
  sips -s format png -z "$double_size" "$double_size" "$icon_source" \
    --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$bundle/Contents/Resources/PictureFetcher.icns"
chmod +x "$bundle/Contents/MacOS/PictureFetcher"
plutil -lint "$bundle/Contents/Info.plist"

# Sign with Developer ID when secrets are present. Otherwise use ad-hoc
# signing for local testing only (the result is not Gatekeeper-trusted).
if [[ -n "${MACOS_CERTIFICATE_BASE64:-}" ]]; then
  : "${MACOS_CERTIFICATE_PASSWORD:?Missing macOS certificate password}"
  : "${MACOS_SIGNING_IDENTITY:?Missing Developer ID Application signing identity}"
  keychain="$work_dir/picturefetcher-signing.keychain-db"
  keychain_password="$(uuidgen)"
  security create-keychain -p "$keychain_password" "$keychain"
  security set-keychain-settings -lut 21600 "$keychain"
  security unlock-keychain -p "$keychain_password" "$keychain"
  echo "$MACOS_CERTIFICATE_BASE64" | base64 -D > "$work_dir/developer-id.p12"
  security import "$work_dir/developer-id.p12" -k "$keychain" \
    -P "$MACOS_CERTIFICATE_PASSWORD" -T /usr/bin/codesign
  security set-key-partition-list -S apple-tool:,apple: -s \
    -k "$keychain_password" "$keychain"
  find "$bundle/Contents/MacOS" -type f \( -name '*.dylib' -o -name '*.so' \) -print0 |
    while IFS= read -r -d '' native; do
      codesign --force --timestamp --options runtime --sign "$MACOS_SIGNING_IDENTITY" \
        --keychain "$keychain" "$native"
    done
  codesign --force --deep --timestamp --options runtime \
    --entitlements packaging/macos/entitlements.plist \
    --sign "$MACOS_SIGNING_IDENTITY" --keychain "$keychain" "$bundle"
else
  codesign --force --deep --sign - "$bundle"
fi
codesign --verify --deep --strict --verbose=2 "$bundle"

# Native macOS installation experience: drag .app onto Applications alias.
mkdir -p "$work_dir/dmg-root"
mv "$bundle" "$work_dir/dmg-root/PictureFetcher.app"
ln -s /Applications "$work_dir/dmg-root/Applications"
hdiutil create -quiet -volname PictureFetcher -srcfolder "$work_dir/dmg-root" -format UDZO -ov \
  "release-assets/PictureFetcher-$label-$rid.dmg"
du -h "release-assets/PictureFetcher-$label-$rid.dmg"

# Notarization is only possible with a trusted Developer ID certificate and
# Apple developer account credentials. Never staple an ad-hoc signed image.
if [[ -n "${APPLE_NOTARY_ID:-}" ]]; then
  : "${MACOS_CERTIFICATE_BASE64:?Notarization requires Developer ID signing}"
  : "${APPLE_NOTARY_PASSWORD:?Missing Apple app-specific password}"
  : "${APPLE_NOTARY_TEAM_ID:?Missing Apple developer team ID}"
  xcrun notarytool submit "release-assets/PictureFetcher-$label-$rid.dmg" \
    --apple-id "$APPLE_NOTARY_ID" \
    --password "$APPLE_NOTARY_PASSWORD" \
    --team-id "$APPLE_NOTARY_TEAM_ID" --wait
  xcrun stapler staple "release-assets/PictureFetcher-$label-$rid.dmg"
  xcrun stapler validate "release-assets/PictureFetcher-$label-$rid.dmg"
fi

