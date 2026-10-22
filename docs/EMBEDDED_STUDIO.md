# PictureFetcher Embedded Graphics Studio

## Product direction

PictureFetcher should become a cross-platform asset workstation for developers of small displays, not a full MCU emulator. It combines:

1. **Assets**: import and transform raster artwork, and later fonts, icons and animations.
2. **Display preview**: show the exact packed framebuffer data on a virtual pixel display.
3. **Export**: generate bytes and source files for the selected controller and pixel format.

## Implemented in the first increment

| Capability | Status |
| --- | --- |
| SSD1306 128×64 monochrome profile | Available; 1024-byte page buffer |
| SSD1306 128×32 monochrome profile | Available; 512-byte page buffer |
| RGBA nearest-neighbor threshold conversion | Available; original polarity retained |
| Binary framebuffer page encoding (vertical LSB-first) | Available |
| Framebuffer page writes and pixel reads | Available via `ScreenSimulator` |
| Simulated display enable / inversion | Available; does not modify exported data |
| Virtual screen pixel grid and bezel | Available in Avalonia, rendered through SkiaSharp |
| Cyan / amber / white simulated illumination | Available; presentation only |
| BMP, hexadecimal and C array export | Available for both built-in profiles |
| Full SSD1306 command decoder and I2C/SPI protocols | Not implemented |
| SH1106 132-column memory offset support | Not implemented |
| ST7735/ST7789/ILI9341 RGB color modes | Not implemented |
| Animated frame/timeline previews | Not implemented |

## Separation of concerns

- `DisplayProfile`: immutable identity and visible display dimensions. A profile defines packed buffer size.
- `MonochromeConverter`: scales/composites RGBA sources and preserves the prior threshold polarity.
- `MonochromeFrame`: stores lit/unlit pixels, serializes controller page bytes and BMP.
- `ScreenSimulator`: controller-independent **framebuffer-level** simulation of SSD1306 page layout. Inversion and power affect display state only, not RAM.
- `ScreenPreviewRenderer`: SkiaSharp presentation adapter. Pixel layout and colors are visual conventions, not physical photometric calibration.
- `OledConverter`/`OledFrame`: retained compatibility API for previous 128×64 consumers.

The reusable core has no Avalonia/SkiaSharp dependency. UI rendering must not be a source of truth for exported bytes. Do not label future color display previews "SSD1306": profile/encoder/device families should be explicit.

## Next phases

**Phase 2 — display controller adapters**
Define controller-specific addressing and command transports. Add SH1106 (132-column RAM, visible-column mapping), SSD1306 command decode (addressing modes, display enable/invert, start-line and segment remap where useful), then cross-check with golden byte fixtures.

**Phase 3 — resource authoring**
Pixel editor, grid overlays, glyph and font pipeline, frames/timeline for animated images, reusable symbol names and batch export. Offer dithering, fit/crop strategies, alpha backgrounds, and naming presets without silently changing legacy conversions.

**Phase 4 — color displays and firmware integration**
Introduce RGB565/RGB888 profiles for ST7735/ST7789/ILI9341 with byte-order and endianness controls. Add Arduino/ESP-IDF/STM32 ecosystem presets and serial/hardware preview only with explicit device transport selection.

## Verification

```bash
dotnet run --project tests/PictureFetcher.Core.Tests/PictureFetcher.Core.Tests.csproj -c Release
dotnet build PictureFetcher.sln -c Release
```

CI additionally packages Windows x64, Linux x64, macOS arm64 and macOS x64 and attempts a Linux GUI smoke launch. Always keep the original 128×64 conversion regression tests passing.
