using System.Text;

namespace PictureFetcher.Core;

/// <summary>Controller-independent 1-bit image. A set pixel is a lit display pixel.</summary>
public sealed class MonochromeFrame
{
    private readonly byte[] _pixels;

    public DisplayProfile Profile { get; }

    public MonochromeFrame(DisplayProfile profile, ReadOnlySpan<byte> pixels)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (pixels.Length != checked(profile.Width * profile.Height))
            throw new ArgumentException("Pixel count does not match the display profile.", nameof(pixels));
        _pixels = pixels.ToArray();
    }

    public bool IsOn(int x, int y)
    {
        if ((uint)x >= (uint)Profile.Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Profile.Height) throw new ArgumentOutOfRangeException(nameof(y));
        return _pixels[y * Profile.Width + x] != 0;
    }

    public byte[] ToPixelBytes() => (byte[])_pixels.Clone();

    /// <summary>SSD1306 page packing: successive columns, bit 0 at the top of each 8-row page.</summary>
    public byte[] ToPageBytes()
    {
        byte[] pages = new byte[Profile.BufferSize];
        for (int y = 0; y < Profile.Height; y++)
            for (int x = 0; x < Profile.Width; x++)
                if (_pixels[y * Profile.Width + x] != 0)
                    pages[(y / 8) * Profile.Width + x] |= (byte)(1 << (y % 8));
        return pages;
    }

    public string ToHexText()
    {
        byte[] data = ToPageBytes();
        var sb = new StringBuilder(data.Length * 3);
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0 && i % 16 == 0) sb.AppendLine();
            sb.Append(data[i].ToString("X2"));
            if (i % 16 != 15 && i != data.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }

    public string ToCArray(string symbol = "picturefetcher_image")
    {
        if (string.IsNullOrEmpty(symbol) || !(char.IsLetter(symbol[0]) || symbol[0] == '_') ||
            symbol.Any(c => !(char.IsLetterOrDigit(c) || c == '_')) || !symbol.All(c => c < 128))
            throw new ArgumentException("Symbol must be an ASCII C identifier.", nameof(symbol));
        byte[] data = ToPageBytes();
        var sb = new StringBuilder($"const unsigned char {symbol}[{data.Length}] = {{\n");
        for (int i = 0; i < data.Length; i++)
        {
            if (i % 16 == 0) sb.Append("    ");
            sb.Append("0x").Append(data[i].ToString("X2"));
            if (i != data.Length - 1) sb.Append(", ");
            if (i % 16 == 15) sb.AppendLine();
        }
        return sb.Append("};").ToString();
    }

    /// <summary>Uncompressed 24-bit BMP; independent of native graphics libraries.</summary>
    public byte[] ToBmp()
    {
        int width = Profile.Width, height = Profile.Height;
        int rowSize = checked((width * 3 + 3) & ~3);
        const int pixelOffset = 54;
        byte[] bmp = new byte[checked(pixelOffset + rowSize * height)];
        using (var stream = new MemoryStream(bmp))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)'B'); writer.Write((byte)'M');
            writer.Write(bmp.Length); writer.Write(0); writer.Write(pixelOffset);
            writer.Write(40); writer.Write(width); writer.Write(height);
            writer.Write((short)1); writer.Write((short)24); writer.Write(0);
            writer.Write(rowSize * height);
            writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    byte color = _pixels[y * width + x] != 0 ? (byte)255 : (byte)0;
                    writer.Write(color); writer.Write(color); writer.Write(color);
                }
                for (int pad = width * 3; pad < rowSize; pad++) writer.Write((byte)0);
            }
        }
        return bmp;
    }
}

public static class MonochromeConverter
{
    /// <summary>Nearest-neighbour RGBA conversion, preserving PictureFetcher's legacy threshold polarity.</summary>
    public static MonochromeFrame Convert(ReadOnlySpan<byte> rgba, int sourceWidth, int sourceHeight,
        DisplayProfile profile, int threshold = 300, bool invert = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (sourceWidth <= 0 || sourceHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceWidth), "Dimensions must be positive.");
        if (threshold is < 0 or > 765)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        if ((long)sourceWidth * sourceHeight * 4 > rgba.Length)
            throw new ArgumentException("RGBA buffer is smaller than the image dimensions.", nameof(rgba));

        byte[] pixels = new byte[checked(profile.Width * profile.Height)];
        for (int y = 0; y < profile.Height; y++)
        {
            int sourceY = Math.Min(sourceHeight - 1, (int)((y + 0.5) * sourceHeight / profile.Height));
            for (int x = 0; x < profile.Width; x++)
            {
                int sourceX = Math.Min(sourceWidth - 1, (int)((x + 0.5) * sourceWidth / profile.Width));
                int offset = checked((sourceY * sourceWidth + sourceX) * 4);
                int alpha = rgba[offset + 3], brightness = 0;
                for (int c = 0; c < 3; c++)
                    brightness += (rgba[offset + c] * alpha + 255 * (255 - alpha)) / 255;
                pixels[y * profile.Width + x] = (brightness <= threshold) != invert ? (byte)1 : (byte)0;
            }
        }
        return new MonochromeFrame(profile, pixels);
    }
}
