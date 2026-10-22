namespace PictureFetcher.Core;

/// <summary>
/// Framebuffer-level SSD1306 display simulator. This does not emulate the I2C/SPI command bus.
/// Page writes, display enable and display inversion operate on actual packed RAM bytes.
/// </summary>
public sealed class ScreenSimulator
{
    private readonly byte[] _ram;
    public DisplayProfile Profile { get; }
    public bool IsPoweredOn { get; set; } = true;
    public bool IsInverted { get; set; }

    public ScreenSimulator(DisplayProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _ram = new byte[profile.BufferSize];
    }

    public void Clear() => Array.Clear(_ram);

    public void LoadFrame(MonochromeFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Profile.Width != Profile.Width || frame.Profile.Height != Profile.Height)
            throw new ArgumentException("Frame dimensions do not match this display.", nameof(frame));
        LoadPageBytes(frame.ToPageBytes());
    }

    public void LoadPageBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != _ram.Length)
            throw new ArgumentException("Expected a complete page-ordered framebuffer.", nameof(bytes));
        bytes.CopyTo(_ram);
    }

    public byte[] ReadPageBytes() => (byte[])_ram.Clone();

    /// <summary>Writes a contiguous portion of one page at the selected visible column.</summary>
    public void WritePage(int page, int startColumn, ReadOnlySpan<byte> bytes)
    {
        if ((uint)page >= (uint)Profile.PageCount) throw new ArgumentOutOfRangeException(nameof(page));
        if ((uint)startColumn >= (uint)Profile.Width) throw new ArgumentOutOfRangeException(nameof(startColumn));
        if (bytes.Length > Profile.Width - startColumn)
            throw new ArgumentException("Page write exceeds the visible display width.", nameof(bytes));
        bytes.CopyTo(_ram.AsSpan(page * Profile.Width + startColumn));
    }

    public bool PixelIsLit(int x, int y)
    {
        if ((uint)x >= (uint)Profile.Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Profile.Height) throw new ArgumentOutOfRangeException(nameof(y));
        if (!IsPoweredOn) return false;
        bool bit = (_ram[(y / 8) * Profile.Width + x] & (1 << (y % 8))) != 0;
        return IsInverted ? !bit : bit;
    }
}
