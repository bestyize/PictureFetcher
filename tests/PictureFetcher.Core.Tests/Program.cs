using PictureFetcher.Core;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
}

var whiteRgba = new byte[] { 255, 255, 255, 255 };
var blackRgba = new byte[] { 0, 0, 0, 255 };
var white = OledConverter.Convert(blackRgba, 1, 1, 300);
Check(white.ToPageBytes().Length == 1024, "Exactly 1024 OLED bytes");
Check(white.ToPageBytes().All(b => b == 0xFF), "Single dark source pixel upscales without zero-step loops");
Check(OledConverter.Convert(whiteRgba, 1, 1, 300).ToPageBytes().All(b => b == 0),
    "Legacy brightness polarity preserved");
Check(OledConverter.Convert(blackRgba, 1, 1, 300, true).ToPageBytes().All(b => b == 0),
    "Invert flips monochrome pixels");
Check(white.ToCArray().Contains("0xFF") && white.ToCArray().Contains("[1024]"),
    "C array is formatted");
Check(white.ToHexText().StartsWith("FF FF"), "Hex page bytes");
var bmp = white.ToBmp();
Check(bmp[0] == (byte)'B' && bmp[1] == (byte)'M' && bmp.Length == 54 + 128 * 64 * 3,
    "Valid-size 24-bit BMP header and payload");
var pattern = new byte[128 * 64];
pattern[0] = 1;
pattern[128 * 7] = 1;
pattern[128 * 8 + 1] = 1;
var pages = new OledFrame(pattern).ToPageBytes();
Check(pages[0] == 0x81 && pages[128 + 1] == 0x01, "Page orientation is LSB-first and correct");
Check(OledConverter.Convert(new byte[] { 0, 0, 0, 0 }, 1, 1).ToPageBytes().All(b => b == 0),
    "Transparent pixels composited over white");
try { OledConverter.Convert(blackRgba, 0, 1); throw new Exception("Missing dimension validation"); }
catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS: Invalid dimensions rejected"); }
var smallProfile = DisplayProfiles.Ssd1306_128x32;
var small = MonochromeConverter.Convert(blackRgba, 1, 1, smallProfile);
Check(small.ToPageBytes().Length == 512 && small.ToPageBytes().All(b => b == 0xFF),
    "128x32 profile packs four pages");
Check(small.ToBmp().Length == 54 + 128 * 32 * 3, "Profile controls BMP dimensions");
Check(small.ToCArray().Contains("[512]"), "C array follows selected profile");
Check(MonochromeConverter.Convert(whiteRgba, 1, 1, smallProfile).ToPageBytes().All(b => b == 0),
    "Dynamic conversion preserves legacy threshold polarity");
var sim = new ScreenSimulator(smallProfile);
sim.LoadFrame(small);
Check(sim.PixelIsLit(0, 0) && sim.PixelIsLit(127, 31), "Simulator reads page framebuffer");
sim.WritePage(0, 0, new byte[] { 0 });
Check(!sim.PixelIsLit(0, 0) && sim.PixelIsLit(1, 0), "Simulator accepts page writes");
sim.IsInverted = true;
Check(sim.PixelIsLit(0, 0) && !sim.PixelIsLit(1, 0), "Display inversion affects visible pixels");
sim.IsPoweredOn = false;
Check(!sim.PixelIsLit(0, 0), "Display power-off blanks visible pixels");
sim.IsPoweredOn = true;
Check(sim.PixelIsLit(0, 0), "Power cycling preserves video RAM");
Check(sim.ReadPageBytes().Length == 512, "Simulator exposes page-ordered framebuffer");
try { sim.LoadPageBytes(new byte[1024]); throw new Exception("Missing profile byte count check"); }
catch (ArgumentException) { Console.WriteLine("PASS: Invalid framebuffer length rejected"); }
try { sim.WritePage(0, 127, new byte[2]); throw new Exception("Missing page bounds check"); }
catch (ArgumentException) { Console.WriteLine("PASS: Out-of-bounds page write rejected"); }

var controller = new Ssd1306CommandSimulator(DisplayProfiles.Ssd1306_128x64);
Check(controller.AddressingMode == Ssd1306AddressingMode.Page && !controller.DisplayEnabled,
    "SSD1306 hardware reset mode and display state");
controller.WriteCommands(new byte[] { 0x20, 0x00, 0x21, 0x02, 0x03, 0x22, 0x01, 0x02 });
controller.WriteData(new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10 });
var gddram = controller.ReadGddram();
Check(gddram[128 + 2] == 0x01 && gddram[128 + 3] == 0x02 &&
      gddram[256 + 2] == 0x04 && gddram[256 + 3] == 0x08 &&
      gddram[128 + 2] == 0x10, "SSD1306 horizontal window wraps columns and pages");
Check(controller.CurrentPage == 1 && controller.CurrentColumn == 3,
    "SSD1306 address cursor updates after full window wrap");
controller.WriteCommands(new byte[] { 0x20, 0x01, 0x21, 0, 1, 0x22, 0, 1 });
controller.WriteData(new byte[] { 0x11, 0x22, 0x33 });
Check(controller.ReadGddram()[0] == 0x11 && controller.ReadGddram()[128] == 0x22 &&
      controller.ReadGddram()[1] == 0x33, "SSD1306 vertical addressing increments pages first");
controller.Reset();
controller.WriteCommands(new byte[] { 0xB1, 0x03, 0x10 });
controller.WriteData(new byte[] { 0x80 });
controller.WriteCommand(0xAF);
Check(controller.PixelIsLit(3, 15), "SSD1306 page-addressed GDDRAM appears on screen");
controller.WriteCommand(0xA7);
Check(!controller.PixelIsLit(3, 15) && controller.PixelIsLit(2, 15),
    "SSD1306 inverse display is visible without changing RAM");
controller.WriteCommand(0xA6);
controller.WriteCommand(0xA1);
Check(controller.PixelIsLit(124, 15), "SSD1306 segment remap mirrors column mapping");
controller.WriteCommand(0xA0);
controller.WriteCommand(0xC8);
Check(controller.PixelIsLit(3, 48), "SSD1306 COM scan direction changes visible row");
controller.WriteCommand(0xC0);
controller.WriteCommands(new byte[] { 0xD3, 0x01 });
Check(controller.PixelIsLit(3, 14), "SSD1306 display offset shifts visible scan");
controller.WriteCommand(0xA5);
Check(controller.PixelIsLit(0, 0) && controller.PixelIsLit(127, 63),
    "SSD1306 force-all-on overrides video RAM");
controller.WriteCommand(0xAE);
Check(!controller.PixelIsLit(0, 0) && controller.ReadGddram()[128 + 3] == 0x80,
    "SSD1306 power-off blanks display without clearing RAM");
Check(controller.SnapshotDisplay().ReadPageBytes().All(b => b == 0),
    "SSD1306 snapshot obeys display enable");
controller.Reset();
controller.WriteCommand(0x21);
Check(controller.AwaitingCommandParameters, "Multi-byte SSD1306 command can span calls");
try { controller.WriteData(new byte[] { 0 }); throw new Exception("Missing unfinished-command guard"); }
catch (InvalidOperationException) { Console.WriteLine("PASS: Unfinished command blocks data writes"); }
controller.WriteCommands(new byte[] { 0, 127 });
try { controller.WriteCommand(0x26); throw new Exception("Unsupported scrolling silently accepted"); }
catch (NotSupportedException) { Console.WriteLine("PASS: Unsupported scroll rejected explicitly"); }

Console.WriteLine("All core tests passed.");
