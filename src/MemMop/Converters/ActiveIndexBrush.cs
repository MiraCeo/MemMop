using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MemMop.Converters;

/// <summary>
/// Paints the label of the stop a stepped slider is sitting on in the accent colour, and the rest muted.
/// The parameter is the stop's index as a string, so one converter serves every label.
/// </summary>
public sealed class ActiveIndexBrush : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var active = value is int index
            && parameter is string text
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected)
            && index == expected;

        return System.Windows.Application.Current?.TryFindResource(active ? "Accent" : "TextMuted") as System.Windows.Media.Brush
            ?? System.Windows.Media.Brushes.Gray;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
