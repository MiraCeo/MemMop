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

    /// <summary>Raised after each refresh with the text for the tray tooltip.</summary>
    public event Action<string>? MemoryRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

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

        MemoryRead?.Invoke(_viewModel.PhysicalText);

        _viewModel.SegmentText = snapshot.HasSegments
            ? text.Segments(snapshot.Active, snapshot.Modified, snapshot.Standby, snapshot.Free)
            : text.SegmentsUnavailable;

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
        if (FindName("Tabs") is System.Windows.Controls.TabControl tabs)
            tabs.SelectedIndex = index;
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
