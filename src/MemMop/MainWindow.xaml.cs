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

        _viewModel.PhysicalText = $"物理内存 {Size(snapshot.Total - snapshot.Available)} / {Size(snapshot.Total)}";

        _viewModel.SegmentText = snapshot.HasSegments
            ? $"活动 {Size(snapshot.Active)} · 已修改 {Size(snapshot.Modified)} · 待机 {Size(snapshot.Standby)} · 空闲 {Size(snapshot.Free)}"
            : "四段构成：当前系统版本无法读取";
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            Settings.Language = tag;
    }

    private void OnCloseToTrayChanged(object sender, RoutedEventArgs e)
    {
        Settings.CloseToTray = CloseToTrayBox.IsChecked == true;
    }
}
