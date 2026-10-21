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
Console.WriteLine("All core tests passed.");
