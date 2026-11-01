using Microsoft.Win32;

namespace MemMop.Core;

/// <summary>
/// User settings, stored under HKCU so the program stays a single file with nothing beside it.
/// </summary>
public static class Settings
{
    private const string Key = @"SOFTWARE\MemMop";

    /// <summary>Gets or sets the UI language ("zh-Hans" or "en").</summary>
    public static string Language { get; set; } = "zh-Hans";

    /// <summary>Gets or sets the font size index: 0 small, 1 normal, 2 large.</summary>
    public static int FontSizeIndex { get; set; } = 1;

    /// <summary>Gets or sets a value indicating whether closing the window minimises it to the tray.</summary>
    public static bool CloseToTray { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the app starts hidden in the tray.</summary>
    public static bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether automatic cleaning stays on the conservative subset.
    /// </summary>
    public static bool AutoCleanSafeOnly { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the tray hint has been shown.</summary>
    public static bool ShownTrayHint { get; set; }

    /// <summary>Gets or sets the automatic clean interval in minutes; 0 disables it.</summary>
    public static int AutoCleanMinutes { get; set; }

    /// <summary>Gets or sets the low-memory threshold in percent; 0 disables it.</summary>
    public static int LowMemoryPercent { get; set; }

    /// <summary>
    /// Reads the stored values, keeping the defaults for anything missing or unreadable.
    /// </summary>
    public static void Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Key);

            if (key == null)
                return;

            Language = key.GetValue(nameof(Language), Language) as string ?? Language;
            FontSizeIndex = ReadInt(key, nameof(FontSizeIndex), FontSizeIndex);
            CloseToTray = ReadInt(key, nameof(CloseToTray), CloseToTray ? 1 : 0) != 0;
            StartMinimized = ReadInt(key, nameof(StartMinimized), StartMinimized ? 1 : 0) != 0;
            AutoCleanSafeOnly = ReadInt(key, nameof(AutoCleanSafeOnly), AutoCleanSafeOnly ? 1 : 0) != 0;
            ShownTrayHint = ReadInt(key, nameof(ShownTrayHint), ShownTrayHint ? 1 : 0) != 0;
            AutoCleanMinutes = ReadInt(key, nameof(AutoCleanMinutes), AutoCleanMinutes);
            LowMemoryPercent = ReadInt(key, nameof(LowMemoryPercent), LowMemoryPercent);
        }
        catch (Exception e)
        {
            // a broken settings key must never stop the program from starting
            System.Diagnostics.Debug.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Writes the current values.
    /// </summary>
    public static void Save()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(Key);

            if (key == null)
                return;

            key.SetValue(nameof(Language), Language, RegistryValueKind.String);
            key.SetValue(nameof(FontSizeIndex), FontSizeIndex, RegistryValueKind.DWord);
            key.SetValue(nameof(CloseToTray), CloseToTray ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(StartMinimized), StartMinimized ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(AutoCleanSafeOnly), AutoCleanSafeOnly ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(ShownTrayHint), ShownTrayHint ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(AutoCleanMinutes), AutoCleanMinutes, RegistryValueKind.DWord);
            key.SetValue(nameof(LowMemoryPercent), LowMemoryPercent, RegistryValueKind.DWord);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }
    }

    private static int ReadInt(RegistryKey key, string name, int fallback)
    {
        return key.GetValue(name) is int value ? value : fallback;
    }
}
