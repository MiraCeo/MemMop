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

    /// <summary>Gets or sets a value indicating whether the window shows the compact view.</summary>
    public static bool CompactMode { get; set; }

    /// <summary>Gets or sets a value indicating whether the window stays above others.</summary>
    public static bool AlwaysOnTop { get; set; }

    /// <summary>Gets or sets a value indicating whether the clean hotkey is registered.</summary>
    public static bool HotkeyEnabled { get; set; }

    /// <summary>Gets or sets the hotkey modifiers: MOD_CONTROL | MOD_ALT.</summary>
    public static int HotkeyModifiers { get; set; } = 0x0003;

    /// <summary>Gets or sets the hotkey virtual-key code (defaults to M).</summary>
    public static int HotkeyKey { get; set; } = 0x4D;

    /// <summary>Gets or sets a value indicating whether the program starts at logon.</summary>
    public static bool RunOnStartup { get; set; }

    /// <summary>Gets or sets a value indicating whether cleaning raises a tray notification.</summary>
    public static bool ShowNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the committed line is shown.</summary>
    public static bool ShowCommitted { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the four-part breakdown is shown.</summary>
    public static bool DetailedMemory { get; set; } = true;

    /// <summary>
    /// Gets the process names excluded from working-set trimming. Stored verbatim: Windows reports names
    /// without an extension, keeping their case, and names may contain spaces.
    /// </summary>
    public static HashSet<string> ExcludedProcesses { get; } = new(StringComparer.OrdinalIgnoreCase);

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
            RunOnStartup = ReadInt(key, nameof(RunOnStartup), RunOnStartup ? 1 : 0) != 0;
            CompactMode = ReadInt(key, nameof(CompactMode), CompactMode ? 1 : 0) != 0;
            AlwaysOnTop = ReadInt(key, nameof(AlwaysOnTop), AlwaysOnTop ? 1 : 0) != 0;
            HotkeyEnabled = ReadInt(key, nameof(HotkeyEnabled), HotkeyEnabled ? 1 : 0) != 0;
            HotkeyModifiers = ReadInt(key, nameof(HotkeyModifiers), HotkeyModifiers);
            HotkeyKey = ReadInt(key, nameof(HotkeyKey), HotkeyKey);
            ShowNotifications = ReadInt(key, nameof(ShowNotifications), ShowNotifications ? 1 : 0) != 0;
            ShowCommitted = ReadInt(key, nameof(ShowCommitted), ShowCommitted ? 1 : 0) != 0;
            DetailedMemory = ReadInt(key, nameof(DetailedMemory), DetailedMemory ? 1 : 0) != 0;

            foreach (var name in (key.GetValue(nameof(ExcludedProcesses)) as string ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                ExcludedProcesses.Add(name);
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
            key.SetValue(nameof(RunOnStartup), RunOnStartup ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(CompactMode), CompactMode ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(AlwaysOnTop), AlwaysOnTop ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(HotkeyEnabled), HotkeyEnabled ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(HotkeyModifiers), HotkeyModifiers, RegistryValueKind.DWord);
            key.SetValue(nameof(HotkeyKey), HotkeyKey, RegistryValueKind.DWord);
            key.SetValue(nameof(ShowNotifications), ShowNotifications ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(ShowCommitted), ShowCommitted ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(DetailedMemory), DetailedMemory ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue(nameof(ExcludedProcesses), string.Join("\n", ExcludedProcesses), RegistryValueKind.String);
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
