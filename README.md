# PictureFetcher

**English** · [简体中文](README.zh-CN.md)

A cross-platform **embedded graphics studio** for preparing monochrome display assets and testing them in a virtual OLED screen. Powered by .NET 10 and Avalonia.

## Package size optimization

The desktop application uses conservative **partial .NET IL trimming** while preserving self-contained single-file executables. On matched GitHub Actions builds, compressed output shrank by **61–66%** across Windows x64, Linux x64, and both macOS architectures versus untrimmed outputs. Windows ZIP and macOS DMG distribution sizes may differ from the tar.gz measurement baseline; see [packaging measurements](docs/PACKAGING.md). Chinese localization, the controller simulators, and the Linux Avalonia GUI startup were tested after trimming.

## Latest release — v2.1.1

This release introduces the SSD1306 command-stream simulator, SH1106 132-column RAM model, controller-specific TX view, and the on-device OLED preview added in v2.0.0. Windows x64 and Linux x64 distribution archives are produced automatically; macOS DMGs are published **only after Developer ID notarization** (otherwise they remain downloadable as temporary CI workflow artifacts, not public release assets).

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
- Open or drag an image into the editor; preview the original and the converted bitmap.
- Choose SSD1306 128 × 64 / 128 × 32, or SH1106 128 × 64 with a 132-column hardware RAM window and a configurable 2-column default glass offset in the core.
- Simulate a virtual OLED display with a pixel-grid bezel, cyan/amber/white illumination, power and hardware-style display inversion.
- Adjustable threshold and invert control.
- Export HEX and C arrays as before, or switch to **Controller TX** to inspect command/data bytes for SSD1306 horizontal mode or SH1106 page writes (1024/512 visible image bytes).
- Save individual BMP images or batch-convert multiple images.

**Simulator scope:** The command interpreters now support SSD1306 page/horizontal/vertical RAM addressing, address windows, display power/inversion/remap and selected scan settings; SH1106 supports its own 132-column, page-only addressing, visible window and page-transfer sequences. Unimplemented commands fail explicitly; analog settings are accepted without rendering a physical effect. This is a **command/data byte-stream simulator, not an I2C/SPI electrical or firmware/MCU emulator**, and does not model continuous scrolling or full-color displays. See [Embedded studio architecture](docs/EMBEDDED_STUDIO.md) for the phased roadmap.

**Conversion compatibility:** The original app's threshold polarity is preserved: a pixel with R+G+B above the threshold is rendered black (unless Invert is enabled). This can differ from other image tools. The monochrome byte encoding is vertical LSB-first, with 128 bytes per visible page (8 pages at 128×64, 4 at 128×32).

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

CI validates the portable conversion core and generates all four self-contained runtime targets; it also smoke-tests the platform-specific package formats. Update the project `<Version>` and the top-level `VERSION` file together. After those changes are merged into `master`, GitHub Actions automatically creates the version tag and publishes Windows/Linux archives, checksums, and any notarized macOS DMGs to GitHub Releases. Pushing a `vX.Y.Z` tag manually is also supported. You can invoke the release workflow manually to generate reviewable artifacts without publishing a public release.

Optimizations are deliberately conservative: single-file bundling with compression, no PDB files or ReadyToRun payload, and no duplicated multi-RID runtime archives. IL trimming is **not** enabled because Avalonia's XAML and reflection-based code need runtime verification before aggressive trimming; smaller file size must not come at the cost of a broken app.

For platform packaging details and signing requirements, see [PACKAGING.md](docs/PACKAGING.md).

## License

[MIT](LICENSE) · Copyright © 2019–2026 bestyize.
