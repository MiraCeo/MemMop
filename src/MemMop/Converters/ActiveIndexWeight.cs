using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MemMop.Converters;

/// <summary>
/// Gives the label of the stop a stepped slider is sitting on a heavier weight, so the current choice reads
/// before the alternatives do. The parameter is the stop's index as a string.
/// </summary>
public sealed class ActiveIndexWeight : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var active = value is int index
            && parameter is string text
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected)
            && index == expected;

        return active ? FontWeights.SemiBold : FontWeights.Normal;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
