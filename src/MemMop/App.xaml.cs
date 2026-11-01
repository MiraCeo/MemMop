using MemMop.Core;

namespace MemMop;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <inheritdoc/>
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        Settings.Load();

        base.OnStartup(e);
    }

    /// <inheritdoc/>
    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Settings.Save();

        base.OnExit(e);
    }
}
