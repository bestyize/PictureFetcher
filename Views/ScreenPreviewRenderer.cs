using System;
using System.IO;
using Avalonia.Media.Imaging;
using PictureFetcher.Core;
using SkiaSharp;

namespace PictureReader;

/// <summary>Renders the virtual SSD1306 RAM into a pixel-grid screen with a device bezel.</summary>
internal static class ScreenPreviewRenderer
{
    internal static Bitmap Render(ScreenSimulator simulator, SKColor activeColor)
    {
        ArgumentNullException.ThrowIfNull(simulator);
        const int pitch = 6, side = 39, top = 47, bottom = 49;
        int width = checked(simulator.Profile.Width * pitch + side * 2);
        int height = checked(simulator.Profile.Height * pitch + top + bottom);
        using var picture = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(picture))
        using (var casing = new SKPaint { IsAntialias = true, Color = new SKColor(28, 36, 53) })
        using (var well = new SKPaint { IsAntialias = true, Color = new SKColor(4, 17, 25) })
        using (var inactive = new SKPaint { IsAntialias = true, Color = new SKColor(14, 38, 49) })
        using (var lit = new SKPaint { IsAntialias = true, Color = activeColor })
        using (var outline = new SKPaint { IsAntialias = true, Color = new SKColor(62, 77, 94), Style = SKPaintStyle.Stroke, StrokeWidth = 2 })
        {
            canvas.Clear(new SKColor(242, 245, 249));
            canvas.DrawRoundRect(new SKRect(4, 4, width - 4, height - 4), 25, 25, casing);
            canvas.DrawRoundRect(new SKRect(side - 9, top - 10,
                width - side + 9, height - bottom + 10), 12, 12, well);
            canvas.DrawRoundRect(new SKRect(side - 9, top - 10,
                width - side + 9, height - bottom + 10), 12, 12, outline);

            for (int y = 0; y < simulator.Profile.Height; y++)
                for (int x = 0; x < simulator.Profile.Width; x++)
                {
                    float left = side + x * pitch + 0.7f;
                    float upper = top + y * pitch + 0.7f;
                    canvas.DrawRoundRect(new SKRect(left, upper, left + pitch - 1.4f,
                        upper + pitch - 1.4f), 0.9f, 0.9f,
                        simulator.PixelIsLit(x, y) ? lit : inactive);
                }
            canvas.Flush();
        }
        using var image = SKImage.FromBitmap(picture);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 95)
            ?? throw new InvalidOperationException("Could not encode screen preview.");
        using var stream = new MemoryStream(encoded.ToArray());
        return new Bitmap(stream);
    }
}
