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
