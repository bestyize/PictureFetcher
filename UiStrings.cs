using System;
using System.Globalization;
using System.IO;
using System.Resources;

namespace PictureReader
{
    internal static class UiStrings
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("PictureReader.UiStrings", typeof(UiStrings).Assembly);

        private static readonly string LanguageFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PictureFetcher", "language.txt");

        internal static CultureInfo Culture { get; private set; } = LoadCulture();

        internal static string Get(string key)
        {
            return ResourceManager.GetString(key, Culture) ?? key;
        }

        internal static void SetCulture(string language)
        {
            Culture = CultureInfo.GetCultureInfo(language == "zh-CN" ? "zh-CN" : "en-US");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LanguageFile)!);
                File.WriteAllText(LanguageFile, Culture.Name);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static CultureInfo LoadCulture()
        {
            try
            {
                if (File.Exists(LanguageFile))
                {
                    string saved = File.ReadAllText(LanguageFile).Trim();
                    if (saved == "en-US" || saved == "zh-CN")
                        return CultureInfo.GetCultureInfo(saved);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return CultureInfo.GetCultureInfo(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh"
                    ? "zh-CN"
                    : "en-US");
        }
    }
}
