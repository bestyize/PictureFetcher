namespace PictureFetcher.Core;

/// <summary>
/// SH1106 132-column GDDRAM, 128-column visible window, page addressing only.
/// The default +2 glass offset is a common 128x64 module mapping; board wiring
/// can differ, so callers may select another valid visible start column.
/// No I2C/SPI framing, PWM/contrast, voltage, scroll timing or analog effects.
/// </summary>
public sealed class Sh1106CommandSimulator
{
    public const int RamWidth = 132;
    private readonly byte[] _ram;
    private int _column, _page, _pendingArgs;
    private byte _pendingOpcode;
    public DisplayProfile Profile { get; }
    public int VisibleColumnOffset { get; }
    public int CurrentColumn => _column;
    public int CurrentPage => _page;
    public bool DisplayEnabled { get; private set; }
    public bool Inverted { get; private set; }
    public bool AllPixelsOn { get; private set; }
    public bool SegmentRemapped { get; private set; }
    public bool ComScanReversed { get; private set; }
    public int DisplayStartLine { get; private set; }
    public int DisplayOffset { get; private set; }

    public Sh1106CommandSimulator(DisplayProfile profile, int visibleColumnOffset = 2)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!profile.Id.StartsWith("sh1106-", StringComparison.Ordinal)
            || profile.Width != 128 || profile.Height != 64)
            throw new ArgumentException("Expected an SH1106 128x64 profile.", nameof(profile));
        if (visibleColumnOffset < 0 || visibleColumnOffset > RamWidth - profile.Width)
            throw new ArgumentOutOfRangeException(nameof(visibleColumnOffset));
        VisibleColumnOffset = visibleColumnOffset;
        _ram = new byte[checked(RamWidth * profile.PageCount)];
        Reset();
    }

    public void Reset()
    {
        Array.Clear(_ram);
        _column = 0; _page = 0; _pendingArgs = 0; _pendingOpcode = 0;
        DisplayEnabled = false; Inverted = false; AllPixelsOn = false;
        SegmentRemapped = false; ComScanReversed = false;
        DisplayStartLine = 0; DisplayOffset = 0;
    }

    public void WriteCommands(ReadOnlySpan<byte> commands)
    {
        foreach (byte command in commands) WriteCommand(command);
    }

    public void WriteCommand(byte command)
    {
        if (_pendingArgs != 0)
        {
            _pendingArgs = 0;
            if (_pendingOpcode == 0xD3) DisplayOffset = (command & 0x3F) % Profile.Height;
            // All other supported parameter registers are consumed but are not
            // rendered (analog/timing properties are out of scope).
            return;
        }
        if (command is >= 0x00 and <= 0x0F)
            SetColumn((_column & 0xF0) | (command & 0x0F));
        else if (command is >= 0x10 and <= 0x1F)
            SetColumn(((command & 0x0F) << 4) | (_column & 0x0F));
        else if (command is >= 0x40 and <= 0x7F)
            DisplayStartLine = (command & 0x3F) % Profile.Height;
        else if (command is >= 0xB0 and <= 0xB7)
            _page = command & 7;
        else switch (command)
        {
            case 0x81: case 0xAD: case 0xA8: case 0xD3:
            case 0xD5: case 0xD9: case 0xDA: case 0xDB:
                _pendingOpcode = command;
                _pendingArgs = 1;
                break;
            case 0xA0: SegmentRemapped = false; break;
            case 0xA1: SegmentRemapped = true; break;
            case 0xA4: AllPixelsOn = false; break;
            case 0xA5: AllPixelsOn = true; break;
            case 0xA6: Inverted = false; break;
            case 0xA7: Inverted = true; break;
            case 0xAE: DisplayEnabled = false; break;
            case 0xAF: DisplayEnabled = true; break;
            case 0xC0: ComScanReversed = false; break;
            case 0xC8: ComScanReversed = true; break;
            case 0xE3: break;
            default: throw new NotSupportedException($"SH1106 command 0x{command:X2} is not simulated.");
        }
    }

    private void SetColumn(int column)
    {
        if (column >= RamWidth)
            throw new ArgumentOutOfRangeException(nameof(column), "SH1106 has 132 RAM columns.");
        _column = column;
    }

    public void WriteData(ReadOnlySpan<byte> data)
    {
        if (_pendingArgs != 0)
            throw new InvalidOperationException("Finish the pending SH1106 command first.");
        foreach (byte value in data)
        {
            _ram[_page * RamWidth + _column] = value;
            _column = (_column + 1) % RamWidth;
            // Unlike SSD1306 horizontal mode, the SH1106 does not automatically
            // step to the next page. Select B0..B7 for every page write.
        }
    }

    public void LoadVisibleFrame(MonochromeFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Profile.Width != Profile.Width || frame.Profile.Height != Profile.Height)
            throw new ArgumentException("Frame dimensions do not match this display.", nameof(frame));
        foreach (var transfer in Sh1106PageTransferEncoder.Encode(frame, VisibleColumnOffset))
        {
            WriteCommands(transfer.Commands);
            WriteData(transfer.Data);
        }
    }

    /// <summary>Entire 132-by-64 raw GDDRAM, independent of screen appearance.</summary>
    public byte[] ReadGddram() => (byte[])_ram.Clone();

    /// <summary>128x64 packed bytes cropped to the actual module glass window.</summary>
    public byte[] ReadVisiblePageBytes()
    {
        byte[] visible = new byte[Profile.BufferSize];
        for (int page = 0; page < Profile.PageCount; page++)
            Array.Copy(_ram, page * RamWidth + VisibleColumnOffset, visible,
                page * Profile.Width, Profile.Width);
        return visible;
    }

    public bool PixelIsLit(int x, int y)
    {
        if ((uint)x >= (uint)Profile.Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Profile.Height) throw new ArgumentOutOfRangeException(nameof(y));
        if (!DisplayEnabled) return false;
        if (AllPixelsOn) return true;
        int col = VisibleColumnOffset + (SegmentRemapped ? Profile.Width - 1 - x : x);
        int row = ComScanReversed ? Profile.Height - 1 - y : y;
        int memoryRow = (row + DisplayStartLine + DisplayOffset) % Profile.Height;
        bool bit = (_ram[(memoryRow / 8) * RamWidth + col] & (1 << (memoryRow % 8))) != 0;
        return Inverted ? !bit : bit;
    }

    public ScreenSimulator SnapshotDisplay()
    {
        var screen = new ScreenSimulator(Profile);
        var packed = new byte[Profile.BufferSize];
        for (int y = 0; y < Profile.Height; y++)
            for (int x = 0; x < Profile.Width; x++)
                if (PixelIsLit(x, y))
                    packed[y / 8 * Profile.Width + x] |= (byte)(1 << (y % 8));
        screen.LoadPageBytes(packed);
        return screen;
    }
}

/// <summary>One complete visible page plus its SH1106 addressing commands.</summary>
public sealed class Sh1106PageTransfer
{
    public int Page { get; }
    public byte[] Commands { get; }
    public byte[] Data { get; }

    public Sh1106PageTransfer(int page, byte[] commands, byte[] data)
    {
        Page = page;
        Commands = (byte[])commands.Clone();
        Data = (byte[])data.Clone();
    }
}

public static class Sh1106PageTransferEncoder
{
    /// <summary>Page-by-page command/data packets, with the default 2-column glass offset.</summary>
    public static IReadOnlyList<Sh1106PageTransfer> Encode(MonochromeFrame frame, int visibleColumnOffset = 2)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Profile.Width != 128 || frame.Profile.Height != 64)
            throw new ArgumentException("SH1106 module transfer expects 128x64 artwork.", nameof(frame));
        if (visibleColumnOffset < 0 || visibleColumnOffset > Sh1106CommandSimulator.RamWidth - frame.Profile.Width)
            throw new ArgumentOutOfRangeException(nameof(visibleColumnOffset));

        byte[] all = frame.ToPageBytes();
        var transfers = new List<Sh1106PageTransfer>(frame.Profile.PageCount);
        for (int page = 0; page < frame.Profile.PageCount; page++)
        {
            byte[] data = new byte[frame.Profile.Width];
            Array.Copy(all, page * frame.Profile.Width, data, 0, data.Length);
            transfers.Add(new Sh1106PageTransfer(page,
                new[] { (byte)(0xB0 | page), (byte)(visibleColumnOffset & 0x0F),
                        (byte)(0x10 | (visibleColumnOffset >> 4)) }, data));
        }
        return transfers;
    }
}
