namespace PictureFetcher.Core;

/// <summary>A visible monochrome display area, with rows grouped into 8-pixel pages.</summary>
public sealed class DisplayProfile
{
    public string Id { get; }
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public int PageCount => Height / 8;
    public int BufferSize => checked(Width * PageCount);

    public DisplayProfile(string id, string name, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A profile ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A profile name is required.", nameof(name));
        if (width is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 8 or > 1024 || height % 8 != 0)
            throw new ArgumentOutOfRangeException(nameof(height), "The display height must be divisible by 8.");
        Id = id;
        Name = name;
        Width = width;
        Height = height;
    }
}

public static class DisplayProfiles
{
    public static readonly DisplayProfile Ssd1306_128x64 = new("ssd1306-128x64", "SSD1306 · 128 × 64", 128, 64);
    public static readonly DisplayProfile Ssd1306_128x32 = new("ssd1306-128x32", "SSD1306 · 128 × 32", 128, 32);

    public static IReadOnlyList<DisplayProfile> BuiltIn { get; } =
        Array.AsReadOnly(new[] { Ssd1306_128x64, Ssd1306_128x32 });
}
