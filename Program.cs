using System;
using Avalonia;

namespace PictureReader;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) =>
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
