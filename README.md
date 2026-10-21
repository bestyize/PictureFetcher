# PictureFetcher

**English** · [简体中文](README.zh-CN.md)

A compact cross-platform image-to-bitmap desktop tool for **128 × 64 monochrome OLED displays**. Powered by .NET 10 and Avalonia.

## Downloads

Get your platform's archive from [GitHub Releases](https://github.com/bestyize/PictureFetcher/releases).

| Platform | Package | Installation |
| --- | --- | --- |
| Windows (Intel / AMD 64-bit) | `PictureFetcher-vX.Y.Z-win-x64.zip` | Extract, then run `PictureFetcher.exe` |
| Linux x64 | `PictureFetcher-vX.Y.Z-linux-x64.tar.gz` | Extract and run `./PictureFetcher` |
| Mac with Apple Silicon | `PictureFetcher-vX.Y.Z-osx-arm64.dmg` | Open DMG, drag PictureFetcher into Applications |
| Mac with Intel | `PictureFetcher-vX.Y.Z-osx-x64.dmg` | Open DMG, drag PictureFetcher into Applications |

The binaries are self-contained, so you do **not** need to install the .NET 10 runtime. The Linux build requires standard system desktop/X11 or Wayland and font libraries (a graphical environment is necessary).

**macOS security note:** Current public builds use *ad-hoc* code signing, not Apple Developer ID signing or notarization. They are **not trusted by Gatekeeper** when downloaded from the internet. For frictionless installation, the release must be signed with an Apple Developer ID and notarized. See [Packaging notes](docs/PACKAGING.md). Do not misinterpret the ad-hoc signature as Apple verification.

## Features

- Native-feeling macOS-inspired desktop layout on Windows, Linux and macOS.
- English / 简体中文 UI, with remembered language preference.
- Open or drag an image into the editor; preview original and 128 × 64 output.
- Adjustable threshold and invert control.
- SSD1306 page-format output: hexadecimal byte stream or 1024-byte C array, with copy action.
- Save individual BMP images or batch-convert multiple images.

**Conversion compatibility:** The original app's threshold polarity is preserved: a pixel with R+G+B above the threshold is rendered black (unless Invert is enabled). This can differ from other image tools. The OLED byte encoding is vertical LSB-first, in eight pages of 128 bytes.

**Safe batch exports:** Converted files are saved beside their sources with the suffix `-oled.bmp`. If the name exists, numbered suffixes are used instead of overwriting any source or previous export.

## Build

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore PictureFetcher.sln
dotnet build PictureFetcher.sln -c Release
dotnet run --project PictureFetcher.csproj
dotnet run --project tests/PictureFetcher.Core.Tests/PictureFetcher.Core.Tests.csproj
```

To build a self-contained release for a selected architecture:

```bash
dotnet publish PictureFetcher.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=false \
  -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
```

Replace `linux-x64` with `win-x64`, `osx-arm64`, or `osx-x64` as needed. For distribution, use the matching GitHub-hosted runner and packaging script.

## Release automation

CI validates the portable conversion core and generates all four self-contained runtime targets; it also smoke-tests the platform-specific package formats. Push a version tag `vX.Y.Z` after merging to publish the four archives and `SHA256SUMS.txt` to GitHub Releases. You can invoke the release workflow manually to generate reviewable artifacts without publishing a public release.

Optimizations are deliberately conservative: single-file bundling with compression, no PDB files or ReadyToRun payload, and no duplicated multi-RID runtime archives. IL trimming is **not** enabled because Avalonia's XAML and reflection-based code need runtime verification before aggressive trimming; smaller file size must not come at the cost of a broken app.

For platform packaging details and signing requirements, see [PACKAGING.md](docs/PACKAGING.md).

## License

[MIT](LICENSE) · Copyright © 2019–2026 bestyize.
