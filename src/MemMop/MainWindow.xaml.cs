using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private int _ticks;

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

        // the candidate list floats over the page, so a click anywhere else has to dismiss it
        PreviewMouseDown += OnWindowPreviewMouseDown;
    }

    private void OnSearchBoxGotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.IsPickerOpen = true;
    }

    private void OnPickerRowClicked(object sender, MouseButtonEventArgs e)
    {
        // a row click fills the box rather than adding: the plus is the only thing that adds
        if (PickerList.SelectedItem is string name)
            _viewModel.ProcessSearchText = name;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_viewModel.IsPickerOpen)
            return;

        if (IsWithin(e.OriginalSource, SearchBox) || IsWithin(e.OriginalSource, PickerList))
            return;

        _viewModel.IsPickerOpen = false;
    }

    private static bool IsWithin(object? source, DependencyObject ancestor)
    {
        var current = source as DependencyObject;

        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;

            current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return false;
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
        _viewModel.RefreshProcessUsage();
        _viewModel.RefreshStatistics();

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    private void OnExportHistoryClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "MemMop-记录-" + DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) + ".csv",
            Filter = "CSV (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = ".csv",
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            System.IO.File.WriteAllText(dialog.FileName, _viewModel.BuildHistoryCsv(), new System.Text.UTF8Encoding(true));
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(this, "导出失败：" + exception.Message, "MemMop", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // regedit opens on the key without needing elevation, because the key is under the current user
            System.Diagnostics.Process.Start("regedit.exe", "/n,HKEY_CURRENT_USER\\SOFTWARE\\MemMop");
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(this, "打不开注册表编辑器：" + exception.Message, "MemMop", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Refresh()
    {
        var snapshot = MemorySnapshot.Capture();
        var text = Strings.Current;

        // the chart reads the counters every two seconds; the process table costs far more to build, so it
        // runs on a slower beat
        _ticks++;

        if (_ticks % 2 == 0)
            _viewModel.SampleMemory(snapshot);

        if (_ticks % 5 == 0)
            _viewModel.RefreshProcessUsage();

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
        _viewModel.AvailableText = "可用 " + Size(snapshot.Available) + " / " + Size(snapshot.Total);
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
            Height = 212;
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
