# PictureFetcher Embedded Graphics Studio

## Product direction

PictureFetcher should become a cross-platform asset workstation for developers of small displays, not a full MCU emulator. It combines:

1. **Assets**: import and transform raster artwork, and later fonts, icons and animations.
2. **Display preview**: show the exact packed framebuffer data on a virtual pixel display.
3. **Export**: generate bytes and source files for the selected controller and pixel format.

## Implemented capabilities (phases 1 and 2)

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
| SSD1306 command-byte parser | Available: page/horizontal/vertical windows; power, inverse, scan and remap |
| SH1106 132-column RAM and visible offset | Available: selectable default offset (+2), page writes and readback |
| Controller TX command/data export | Available: SSD1306 horizontal-mode and SH1106 per-page transfers |
| I2C/SPI electrical protocol simulation | Not implemented; command/data streams only |
| SSD1306 scrolling/analog register effects | Not implemented (explicitly rejected or parameter-consumed without physical effects) |
| ST7735/ST7789/ILI9341 RGB color modes | Not implemented |
| Animated frame/timeline previews | Not implemented |

## Separation of concerns

- `DisplayProfile`: immutable identity and visible display dimensions. A profile defines packed buffer size.
- `MonochromeConverter`: scales/composites RGBA sources and preserves the prior threshold polarity.
- `MonochromeFrame`: stores lit/unlit pixels, serializes controller page bytes and BMP.
- `ScreenSimulator`: controller-independent **framebuffer-level** simulation of SSD1306 page layout. Inversion and power affect display state only, not RAM.
- `Ssd1306CommandSimulator`: byte-stream state machine with address windows, hardware page/horizontal/vertical cursor progression and visible scan options.
- `Sh1106CommandSimulator`: separate 132-column RAM layout, default +2-column visible crop, and explicit page addressing.
- `Sh1106PageTransferEncoder`: writes 128 visible bytes per page with page and column-position commands; does not prepend I2C control bytes.
- `ScreenPreviewRenderer`: SkiaSharp presentation adapter. Pixel layout and colors are visual conventions, not physical photometric calibration.
- `OledConverter`/`OledFrame`: retained compatibility API for previous 128×64 consumers.

The reusable core has no Avalonia/SkiaSharp dependency. UI rendering must not be a source of truth for exported bytes. Do not label future color display previews "SSD1306": profile/encoder/device families should be explicit.

## Next phases

**Phase 2 — display controller adapters (implemented baseline)**
Added SSD1306 byte-stream decoding (page/horizontal/vertical data writes, address windows, power/invert/all-pixels-on, start-line, display offset, SEG/COM orientation); separate SH1106 page addressing, 132-column RAM, 128-column visible window and +2-column default offset. Regression coverage includes wraparound and generated transfer round-trips. Scroll animation, analog timing and electrical transport are intentionally not emulated.

**Follow-up — firmware command harness**
Allow pasting/importing firmware command/data traces, run them step-by-step, show GDDRAM inspection, and model additional display commands with datasheet fixtures. The existing "Controller TX" export is a textual sequence of logical command/data writes, **not** physical bus packets.

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

## Hardware reference sources

- [Solomon Systech SSD1306 datasheet](https://www.olimex.com/Products/Modules/LCD/MOD-OLED-128x64/resources/SSD1306.pdf) — page addressing reset default and `20/21/22` modes/windows.
- [SH1106 datasheet (NXP community mirror)](https://community.nxp.com/pwmxy87654/attachments/pwmxy87654/kinetis/46518/1/SH1106%20datasheet.pdf) — 132-column page-organized GDDRAM.
- [Linux SH1106 framebuffer example](https://android.googlesource.com/kernel/common/+/52d1e606ee733/drivers/staging/fbtft/fb_sh1106.c) — typical two-column visible-panel offset.

SH1106's 2-column offset is **module-specific**, not universal for every panel. UI uses the common setting; core supports other values between 0 and 4. SSD1306 module-dependent scan orientations likewise are not inferred automatically.
