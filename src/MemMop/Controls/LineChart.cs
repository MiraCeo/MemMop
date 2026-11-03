using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace MemMop.Controls;

/// <summary>
/// A line over time, drawn from evenly spaced readings, with both axes divided into fifths and labelled.
/// The vertical axis is fixed to the machine's total memory rather than scaled to the data, so the height of
/// the line means something: a flat line near the top is a machine under pressure, not a machine whose
/// readings happen to be similar.
/// </summary>
public sealed class LineChart : FrameworkElement
{
    /// <summary>How many segments each axis is divided into.</summary>
    private const int Divisions = 5;

    /// <summary>Room reserved on the left for the vertical axis labels.</summary>
    private const double LabelWidth = 54;

    /// <summary>Room reserved at the bottom for the horizontal axis labels.</summary>
    private const double LabelHeight = 16;

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

    /// <summary>Identifies the <see cref="WindowMinutes"/> property.</summary>
    public static readonly DependencyProperty WindowMinutesProperty = DependencyProperty.Register(
        nameof(WindowMinutes), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(15d, FrameworkPropertyMetadataOptions.AffectsRender));

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

    /// <summary>Identifies the <see cref="GridBrush"/> property.</summary>
    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="LabelBrush"/> property.</summary>
    public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
        nameof(LabelBrush), typeof(Brush), typeof(LineChart),
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

    /// <summary>Gets or sets how far back the chart reaches, in minutes, for the horizontal axis labels.</summary>
    public double WindowMinutes
    {
        get => (double)GetValue(WindowMinutesProperty);
        set => SetValue(WindowMinutesProperty, value);
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

    /// <summary>Gets or sets the colour of the grid lines; null draws none.</summary>
    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>Gets or sets the colour of the axis labels; null draws none.</summary>
    public Brush LabelBrush
    {
        get => (Brush)GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);

        var values = Numbers(Values);
        var maximum = Maximum;
        var plotWidth = ActualWidth - LabelWidth;
        var plotHeight = ActualHeight - LabelHeight;

        if (values.Length < 2 || maximum <= 0 || plotWidth <= 0 || plotHeight <= 0)
            return;

        var typeface = new Typeface("Segoe UI");
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var gridPen = GridBrush == null ? null : new Pen(GridBrush, 1);

        // the horizontal divisions, each labelled with the memory it represents
        for (var i = 0; i <= Divisions; i++)
        {
            var ratio = i / (double)Divisions;
            var y = plotHeight - ratio * plotHeight;

            if (gridPen != null)
                context.DrawLine(gridPen, new Point(LabelWidth, y), new Point(ActualWidth, y));

            if (LabelBrush == null)
                continue;

            var text = Label(FormatSize(maximum * ratio), typeface, dpi);

            // the top and bottom labels would hang outside the plot, so they are pulled back inside
            var top = Math.Max(0, Math.Min(plotHeight - text.Height, y - text.Height / 2));

            context.DrawText(text, new Point(LabelWidth - text.Width - 6, top));
        }

        // the vertical divisions, each labelled with how long ago it was
        for (var i = 0; i <= Divisions; i++)
        {
            var ratio = i / (double)Divisions;
            var x = LabelWidth + ratio * plotWidth;

            if (gridPen != null)
                context.DrawLine(gridPen, new Point(x, 0), new Point(x, plotHeight));

            if (LabelBrush == null)
                continue;

            var minutes = WindowMinutes * (1 - ratio);
            var text = Label(
                i == Divisions
                    ? "现在"
                    : "-" + minutes.ToString("0", CultureInfo.InvariantCulture) + " 分钟",
                typeface,
                dpi);

            // the first and last labels would hang off the edge, so they are pulled back inside
            var left = Math.Max(0, Math.Min(ActualWidth - text.Width, x - text.Width / 2));

            context.DrawText(text, new Point(left, plotHeight + 2));
        }

        var points = new Point[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var x = LabelWidth + plotWidth * i / (values.Length - 1);
            var ratio = Math.Max(0, Math.Min(1, values[i] / maximum));

            points[i] = new Point(x, plotHeight - ratio * plotHeight);
        }

        if (FillBrush != null)
        {
            var area = new PathFigure { StartPoint = new Point(LabelWidth, plotHeight), IsClosed = true };

            foreach (var point in points)
                area.Segments.Add(new LineSegment(point, true));

            area.Segments.Add(new LineSegment(new Point(ActualWidth, plotHeight), true));

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

        var markerPen = new Pen(MarkerBrush, 1);

        foreach (var index in Numbers(Markers))
        {
            var i = (int)Math.Round(index);

            if (i < 0 || i >= points.Length)
                continue;

            context.DrawLine(markerPen, new Point(points[i].X, 0), new Point(points[i].X, plotHeight));
        }
    }

    private FormattedText Label(string text, Typeface typeface, double dpi)
        => new(
            text,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            10,
            LabelBrush,
            dpi);

    private static string FormatSize(double bytes)
    {
        const double gigabyte = 1024d * 1024 * 1024;

        if (bytes <= 0)
            return "0";

        return bytes >= gigabyte
            ? (bytes / gigabyte).ToString("0.#", CultureInfo.InvariantCulture) + " GB"
            : (bytes / (1024d * 1024)).ToString("0", CultureInfo.InvariantCulture) + " MB";
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
