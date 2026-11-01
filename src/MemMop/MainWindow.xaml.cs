using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MemMop.Core;
using MemMop.ViewModel;

namespace MemMop;

/// <summary>
/// Main window. Memory is read once a second; the cleaner runs on demand.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly MainViewModel _viewModel = new();

    /// <summary>Sets the compact state before the window is shown (used by the screenshot harness).</summary>
    public bool CompactMode
    {
        set => _viewModel.CompactMode = value;
    }
    private readonly MemMop.Services.HotkeyService _hotkey = new();

    /// <summary>Raised after each refresh with the text for the tray tooltip.</summary>
    public event Action<string>? MemoryRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        _hotkey.Pressed += () =>
        {
            if (_viewModel.CleanCommand.CanExecute(null))
                _viewModel.CleanCommand.Execute(null);
        };

        Loaded += (_, _) =>
        {
            _viewModel.HotkeyChanged += ApplyHotkey;
            ApplyHotkey();
        };

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CompactMode))
                ApplyWindowSize();
        };

        Loaded += (_, _) => ApplyWindowSize();

        Loaded += OnLoaded;
        Closed += (_, _) => _timer.Stop();
    }

    private static string Size(long bytes)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} GB", bytes / (1024d * 1024 * 1024));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        foreach (ComboBoxItem item in LanguageBox.Items)
        {
            if ((item.Tag as string) == Settings.Language)
            {
                LanguageBox.SelectedItem = item;
                break;
            }
        }

        if (LanguageBox.SelectedItem == null)
            LanguageBox.SelectedIndex = 0;

        CloseToTrayBox.IsChecked = Settings.CloseToTray;

        Refresh();

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    private void Refresh()
    {
        var snapshot = MemorySnapshot.Capture();
        var text = Strings.Current;

        _viewModel.PhysicalText = text.Physical(snapshot.Total - snapshot.Available, snapshot.Total);

        var tooltip = _viewModel.ShowCommitted && snapshot.CommitLimit > 0
            ? _viewModel.PhysicalText + Environment.NewLine + text.Commit(snapshot.CommitUsed, snapshot.CommitLimit)
            : _viewModel.PhysicalText;

        MemoryRead?.Invoke(tooltip);

        _viewModel.SegmentText = snapshot.HasSegments
            ? text.Segments(snapshot.Active, snapshot.Modified, snapshot.Standby, snapshot.Free)
            : text.SegmentsUnavailable;

        _viewModel.PhysicalSegments = snapshot.HasSegments
            ? string.Join(",", snapshot.Active, snapshot.Modified, snapshot.Standby, snapshot.Free)
            : string.Empty;

        _viewModel.CommitSegments = snapshot.CommitLimit > 0
            ? snapshot.CommitUsed + "," + (snapshot.CommitLimit - snapshot.CommitUsed)
            : string.Empty;

        var usedPercent = snapshot.Total > 0 ? (snapshot.Total - snapshot.Available) * 100.0 / snapshot.Total : 0;

        _viewModel.UsedPercentText = usedPercent.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        _viewModel.UsedText = Size(snapshot.Total - snapshot.Available) + " / " + Size(snapshot.Total);
        _viewModel.ActiveText = "活动 " + Size(snapshot.Active);
        _viewModel.ModifiedText = "已修改 " + Size(snapshot.Modified);
        _viewModel.StandbyText = "待机 " + Size(snapshot.Standby);
        _viewModel.FreeText = "空闲 " + Size(snapshot.Free);

        _viewModel.CommitText = snapshot.CommitLimit > 0
            ? text.Commit(snapshot.CommitUsed, snapshot.CommitLimit)
            : string.Empty;
    }

    /// <summary>
    /// Brings a tab to the front.
    /// </summary>
    /// <param name="index">0 home, 1 settings, 2 about.</param>
    public void ShowTab(int index)
    {
        // the generated field, not FindName: the name lives in the window namescope and a lookup there is a
        // silent no-op when it misses
        Tabs.SelectedIndex = index;
    }

    private void OnTitleBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // a frameless window has to move itself
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void ApplyWindowSize()
    {
        // the compact view is a fixed-size card, the tabbed one a fixed-size window: neither should be resizable
        // by accident, so both are set explicitly
        if (_viewModel.CompactMode)
        {
            Width = 320;
            Height = 190;
        }
        else
        {
            Width = 540;
            Height = 640;
        }
    }

    private void ApplyHotkey()
    {
        _hotkey.Unregister();

        if (!_viewModel.HotkeyEnabled)
            return;

        if (!_hotkey.Register(this, (uint)_viewModel.HotkeyModifiers, (uint)_viewModel.HotkeyKey, out var error))
            _viewModel.ReportHotkeyFailure("清理热键注册失败：" + error);
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            Settings.Language = tag;
            Settings.Save();

            Strings.Current.SetLanguage(tag == "en");
        }
    }

    private void OnCloseToTrayChanged(object sender, RoutedEventArgs e)
    {
        Settings.CloseToTray = CloseToTrayBox.IsChecked == true;
    }
}
