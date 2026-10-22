#!/usr/bin/env bash
# Publish profiles: baseline (existing) or partial-trim (experimental)
set -euo pipefail
rid="$1"; output="$2"; profile="${3:-baseline}"
case "$rid" in win-x64|linux-x64|osx-arm64|osx-x64) ;; *) exit 2;; esac
case "$profile" in baseline|partial-trim) ;; *) exit 2;; esac
args=(
  -c Release -r "$rid" --self-contained true
  -p:PublishSingleFile=true
  -p:IncludeNativeLibrariesForSelfExtract=true
  -p:EnableCompressionInSingleFile=true
  -p:PublishReadyToRun=false
  -p:DebugType=None -p:DebugSymbols=false
  -p:PublishTrimmed=false
)
if [[ "$profile" == "partial-trim" ]]; then
  args+=(-p:PublishTrimmed=true -p:TrimMode=partial)
fi
dotnet publish PictureFetcher.csproj "${args[@]}" -o "$output"
