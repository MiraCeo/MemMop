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

    /// <summary>Gets the menu, so the screenshot tool can render it.</summary>
    public ContextMenuStrip? Menu => _icon?.ContextMenuStrip;

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

        var colors = TrayColors.FromTheme();
        var menu = new ContextMenuStrip
        {
            BackColor = colors.Background,
            ForeColor = colors.Text,
            Font = new Font("Microsoft YaHei UI", 9f),
            ShowImageMargin = false,
            Padding = new Padding(0, 4, 0, 4),
            Renderer = new TrayMenuRenderer(colors),
        };

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

    /// <summary>
    /// The colours the tray menu is painted with, read from the same theme dictionaries the window uses.
    /// </summary>
    private readonly struct TrayColors
    {
        public TrayColors(Color background, Color hover, Color border, Color text, Color muted)
        {
            Background = background;
            Hover = hover;
            Border = border;
            Text = text;
            Muted = muted;
        }

        public Color Background { get; }

        public Color Hover { get; }

        public Color Border { get; }

        public Color Text { get; }

        public Color Muted { get; }

        public static TrayColors FromTheme() => new(
            Read("Nav", Color.FromArgb(0x1F, 0x17, 0x10)),
            Read("Hover", Color.FromArgb(0x2B, 0x20, 0x18)),
            Read("Border", Color.FromArgb(0x3A, 0x2A, 0x1E)),
            Read("TextPrimary", Color.FromArgb(0xF3, 0xE7, 0xD8)),
            Read("TextMuted", Color.FromArgb(0xA8, 0x84, 0x6A)));

        private static Color Read(string key, Color fallback)
        {
            if (System.Windows.Application.Current?.TryFindResource(key) is System.Windows.Media.SolidColorBrush brush)
                return Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);

            return fallback;
        }
    }

    /// <summary>
    /// Paints the tray menu in the program's own colours. A WinForms menu would otherwise come up in the
    /// system style, which is the one surface in the program that would not match the window beside it.
    /// </summary>
    private sealed class TrayMenuRenderer : ToolStripRenderer
    {
        private readonly TrayColors _colors;

        public TrayMenuRenderer(TrayColors colors) => _colors = colors;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(_colors.Background);

            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(_colors.Border);

            e.Graphics.DrawRectangle(
                pen,
                new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
                return;

            using var brush = new SolidBrush(_colors.Hover);

            e.Graphics.FillRectangle(brush, new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? _colors.Text : _colors.Muted;

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(_colors.Border);
            var middle = e.Item.Height / 2;

            e.Graphics.DrawLine(pen, 8, middle, e.Item.Width - 8, middle);
        }
    }
}
