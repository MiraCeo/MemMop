using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace MemMop.Controls;

/// <summary>
/// A line over time, drawn from evenly spaced readings. The vertical axis is fixed to the machine's total
/// memory rather than scaled to the data, so the height of the line means something: a flat line near the
/// top is a machine under pressure, not a machine whose readings happen to be similar.
/// </summary>
public sealed class LineChart : FrameworkElement
{
    /// <summary>Identifies the <see cref="Values"/> property.</summary>
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(string), typeof(LineChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Maximum"/> property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Markers"/> property.</summary>
    public static readonly DependencyProperty MarkersProperty = DependencyProperty.Register(
        nameof(Markers), typeof(string), typeof(LineChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="LineBrush"/> property.</summary>
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FillBrush"/> property.</summary>
    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="MarkerBrush"/> property.</summary>
    public static readonly DependencyProperty MarkerBrushProperty = DependencyProperty.Register(
        nameof(MarkerBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the readings, comma-separated, oldest first.</summary>
    public string Values
    {
        get => (string)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Gets or sets the value the top of the chart represents.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Gets or sets the indices to mark with a vertical line, comma-separated.</summary>
    public string Markers
    {
        get => (string)GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>Gets or sets the colour of the line.</summary>
    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>Gets or sets the colour of the area under the line; null draws no area.</summary>
    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    /// <summary>Gets or sets the colour of the clean markers; null draws none.</summary>
    public Brush MarkerBrush
    {
        get => (Brush)GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);

        var values = Numbers(Values);
        var maximum = Maximum;

        if (values.Length < 2 || maximum <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var points = new Point[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var x = ActualWidth * i / (values.Length - 1);
            var ratio = Math.Max(0, Math.Min(1, values[i] / maximum));

            points[i] = new Point(x, ActualHeight - ratio * ActualHeight);
        }

        if (FillBrush != null)
        {
            var area = new PathFigure { StartPoint = new Point(0, ActualHeight), IsClosed = true };

            foreach (var point in points)
                area.Segments.Add(new LineSegment(point, true));

            area.Segments.Add(new LineSegment(new Point(ActualWidth, ActualHeight), true));

            var geometry = new PathGeometry();

            geometry.Figures.Add(area);

            // the area is a hint, not a second reading: the line carries the value
            context.PushOpacity(0.12);
            context.DrawGeometry(FillBrush, null, geometry);
            context.Pop();
        }

        var line = new PathFigure { StartPoint = points[0] };

        for (var i = 1; i < points.Length; i++)
            line.Segments.Add(new LineSegment(points[i], true));

        var stroke = new PathGeometry();

        stroke.Figures.Add(line);
        context.DrawGeometry(null, new Pen(LineBrush, 1.5) { LineJoin = PenLineJoin.Round }, stroke);

        if (MarkerBrush == null)
            return;

        var pen = new Pen(MarkerBrush, 1);

        foreach (var index in Numbers(Markers))
        {
            var i = (int)Math.Round(index);

            if (i < 0 || i >= points.Length)
                continue;

            context.DrawLine(pen, new Point(points[i].X, 0), new Point(points[i].X, ActualHeight));
        }
    }

    private static double[] Numbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<double>();

        return text.Split(',')
            .Select(part => double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0d)
            .ToArray();
    }
}
