using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MemMop.Converters;

/// <summary>
/// Maps a bool to a visibility. WPF ships one for some controls but not as a reusable resource.
/// </summary>
public sealed class BoolToVisibility : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>
/// Maps a bool to a visibility, inverted - used to hide the tabbed view while the compact one is shown.
/// </summary>
public sealed class InverseBoolToVisibility : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not Visibility.Visible;
}
