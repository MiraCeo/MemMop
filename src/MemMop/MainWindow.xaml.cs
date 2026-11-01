using System.Windows;
using System.Windows.Controls;
using MemMop.Core;

namespace MemMop;

/// <summary>
/// Main window. Two settings are bound so the store can be exercised end to end before any cleaner exists.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
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
