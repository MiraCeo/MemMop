using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MemMop.Controls;

/// <summary>
/// A horizontal bar split into proportional coloured segments, used for the memory composition and for the
/// committed usage. Segment values and colours are comma-separated strings so the control needs no collection
/// dependency properties.
/// </summary>
public sealed class ProportionBar : FrameworkElement
{
    /// <summary>Identifies the <see cref="Values"/> property.</summary>
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(string), typeof(ProportionBar),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Palette"/> property.</summary>
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(string), typeof(ProportionBar),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the segment sizes, comma-separated, in any unit.</summary>
    public string Values
    {
        get => (string)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Gets or sets the segment colours, comma-separated, one per value.</summary>
    public string Palette
    {
        get => (string)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);

        var values = Parse(Values);
        var colours = string.IsNullOrWhiteSpace(Palette) ? Array.Empty<string>() : Palette.Split(',').Select(part => part.Trim()).ToArray();

        if (values.Length == 0 || colours.Length == 0)
            return;

        var total = values.Sum();

        if (total <= 0)
            return;

        var width = ActualWidth;
        var height = ActualHeight;
        var radius = Math.Min(3, height / 2);
        var x = 0d;
        var last = Math.Min(values.Length, colours.Length);

        for (var i = 0; i < last; i++)
        {
            var segment = i == last - 1 ? width - x : width * values[i] / total;

            if (segment <= 0)
                continue;

            var colour = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colours[i]);
            var brush = new SolidColorBrush(colour);

            brush.Freeze();

            // round only the outermost corners so the bar reads as one continuous strip
            var geometry = new RectangleGeometry(new Rect(x, 0, segment, height), radius, radius);

            context.DrawGeometry(brush, null, geometry);
            x += segment;
        }
    }

    private static double[] Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<double>();

        return text.Split(',')
            .Select(part => double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0d)
            .ToArray();
    }
}
