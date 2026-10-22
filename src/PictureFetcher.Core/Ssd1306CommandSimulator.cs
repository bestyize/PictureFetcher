namespace PictureFetcher.Core;

/// <summary>SSD1306 GDDRAM addressing modes. Page is the hardware reset mode.</summary>
public enum Ssd1306AddressingMode { Horizontal = 0, Vertical = 1, Page = 2 }

/// <summary>
/// Byte-stream SSD1306 command/data simulator. Models addressing, visible scan mapping,
/// normal/inverted/all-on/power display state and packed GDDRAM, not the electrical bus,
/// charge pump, oscillator, analog contrast or scrolling.
///
/// Command parameters can be streamed over multiple WriteCommands calls.
/// </summary>
public sealed class Ssd1306CommandSimulator
{
    private readonly ScreenSimulator _ram;
    private int _columnStart, _columnEnd, _pageStart, _pageEnd;
    private int _column, _page;
    private byte _pendingOpcode;
    private readonly byte[] _arguments = new byte[2];
    private int _pendingLength, _argumentCount;

    public DisplayProfile Profile { get; }
    public Ssd1306AddressingMode AddressingMode { get; private set; }
    public int CurrentColumn => _column;
    public int CurrentPage => _page;
    public bool DisplayEnabled { get; private set; }
    public bool Inverted { get; private set; }
    public bool AllPixelsOn { get; private set; }
    public bool SegmentRemapped { get; private set; }
    public bool ComScanReversed { get; private set; }
    public int DisplayStartLine { get; private set; }
    public int DisplayOffset { get; private set; }
    public bool AwaitingCommandParameters => _pendingLength != 0;

    public Ssd1306CommandSimulator(DisplayProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!profile.Id.StartsWith("ssd1306-", StringComparison.Ordinal))
            throw new ArgumentException("Expected an SSD1306 profile.", nameof(profile));
        _ram = new ScreenSimulator(profile);
        Reset();
    }

    public void Reset()
    {
        _ram.Clear();
        _columnStart = 0;
        _columnEnd = Profile.Width - 1;
        _pageStart = 0;
        _pageEnd = Profile.PageCount - 1;
        _column = 0; _page = 0;
        AddressingMode = Ssd1306AddressingMode.Page;
        DisplayEnabled = false; Inverted = false; AllPixelsOn = false;
        SegmentRemapped = false; ComScanReversed = false;
        DisplayStartLine = 0; DisplayOffset = 0;
        _pendingLength = 0; _argumentCount = 0;
    }

    public void WriteCommands(ReadOnlySpan<byte> commands)
    {
        foreach (byte command in commands) WriteCommand(command);
    }

    public void WriteCommand(byte command)
    {
        if (_pendingLength > 0)
        {
            _arguments[_argumentCount++] = command;
            if (_argumentCount == _pendingLength)
            {
                byte opcode = _pendingOpcode;
                _pendingLength = 0;
                _argumentCount = 0;
                ApplyParameters(opcode);
            }
            return;
        }

        if (command is >= 0x00 and <= 0x0F)
        {
            RequirePageMode();
            _column = ((_column & 0xF0) | (command & 0x0F)) % Profile.Width;
        }
        else if (command is >= 0x10 and <= 0x1F)
        {
            RequirePageMode();
            _column = (((command & 0x0F) << 4) | (_column & 0x0F)) % Profile.Width;
        }
        else if (command is >= 0x40 and <= 0x7F) DisplayStartLine = (command & 0x3F) % Profile.Height;
        else if (command is >= 0xB0 and <= 0xB7)
        {
            RequirePageMode();
            int page = command & 7;
            if (page >= Profile.PageCount)
                throw new ArgumentOutOfRangeException(nameof(command), "Page exceeds display dimensions.");
            _page = page;
        }
        else
        {
            switch (command)
            {
                case 0x20: case 0x81: case 0x8D: case 0xA8:
                case 0xD3: case 0xD5: case 0xD9: case 0xDA: case 0xDB:
                    BeginParameters(command, 1); break;
                case 0x21: case 0x22: case 0xA3:
                    BeginParameters(command, 2); break;
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
                case 0x2E: case 0xE3: break; // Stop-scroll / NOP; scroll animation not modeled.
                default: throw new NotSupportedException($"SSD1306 command 0x{command:X2} is not simulated.");
            }
        }
    }

    private void BeginParameters(byte opcode, int count)
    {
        _pendingOpcode = opcode;
        _pendingLength = count;
        _argumentCount = 0;
    }

    private void ApplyParameters(byte opcode)
    {
        int start = _arguments[0], end = _arguments[1];
        switch (opcode)
        {
            case 0x20:
                if (start > 2) throw new ArgumentOutOfRangeException(nameof(start), "Invalid address mode.");
                AddressingMode = (Ssd1306AddressingMode)start;
                _column = AddressingMode == Ssd1306AddressingMode.Page ? _column : _columnStart;
                _page = AddressingMode == Ssd1306AddressingMode.Page ? _page : _pageStart;
                break;
            case 0x21:
                if (start >= Profile.Width || end >= Profile.Width || start > end)
                    throw new ArgumentOutOfRangeException(nameof(start), "Invalid column window.");
                _columnStart = start; _columnEnd = end; _column = start;
                break;
            case 0x22:
                if (start >= Profile.PageCount || end >= Profile.PageCount || start > end)
                    throw new ArgumentOutOfRangeException(nameof(start), "Invalid page window.");
                _pageStart = start; _pageEnd = end; _page = start;
                break;
            case 0xD3: DisplayOffset = (start & 0x3F) % Profile.Height; break;
            // Register parameters below are consumed to prevent command-stream desynchronization;
            // analog/timing configuration is outside this framebuffer simulator's scope.
            case 0x81: case 0x8D: case 0xA8: case 0xD5:
            case 0xD9: case 0xDA: case 0xDB: case 0xA3: break;
        }
    }

    private void RequirePageMode()
    {
        if (AddressingMode != Ssd1306AddressingMode.Page)
            throw new InvalidOperationException("Page-addressing commands require page mode (0x20 0x02).");
    }

    public void WriteData(ReadOnlySpan<byte> data)
    {
        if (AwaitingCommandParameters)
            throw new InvalidOperationException("Finish the pending command before writing display data.");
        foreach (byte value in data)
        {
            Span<byte> item = stackalloc byte[1] { value };
            _ram.WritePage(_page, _column, item);
            switch (AddressingMode)
            {
                case Ssd1306AddressingMode.Page:
                    _column = (_column + 1) % Profile.Width;
                    break;
                case Ssd1306AddressingMode.Horizontal:
                    if (++_column > _columnEnd)
                    {
                        _column = _columnStart;
                        if (++_page > _pageEnd) _page = _pageStart;
                    }
                    break;
                case Ssd1306AddressingMode.Vertical:
                    if (++_page > _pageEnd)
                    {
                        _page = _pageStart;
                        if (++_column > _columnEnd) _column = _columnStart;
                    }
                    break;
            }
        }
    }

    /// <summary>Unmodified packed controller RAM, regardless of display state.</summary>
    public byte[] ReadGddram() => _ram.ReadPageBytes();

    public bool PixelIsLit(int x, int y)
    {
        if ((uint)x >= (uint)Profile.Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Profile.Height) throw new ArgumentOutOfRangeException(nameof(y));
        if (!DisplayEnabled) return false;
        if (AllPixelsOn) return true;
        int memoryX = SegmentRemapped ? Profile.Width - 1 - x : x;
        int row = ComScanReversed ? Profile.Height - 1 - y : y;
        int memoryY = (row + DisplayStartLine + DisplayOffset) % Profile.Height;
        bool on = _ram.PixelIsLit(memoryX, memoryY);
        return Inverted ? !on : on;
    }

    /// <summary>Visible state snapshot suitable for the existing Avalonia screen renderer.</summary>
    public ScreenSimulator SnapshotDisplay()
    {
        var snapshot = new ScreenSimulator(Profile);
        var bytes = new byte[Profile.BufferSize];
        for (int y = 0; y < Profile.Height; y++)
            for (int x = 0; x < Profile.Width; x++)
                if (PixelIsLit(x, y))
                    bytes[y / 8 * Profile.Width + x] |= (byte)(1 << (y % 8));
        snapshot.LoadPageBytes(bytes);
        return snapshot;
    }
}
