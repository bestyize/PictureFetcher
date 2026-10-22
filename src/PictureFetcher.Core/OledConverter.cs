using System.Text;

namespace PictureFetcher.Core;

/// <summary>Pure image-to-OLED conversion. Input pixels use RGBA byte order.</summary>
public static class OledConverter
{
    public const int Width = 128;
    public const int Height = 64;

    public static OledFrame Convert(ReadOnlySpan<byte> rgba, int sourceWidth, int sourceHeight,
        int threshold = 300, bool invert = false)
    {
        // Keep the original 128x64 API and byte layout stable for existing integrations.
        var frame = MonochromeConverter.Convert(rgba, sourceWidth, sourceHeight,
            DisplayProfiles.Ssd1306_128x64, threshold, invert);
        return new OledFrame(frame.ToPixelBytes());
    }
}

/// <summary>128×64 monochrome frame. White pixels are 1; black pixels are 0.</summary>
public sealed class OledFrame
{
    private readonly byte[] _pixels;

    public OledFrame(byte[] pixels)
    {
        if (pixels is null || pixels.Length != OledConverter.Width * OledConverter.Height)
            throw new ArgumentException("Expected a 128×64 pixel buffer.", nameof(pixels));
        _pixels = (byte[])pixels.Clone();
    }

    public bool IsWhite(int x, int y) => _pixels[y * OledConverter.Width + x] != 0;

    /// <summary>SSD1306 page order, vertical LSB-first packing, exactly 1024 bytes.</summary>
    public byte[] ToPageBytes()
    {
        byte[] bytes = new byte[OledConverter.Width * OledConverter.Height / 8];
        for (int page = 0; page < OledConverter.Height / 8; page++)
            for (int x = 0; x < OledConverter.Width; x++)
                for (int bit = 0; bit < 8; bit++)
                    if (IsWhite(x, page * 8 + bit))
                        bytes[page * OledConverter.Width + x] |= (byte)(1 << bit);
        return bytes;
    }

    public string ToHexText()
    {
        byte[] bytes = ToPageBytes();
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && i % 16 == 0) sb.AppendLine();
            sb.Append(bytes[i].ToString("X2")).Append(i % 16 == 15 ? "" : " ");
        }
        return sb.ToString();
    }

    public string ToCArray()
    {
        byte[] bytes = ToPageBytes();
        var sb = new StringBuilder("const unsigned char yize_image[1024] = {\n");
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i % 16 == 0) sb.Append("    ");
            sb.Append("0x").Append(bytes[i].ToString("X2"));
            if (i != bytes.Length - 1) sb.Append(", ");
            if (i % 16 == 15) sb.AppendLine();
        }
        return sb.Append("};").ToString();
    }

    /// <summary>Portable, uncompressed 24-bit BMP output with no native graphics dependency.</summary>
    public byte[] ToBmp()
    {
        const int width = OledConverter.Width;
        const int height = OledConverter.Height;
        const int pixelOffset = 54;
        int rowSize = (width * 3 + 3) & ~3;
        byte[] bmp = new byte[pixelOffset + rowSize * height];
        using (var stream = new MemoryStream(bmp))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(bmp.Length);
            writer.Write(0);
            writer.Write(pixelOffset);
            writer.Write(40);
            writer.Write(width);
            writer.Write(height);
            writer.Write((short)1);
            writer.Write((short)24);
            writer.Write(0);
            writer.Write(rowSize * height);
            writer.Write(2835);
            writer.Write(2835);
            writer.Write(0);
            writer.Write(0);
            for (int y = height - 1; y >= 0; y--)
                for (int x = 0; x < width; x++)
                {
                    byte c = IsWhite(x, y) ? (byte)255 : (byte)0;
                    writer.Write(c); writer.Write(c); writer.Write(c);
                }
        }
        return bmp;
    }
}
