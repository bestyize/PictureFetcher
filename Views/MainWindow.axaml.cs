using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private OledFrame? _frame;
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
        };
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        ApplyLanguage();
        SizeChanged += (_, _) => AdaptLayout();
        AdaptLayout();
    }

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
        DataHint.Text = UiStrings.Get("ArrayDescription");
        ThresholdLabel.Text = UiStrings.Get("Threshold");
        InvertBox.Content = UiStrings.Get("Invert");
        OutputText.Watermark = UiStrings.Get("OutputPlaceholder");
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

    private void Rebuild()
    {
        if (ThresholdValue is null || FormatPicker is null || OutputText is null) return;
        ThresholdValue.Text = ((int)ThresholdSlider.Value).ToString();
        if (_rgba is null) return;
        _frame = OledConverter.Convert(_rgba, _sourceWidth, _sourceHeight,
            (int)ThresholdSlider.Value, InvertBox.IsChecked == true);
        using var imageStream = new MemoryStream(_frame.ToBmp());
        var next = new Bitmap(imageStream);
        ResultPreview.Source = next;
        _resultBitmap?.Dispose();
        _resultBitmap = next;
        OutputText.Text = FormatPicker.SelectedIndex == 1 ? _frame.ToCArray() : _frame.ToHexText();
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (_frame is null) { ShowError(new InvalidOperationException(UiStrings.Get("SelectImageFirst"))); return; }
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = UiStrings.Get("SaveFile"),
                SuggestedFileName = "oled-128x64.bmp",
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
                OledFrame frame = OledConverter.Convert(CopyRgba(decoded), decoded.Width, decoded.Height,
                    (int)ThresholdSlider.Value, InvertBox.IsChecked == true);
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
        base.OnClosed(e);
    }
}
