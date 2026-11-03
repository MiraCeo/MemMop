using System.Drawing;
using System.Windows.Forms;
using MemMop.Core;

namespace MemMop.Services;

/// <summary>
/// The only file that knows about WinForms. Everything the rest of the program needs is on
/// <see cref="ITrayService"/>.
/// </summary>
public sealed class TrayService : ITrayService
{
    private NotifyIcon? _icon;
    private ToolStripMenuItem? _cleanItem;
    private ToolStripMenuItem? _statusMain;
    private ToolStripMenuItem? _statusLast;

    /// <inheritdoc/>
    public event Action? ShowRequested;

    /// <inheritdoc/>
    public event Action<int>? ViewRequested;

    /// <inheritdoc/>
    public event Action? CleanRequested;

    /// <inheritdoc/>
    public event Action? ExitRequested;

    /// <inheritdoc/>
    public void Initialize()
    {
        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "MemMop",
            Visible = true,
        };

        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowRequested?.Invoke();
        };

        var menu = new ContextMenuStrip();

        // two disabled rows: the figures live here so they are readable without opening the window
        _statusMain = new ToolStripMenuItem(string.Empty) { Enabled = false };
        _statusLast = new ToolStripMenuItem(string.Empty) { Enabled = false };

        menu.Items.Add(_statusMain);
        menu.Items.Add(_statusLast);
        menu.Items.Add(new ToolStripSeparator());

        var home = new ToolStripMenuItem(Strings.Current.Home, null, (_, _) => ViewRequested?.Invoke(0));
        var settings = new ToolStripMenuItem(Strings.Current.TraySettings, null, (_, _) => ViewRequested?.Invoke(1));

        _cleanItem = new ToolStripMenuItem(Strings.Current.Clean, null, (_, _) => CleanRequested?.Invoke());
        var exit = new ToolStripMenuItem(Strings.Current.TrayExit, null, (_, _) => ExitRequested?.Invoke());

        menu.Items.Add(home);
        menu.Items.Add(settings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_cleanItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        _icon.ContextMenuStrip = menu;
    }

    /// <inheritdoc/>
    public void SetStatus(string main, string last)
    {
        if (_statusMain != null)
            _statusMain.Text = main;

        if (_statusLast != null)
            _statusLast.Text = last;
    }

    /// <inheritdoc/>
    public void UpdateTooltip(string text)
    {
        if (_icon == null)
            return;

        // the shell caps tooltips; keep it short rather than let it truncate mid-number
        _icon.Text = text.Length > 120 ? text.Substring(0, 120) : text;
    }

    /// <inheritdoc/>
    public void Notify(string? message)
    {
        if (!string.IsNullOrEmpty(message))
            _icon?.ShowBalloonTip(5000, null, message, ToolTipIcon.None);
    }

    /// <summary>
    /// Enables or disables the clean entry while a run is in progress.
    /// </summary>
    /// <param name="enabled">Whether cleaning is available.</param>
    public void SetCleanEnabled(bool enabled)
    {
        if (_cleanItem != null)
            _cleanItem.Enabled = enabled;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_icon == null)
            return;

        _icon.Visible = false;
        _icon.Dispose();
        _icon = null;
    }

    /// <summary>
    /// Loads the program's own icon at the size the tray asks for. The generic Windows icon is the fallback
    /// rather than the default: a program with no face of its own is hard to find in a tray full of them.
    /// </summary>
    private static Icon LoadIcon()
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/MemMop;component/Assets/MemMop.ico"));

            if (resource == null)
                return SystemIcons.Application;

            using var stream = resource.Stream;
            using var source = new Icon(stream);

            return new Icon(source, SystemInformation.SmallIconSize);
        }
        catch (Exception)
        {
            return SystemIcons.Application;
        }
    }
}
