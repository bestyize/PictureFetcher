# PictureFetcher

[简体中文](README.zh-CN.md) | English

PictureFetcher is a lightweight Windows desktop utility for preparing images for **128 × 64 monochrome OLED displays**. It can resize and binarize images, batch-convert them to BMP files, and generate hexadecimal or C-array-style bitmap data for embedded projects.

## Features

- Convert images into 128 × 64 monochrome previews with an adjustable threshold.
- Invert pixel colors and switch between hexadecimal and C array output modes.
- Open a single image, or batch-convert multiple images to BMP files alongside their originals.
- Adjust window opacity.
- Switch the interface between **English** and **Simplified Chinese**. The app initially follows the Windows UI language (Chinese or English fallback) and remembers your manual selection.

## Download

Go to [GitHub Releases](https://github.com/bestyize/PictureFetcher/releases) to download a Windows build. Release archives include self-contained executables: the .NET desktop runtime does **not** need to be installed separately.

- `win-x64`: most Intel/AMD Windows 10/11 PCs.
- `win-arm64`: ARM-based Windows PCs.

Extract the ZIP and launch `PictureReader.exe`. There is no installer and the build is not code-signed; Windows SmartScreen may ask you to confirm the publisher.

**Platform support:** Windows only. The UI uses Windows Forms; targeting .NET 10 does not make the application compatible with macOS or Linux.

## Usage

1. Choose **Open image** to load a single image, or **Batch convert** to select multiple images.
2. Adjust the threshold or enable **Invert** to change the black/white conversion.
3. Choose **Hex mode** or **C array** to view the generated bitmap data.
4. Select **Save BMP** to export the current image; batch conversion exports BMP files automatically.
5. Use the **Language** selector in the upper-right corner to change UI language.

**Note:** BMP files are saved with the same base names as their source images in the same directories. Back up any existing BMPs with those names before converting.

## Build from source

Requires the **.NET 10 SDK** and a Windows environment (or a Windows-capable build agent).

```powershell
dotnet restore PictureFetcher.csproj
dotnet build PictureFetcher.csproj --configuration Release
dotnet run --project PictureFetcher.csproj
```

For a self-contained, single-file Windows x64 executable:

```powershell
dotnet publish PictureFetcher.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None --output publish/win-x64
```

## Automation

- **CI** builds the application on Windows for pushes and pull requests.
- **Release** packages Windows x64 and ARM64 builds when a `v*` tag is pushed; it uploads the ZIPs as GitHub Release assets. Manual runs produce downloadable workflow artifacts without publishing a release.

To publish a version after merging a PR:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## License

Licensed under the [MIT License](LICENSE). Copyright © 2019–2026 bestyize.
