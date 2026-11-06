namespace MemMop.Core;

/// <summary>
/// The pages, in the order the tab strip shows them. The numbers are the TabControl's indices, so this enum
/// and the markup have to agree.
/// </summary>
/// <remarks>
/// This exists because the tray menu used to pass a bare <c>1</c> for settings. When the statistics page was
/// inserted ahead of settings, that number silently started opening the wrong page - a bug with no compiler
/// error and no crash, just a menu entry that did the wrong thing.
/// </remarks>
public enum AppPage
{
    /// <summary>The home page.</summary>
    Home = 0,

    /// <summary>The statistics page.</summary>
    Statistics = 1,

    /// <summary>The settings page.</summary>
    Settings = 2,

    /// <summary>The whitelist page.</summary>
    Whitelist = 3,

    /// <summary>The about page.</summary>
    About = 4,
}
