using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PictureFetcher.Core;
using SkiaSharp;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace PictureReader;

public partial class MainWindow : Window
{
    private byte[]? _rgba;
    private int _sourceWidth;
    private int _sourceHeight;
    private MonochromeFrame? _frame;
    private ScreenSimulator? _simulator;
    private Ssd1306CommandSimulator? _ssd1306Controller;
    private Sh1106CommandSimulator? _sh1106Controller;
    private Bitmap? _simulationBitmap;
    private Bitmap? _sourceBitmap;
    private Bitmap? _resultBitmap;

    private static readonly FilePickerFileType ImageFiles = new("Image files")
    {
        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" },
        MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp", "image/gif", "image/webp" }
    };

    public MainWindow()
    {
        InitializeComponent();
        LanguagePicker.SelectedIndex = UiStrings.Culture.Name == "zh-CN" ? 1 : 0;
        LanguagePicker.SelectionChanged += (_, _) =>
        {
            UiStrings.SetCulture(LanguagePicker.SelectedIndex == 1 ? "zh-CN" : "en-US");
            ApplyLanguage();
            Rebuild();
        };
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        ApplyLanguage();
        SizeChanged += (_, _) => AdaptLayout();
        AdaptLayout();
        Rebuild();
        if (Environment.GetEnvironmentVariable("PICTUREFETCHER_UI_SMOKE") == "1")
        {
            // Verify live controls immediately after the XAML tree is initialized.
            // The Opened event is not guaranteed on every headless CI window server.
            try
            {
                RunUiSmoke();
                Console.WriteLine("PASS: Avalonia screen controls, SSD1306/SH1106, exports, zh-CN/en-US");
                Environment.Exit(0);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: Avalonia UI smoke: " + error);
                Environment.Exit(1);
            }
        }
    }

    // Opt-in CI smoke uses the actual compiled desktop executable, not a test-only UI mock.
    // It covers reflection-sensitive XAML, localization resources and controller rendering.
    private void RunUiSmoke()
    {
        _rgba = new byte[] { 0, 0, 0, 255 };
        _sourceWidth = 1;
        _sourceHeight = 1;
        for (int profile = 0; profile < 3; profile++)
        {
            DisplayProfilePicker.SelectedIndex = profile;
            for (int format = 0; format < 3; format++)
            {
                FormatPicker.SelectedIndex = format;
                Rebuild();
                int expected = profile == 1 ? 512 : 1024;
                if (_frame?.ToPageBytes().Length != expected)
                    throw new InvalidOperationException("Wrong packed pixel data size.");
                if (string.IsNullOrWhiteSpace(OutputText.Text) || SimulatorPreview.Source is null)
                    throw new InvalidOperationException("Device renderer or output format failed.");
            }
            ScreenPowerBox.IsChecked = false;
            RefreshSimulation();
            if (_simulator?.PixelIsLit(0, 0) == true)
                throw new InvalidOperationException("Power-off did not blank the simulated display.");
            ScreenPowerBox.IsChecked = true;
            ScreenInvertBox.IsChecked = true;
            RefreshSimulation();
            ScreenInvertBox.IsChecked = false;
            RefreshSimulation();
            if (_simulator?.PixelIsLit(0, 0) != true)
                throw new InvalidOperationException("Device frame was not restored.");
        }
        UiStrings.SetCulture("zh-CN");
        ApplyLanguage();
        if (ProfileLabel.Text != "屏幕")
            throw new InvalidOperationException("zh-CN resources were unavailable.");
        UiStrings.SetCulture("en-US");
        ApplyLanguage();
        if (ProfileLabel.Text != "Screen")
            throw new InvalidOperationException("en-US resources were unavailable.");
    }

    private DisplayProfile SelectedDisplayProfile => DisplayProfilePicker?.SelectedIndex switch
    {
        1 => DisplayProfiles.Ssd1306_128x32,
        2 => DisplayProfiles.Sh1106_128x64,
        _ => DisplayProfiles.Ssd1306_128x64
    };

    private void AdaptLayout()
    {
        // Keep tool controls and preview cards usable on smaller laptop windows.
        bool compact = Bounds.Width < 940;
        LayoutRoot.ColumnDefinitions[0].Width = new GridLength(compact ? 180 : 232);
        BrandCaption.IsVisible = !compact;
        BrandTitle.FontSize = compact ? 14 : 18;
        PageCaption.IsVisible = !compact;
        OriginalCard.Padding = compact ? new Thickness(12) : new Thickness(18);
        OutputCard.Padding = compact ? new Thickness(12) : new Thickness(18);
    }

    private void ApplyLanguage()
    {
        Title = UiStrings.Get("WindowTitle");
        BrandCaption.Text = UiStrings.Get("BrandCaption");
        PageTitle.Text = UiStrings.Get("Workspace");
        PageCaption.Text = UiStrings.Get("WorkspaceDescription");
        OpenButton.Content = UiStrings.Get("SingleConvert");
        BatchButton.Content = UiStrings.Get("BatchConvert");
        SaveButton.Content = UiStrings.Get("SaveFile");
        CopyButton.Content = UiStrings.Get("CopyData");
        RepoButton.Content = UiStrings.Get("SourceCode");
        SettingsHeading.Text = UiStrings.Get("Settings");
        LanguageLabel.Text = UiStrings.Get("Language");
        OriginalHeading.Text = UiStrings.Get("OriginalImage");
        ConvertedHeading.Text = UiStrings.Get("ResizedImage");
        EmptyHint.Text = UiStrings.Get("DropImage");
        DataHeading.Text = UiStrings.Get("ImageArray");
        DataHint.Text = string.Format(UiStrings.Get("ArrayProfileDescription"), SelectedDisplayProfile.BufferSize);
        ThresholdLabel.Text = UiStrings.Get("Threshold");
        InvertBox.Content = UiStrings.Get("Invert");
        OutputText.Watermark = UiStrings.Get("OutputPlaceholder");
        ProfileLabel.Text = UiStrings.Get("DisplayProfile");
        TintLabel.Text = UiStrings.Get("PixelTint");
        ScreenPowerBox.Content = UiStrings.Get("DevicePower");
        ScreenInvertBox.Content = UiStrings.Get("DeviceInvert");
        RawPreviewLabel.Text = UiStrings.Get("RawBitmap");
        CyanOption.Content = UiStrings.Get("TintCyan");
        AmberOption.Content = UiStrings.Get("TintAmber");
        WhiteOption.Content = UiStrings.Get("TintWhite");
        ControllerTxOption.Content = UiStrings.Get("ControllerTx");
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.TryGetFiles()?.Any() == true
            ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>().FirstOrDefault();
        if (file is not null) await TryLoadAsync(file);
    }

    private async void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = UiStrings.Get("SingleConvert"),
            AllowMultiple = false,
            FileTypeFilter = new[] { ImageFiles }
        });
        if (files.Count > 0) await TryLoadAsync(files[0]);
    }

    private async Task TryLoadAsync(IStorageFile file)
    {
        try
        {
            await using var stream = await file.OpenReadAsync();
            using var decoded = SKBitmap.Decode(stream)
                ?? throw new InvalidDataException(UiStrings.Get("InvalidImage"));
            if ((long)decoded.Width * decoded.Height > 80_000_000)
                throw new InvalidDataException(UiStrings.Get("ImageTooLarge"));

            byte[] rgba = CopyRgba(decoded);
            using var skImage = SKImage.FromBitmap(decoded);
            using var png = skImage.Encode(SKEncodedImageFormat.Png, 95);
            using var previewStream = new MemoryStream(png.ToArray());
            var nextSource = new Bitmap(previewStream);
            _rgba = rgba;
            _sourceWidth = decoded.Width;
            _sourceHeight = decoded.Height;
            SourcePreview.Source = nextSource;
            _sourceBitmap?.Dispose();
            _sourceBitmap = nextSource;
            EmptyState.IsVisible = false;
            Rebuild();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                       or InvalidDataException or ArgumentException)
        {
            ShowError(error);
        }
    }

    private static byte[] CopyRgba(SKBitmap bmp)
    {
        byte[] rgba = new byte[checked(bmp.Width * bmp.Height * 4)];
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                SKColor c = bmp.GetPixel(x, y);
                int p = (y * bmp.Width + x) * 4;
                rgba[p] = c.Red; rgba[p + 1] = c.Green;
                rgba[p + 2] = c.Blue; rgba[p + 3] = c.Alpha;
            }
        return rgba;
    }

    private void OnThresholdChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        => Rebuild();

    private void OnSettingsChanged(object? sender, RoutedEventArgs e) => Rebuild();

    private void OnDisplayProfileChanged(object? sender, SelectionChangedEventArgs e) => Rebuild();

    private void OnSimulationSettingChanged(object? sender, RoutedEventArgs e) => RefreshSimulation();

    private void RefreshSimulation()
    {
        if (SimulatorPreview is null || ScreenPowerBox is null ||
            ScreenInvertBox is null || PixelTintPicker is null) return;
        bool on = ScreenPowerBox.IsChecked == true;
        bool inverted = ScreenInvertBox.IsChecked == true;

        // Every interactive power/invert operation travels through the controller's
        // actual command parser, not a bitmap-only post-processing shortcut.
        if (_ssd1306Controller is { } ssd)
        {
            ssd.WriteCommands(new byte[] {
                on ? (byte)0xAF : (byte)0xAE, inverted ? (byte)0xA7 : (byte)0xA6
            });
            _simulator = ssd.SnapshotDisplay();
        }
        else if (_sh1106Controller is { } sh)
        {
            sh.WriteCommands(new byte[] {
                on ? (byte)0xAF : (byte)0xAE, inverted ? (byte)0xA7 : (byte)0xA6
            });
            _simulator = sh.SnapshotDisplay();
        }
        else
        {
            _simulator ??= new ScreenSimulator(SelectedDisplayProfile);
            _simulator.IsPoweredOn = on;
            _simulator.IsInverted = inverted;
        }

        SKColor tint = PixelTintPicker.SelectedIndex switch
        {
            1 => new SKColor(255, 191, 79),
            2 => new SKColor(238, 250, 255),
            _ => new SKColor(89, 225, 250)
        };
        var next = ScreenPreviewRenderer.Render(_simulator, tint);
        SimulatorPreview.Source = next;
        _simulationBitmap?.Dispose();
        _simulationBitmap = next;
    }

    private void Rebuild()
    {
        if (ThresholdValue is null || FormatPicker is null || OutputText is null ||
            DisplayProfilePicker is null || ProfileBadge is null) return;
        var profile = SelectedDisplayProfile;
        ThresholdValue.Text = ((int)ThresholdSlider.Value).ToString();
        ProfileBadge.Text = profile.Name;
        SimulationStatus.Text = $"{profile.BufferSize} B";
        bool isSh1106 = profile.Id.StartsWith("sh1106-", StringComparison.Ordinal);
        DataHint.Text = string.Format(
            UiStrings.Get(isSh1106 ? "Sh1106ArrayDescription" : "ArrayProfileDescription"),
            profile.BufferSize);
        ControllerCommandHint.Text = UiStrings.Get(isSh1106 ? "Sh1106ProtocolHint" : "Ssd1306ProtocolHint");
        _ssd1306Controller = null;
        _sh1106Controller = null;
        _simulator = new ScreenSimulator(profile);
        if (_rgba is null)
        {
            RefreshSimulation();
            return;
        }

        _frame = MonochromeConverter.Convert(_rgba, _sourceWidth, _sourceHeight,
            profile, (int)ThresholdSlider.Value, InvertBox.IsChecked == true);
        // Route display preview through the same command/data paths that firmware uses.
        if (isSh1106)
        {
            _sh1106Controller = new Sh1106CommandSimulator(profile);
            _sh1106Controller.LoadVisibleFrame(_frame);
        }
        else
        {
            _ssd1306Controller = new Ssd1306CommandSimulator(profile);
            _ssd1306Controller.WriteCommands(new byte[] {
                0x20, 0x00, 0x21, 0x00, (byte)(profile.Width - 1),
                0x22, 0x00, (byte)(profile.PageCount - 1)
            });
            _ssd1306Controller.WriteData(_frame.ToPageBytes());
        }
        using var imageStream = new MemoryStream(_frame.ToBmp());
        var next = new Bitmap(imageStream);
        ResultPreview.Source = next;
        _resultBitmap?.Dispose();
        _resultBitmap = next;
        OutputText.Text = FormatPicker.SelectedIndex switch
        {
            1 => _frame.ToCArray(),
            2 => FormatControllerTransfers(_frame),
            _ => _frame.ToHexText()
        };
        RefreshSimulation();
    }

    private static string FormatControllerTransfers(MonochromeFrame frame)
    {
        var sb = new StringBuilder();
        if (frame.Profile.Id.StartsWith("sh1106-", StringComparison.Ordinal))
        {
            foreach (var transfer in Sh1106PageTransferEncoder.Encode(frame))
            {
                sb.Append("CMD: ").AppendLine(ToSpacedHex(transfer.Commands));
                sb.Append("DATA: ").AppendLine(ToSpacedHex(transfer.Data));
            }
        }
        else
        {
            byte[] setup = {
                0x20, 0x00, 0x21, 0x00, (byte)(frame.Profile.Width - 1),
                0x22, 0x00, (byte)(frame.Profile.PageCount - 1)
            };
            sb.Append("CMD: ").AppendLine(ToSpacedHex(setup));
            sb.Append("DATA: ").AppendLine(ToSpacedHex(frame.ToPageBytes()));
        }
        sb.AppendLine("CMD: AF");
        return sb.ToString();
    }

    private static string ToSpacedHex(byte[] data) =>
        string.Join(" ", data.Select(value => value.ToString("X2")));

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (_frame is null) { ShowError(new InvalidOperationException(UiStrings.Get("SelectImageFirst"))); return; }
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = UiStrings.Get("SaveFile"),
                SuggestedFileName = $"oled-{_frame.Profile.Width}x{_frame.Profile.Height}.bmp",
                DefaultExtension = "bmp",
                FileTypeChoices = new[] { new FilePickerFileType("Bitmap") { Patterns = new[] { "*.bmp" } } },
                ShowOverwritePrompt = true
            });
            if (file is null) return;
            await using var output = await file.OpenWriteAsync();
            if (output.CanSeek) output.SetLength(0);
            await output.WriteAsync(_frame.ToBmp());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ShowError(error);
        }
    }

    private async void OnBatchClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = UiStrings.Get("BatchConvert"),
            AllowMultiple = true,
            FileTypeFilter = new[] { ImageFiles }
        });
        int saved = 0;
        foreach (var file in files)
        {
            try
            {
                string? localPath = file.TryGetLocalPath();
                if (localPath is null) continue;
                await using var input = await file.OpenReadAsync();
                using var decoded = SKBitmap.Decode(input)
                    ?? throw new InvalidDataException(UiStrings.Get("InvalidImage"));
                if ((long)decoded.Width * decoded.Height > 80_000_000)
                    throw new InvalidDataException(UiStrings.Get("ImageTooLarge"));
                MonochromeFrame frame = MonochromeConverter.Convert(CopyRgba(decoded), decoded.Width, decoded.Height,
                    SelectedDisplayProfile, (int)ThresholdSlider.Value, InvertBox.IsChecked == true);
                // Never overwrite a source BMP or existing export.
                string folder = Path.GetDirectoryName(localPath)!;
                string stem = Path.GetFileNameWithoutExtension(localPath);
                string candidate = Path.Combine(folder, stem + "-oled.bmp");
                int suffix = 2;
                while (File.Exists(candidate))
                    candidate = Path.Combine(folder, stem + "-oled-" + suffix++ + ".bmp");
                await File.WriteAllBytesAsync(candidate, frame.ToBmp());
                saved++;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                           or InvalidDataException or ArgumentException)
            {
                ShowError(error);
            }
        }
        if (saved > 0)
            DataHint.Text = string.Format(UiStrings.Get("BatchCompletedCount"), saved);
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(OutputText.Text) && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(OutputText.Text);
    }

    private void OnRepoClicked(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/bestyize/PictureFetcher")
        { UseShellExecute = true });
    }

    private void ShowError(Exception error)
    {
        DataHint.Text = UiStrings.Get("Error") + ": " + error.Message;
    }

    protected override void OnClosed(EventArgs e)
    {
        _sourceBitmap?.Dispose();
        _resultBitmap?.Dispose();
        _simulationBitmap?.Dispose();
        base.OnClosed(e);
    }
}
