#!/usr/bin/env bash
# Usage: bash packaging/linux/package.sh <version-label> <publish-dir>
set -euo pipefail
label="${1:?missing version label}"
publish_dir="${2:?missing publish directory}"
mkdir -p release-assets
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/PictureFetcher"
cp -R "$publish_dir/." "$stage/PictureFetcher/"
cp README.md README.zh-CN.md LICENSE "$stage/PictureFetcher/"
chmod +x "$stage/PictureFetcher/PictureFetcher"
tar -czf "release-assets/PictureFetcher-$label-linux-x64.tar.gz" -C "$stage" PictureFetcher
du -h "release-assets/PictureFetcher-$label-linux-x64.tar.gz"
