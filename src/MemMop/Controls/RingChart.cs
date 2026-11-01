using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;
using Pen = System.Windows.Media.Pen;
using Size = System.Windows.Size;

namespace MemMop.Controls;

/// <summary>
/// A donut showing how physical memory is made up: active, modified, standby and free.
/// </summary>
public sealed class RingChart : FrameworkElement
{
    /// <summary>Identifies the <see cref="Values"/> property.</summary>
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(string), typeof(RingChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Palette"/> property.</summary>
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(string), typeof(RingChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="RingThickness"/> property.</summary>
    public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
        nameof(RingThickness), typeof(double), typeof(RingChart),
        new FrameworkPropertyMetadata(16d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the segment sizes, comma-separated.</summary>
    public string Values
    {
        get => (string)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Gets or sets the segment colours, comma-separated.</summary>
    public string Palette
    {
        get => (string)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>Gets or sets the ring thickness in pixels.</summary>
    public double RingThickness
    {
        get => (double)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);

        var values = Numbers(Values);
        var colours = Colours(Palette);

        if (values.Length == 0 || colours.Length == 0)
            return;

        var total = values.Sum();

        if (total <= 0)
            return;

        var size = Math.Min(ActualWidth, ActualHeight);
        var radius = (size - RingThickness) / 2;
        var centre = new Point(ActualWidth / 2, ActualHeight / 2);

        // the gap keeps neighbouring segments readable without inventing proportions
        const double gap = 1.6;

        // twelve o'clock, counterclockwise: the angle grows negatively
        var angle = -90d;
        var count = Math.Min(values.Length, colours.Length);

        for (var i = 0; i < count; i++)
        {
            var sweep = 360d * values[i] / total;

            if (sweep <= gap)
            {
                angle -= sweep;

                continue;
            }

            var start = angle - gap / 2;
            var end = angle - sweep + gap / 2;
            var colour = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colours[i]);
            var brush = new SolidColorBrush(colour);

            brush.Freeze();

            var figure = new PathFigure { StartPoint = PointAt(centre, radius, start) };
            var arc = new ArcSegment(PointAt(centre, radius, end), new Size(radius, radius), 0, sweep > 180, SweepDirection.Counterclockwise, true);

            figure.Segments.Add(arc);

            var geometry = new PathGeometry();

            geometry.Figures.Add(figure);

            context.DrawGeometry(null, new Pen(brush, RingThickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat }, geometry);

            angle -= sweep;
        }
    }

    private static Point PointAt(Point centre, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;

        return new Point(centre.X + radius * Math.Cos(radians), centre.Y + radius * Math.Sin(radians));
    }

    private static double[] Numbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<double>();

        return text.Split(',')
            .Select(part => double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0d)
            .ToArray();
    }

    private static string[] Colours(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        return text.Split(',').Select(part => part.Trim()).ToArray();
    }
}
