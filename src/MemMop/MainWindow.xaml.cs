using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MemMop.Core;

namespace MemMop;

/// <summary>
/// Main window. Reads memory once a second; the cleaner does not exist yet.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Closed += (_, _) => _timer.Stop();
    }

    private static string Size(long bytes)
    {
        var gb = bytes / (1024d * 1024 * 1024);

        return string.Format(CultureInfo.InvariantCulture, "{0:0.#} GB", gb);
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

        PhysicalText.Text = $"物理内存 {Size(snapshot.Total - snapshot.Available)} / {Size(snapshot.Total)}";

        SegmentText.Text = snapshot.HasSegments
            ? $"活动 {Size(snapshot.Active)} · 已修改 {Size(snapshot.Modified)} · 待机 {Size(snapshot.Standby)} · 空闲 {Size(snapshot.Free)}"
            : "四段构成：当前系统版本无法读取";

        CommitText.Text = snapshot.CommitLimit > 0
            ? $"已提交 {Size(snapshot.CommitUsed)} / {Size(snapshot.CommitLimit)}"
            : "已提交：不可用";
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
