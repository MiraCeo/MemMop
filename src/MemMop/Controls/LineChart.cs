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

    /// <summary>Identifies the <see cref="WindowSeconds"/> property.</summary>
    public static readonly DependencyProperty WindowSecondsProperty = DependencyProperty.Register(
        nameof(WindowSeconds), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(300d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="SpanSeconds"/> property.</summary>
    public static readonly DependencyProperty SpanSecondsProperty = DependencyProperty.Register(
        nameof(SpanSeconds), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

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

    /// <summary>Identifies the <see cref="Threshold"/> property.</summary>
    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="TooltipBrush"/> property.</summary>
    public static readonly DependencyProperty TooltipBrushProperty = DependencyProperty.Register(
        nameof(TooltipBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="ThresholdBrush"/> property.</summary>
    public static readonly DependencyProperty ThresholdBrushProperty = DependencyProperty.Register(
        nameof(ThresholdBrush), typeof(Brush), typeof(LineChart),
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

    /// <summary>Gets or sets how far back the chart reaches, in seconds.</summary>
    public double WindowSeconds
    {
        get => (double)GetValue(WindowSecondsProperty);
        set => SetValue(WindowSecondsProperty, value);
    }

    /// <summary>
    /// Gets or sets how much time the readings actually cover, in seconds. When it is shorter than the window
    /// the line is drawn against the right edge and grows leftwards, rather than being stretched to fill a
    /// window it does not have.
    /// </summary>
    public double SpanSeconds
    {
        get => (double)GetValue(SpanSecondsProperty);
        set => SetValue(SpanSecondsProperty, value);
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

    /// <summary>Gets or sets the usage percentage at which automatic cleaning fires; 0 draws no line.</summary>
    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <summary>Gets or sets the background of the hover label; null draws the text bare.</summary>
    public Brush TooltipBrush
    {
        get => (Brush)GetValue(TooltipBrushProperty);
        set => SetValue(TooltipBrushProperty, value);
    }

    /// <summary>Gets or sets the colour of the threshold line; null draws none.</summary>
    public Brush ThresholdBrush
    {
        get => (Brush)GetValue(ThresholdBrushProperty);
        set => SetValue(ThresholdBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="HoverIndex"/> property.</summary>
    public static readonly DependencyProperty HoverIndexProperty = DependencyProperty.Register(
        nameof(HoverIndex), typeof(int), typeof(LineChart),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets which reading the pointer is on; -1 when it is not on the chart.</summary>
    public int HoverIndex
    {
        get => (int)GetValue(HoverIndexProperty);
        set => SetValue(HoverIndexProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);

        HoverIndex = IndexAt(e.GetPosition(this).X);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        HoverIndex = -1;
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

        // a transparent fill so the whole control answers the pointer, not just the drawn line
        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

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

            var text = Label(FormatPercent(ratio), typeface, dpi);

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

            var divisionAgo = WindowSeconds * (1 - ratio);

            // one unit per axis: a one-minute window labelled in seconds throughout, the rest in minutes
            var text = Label(
                i == Divisions
                    ? "现在"
                    : WindowSeconds <= 120
                        ? ((int)Math.Round(divisionAgo)).ToString(CultureInfo.InvariantCulture) + " 秒前"
                        : FormatAgo(divisionAgo),
                typeface,
                dpi);

            // the first and last labels would hang off the edge, so they are pulled back inside
            var left = Math.Max(0, Math.Min(ActualWidth - text.Width, x - text.Width / 2));

            context.DrawText(text, new Point(left, plotHeight + 2));
        }

        // where automatic cleaning fires: the line crossing this is the whole point of the chart
        if (Threshold > 0 && ThresholdBrush != null)
        {
            var y = plotHeight - Math.Min(1, Threshold / 100) * plotHeight;
            var pen = new Pen(ThresholdBrush, 1) { DashStyle = new DashStyle(new double[] { 3, 3 }, 0) };

            context.DrawLine(pen, new Point(LabelWidth, y), new Point(ActualWidth, y));

            if (LabelBrush != null)
            {
                var text = Label("自动清理 " + Threshold.ToString("0", CultureInfo.InvariantCulture) + "%", typeface, dpi);

                // the label goes on whichever side of the threshold the readings are not
                var firstRatio = Math.Max(0, Math.Min(1, values[0] / maximum));
                var dataAbove = plotHeight - firstRatio * plotHeight < y;

                context.DrawText(text, new Point(LabelWidth + 2, dataAbove ? y + 1 : y - text.Height - 1));
            }
        }

        var points = new Point[values.Length];

        // the newest reading sits on the right edge; the oldest is as far left as the readings actually reach,
        // which is short of the window until the buffer has filled
        var span = SpanSeconds > 0 ? Math.Min(SpanSeconds, WindowSeconds) : WindowSeconds;

        for (var i = 0; i < values.Length; i++)
        {
            var pointAgo = span * (1 - (double)i / (values.Length - 1));
            var x = LabelWidth + plotWidth * (1 - pointAgo / WindowSeconds);
            var ratio = Math.Max(0, Math.Min(1, values[i] / maximum));

            points[i] = new Point(x, plotHeight - ratio * plotHeight);
        }

        if (FillBrush != null)
        {
            // the area starts where the readings start, not at the left edge: an empty stretch of window is
            // empty, and filling it would claim data that is not there
            var area = new PathFigure { StartPoint = new Point(points[0].X, plotHeight), IsClosed = true };

            foreach (var point in points)
                area.Segments.Add(new LineSegment(point, true));

            area.Segments.Add(new LineSegment(new Point(points[points.Length - 1].X, plotHeight), true));

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

        // the newest reading gets a dot, so "now" is a place on the line and not just a number in the title
        var last = points[points.Length - 1];

        context.DrawEllipse(LineBrush, null, last, 2.5, 2.5);

        if (MarkerBrush == null)
            return;

        foreach (var index in Numbers(Markers))
        {
            var i = (int)Math.Round(index);

            if (i < 0 || i >= points.Length)
                continue;

            // a dot where the line lands, not a line down the whole plot: the drop is already a shape, and a
            // full-height rule reads as another grid line rather than as an event
            context.DrawEllipse(null, new Pen(LineBrush, 1.5), points[i], 3, 3);

            // how much the clean actually freed, measured across the drop
            if (LabelBrush == null || i == 0)
                continue;

            var drop = (values[i - 1] - values[i]) / maximum * 100;

            if (drop < 1)
                continue;

            var freed = Label("-" + drop.ToString("0", CultureInfo.InvariantCulture) + "%", typeface, dpi);
            var freedLeft = Math.Max(LabelWidth, Math.Min(ActualWidth - freed.Width, points[i].X + 5));

            context.DrawText(freed, new Point(freedLeft, points[i].Y + 2));
        }

        var hover = HoverIndex;

        if (hover < 0 || hover >= points.Length)
            return;

        // where the pointer is: a guide up the plot, a dot on the line, and the reading in words
        var hovered = points[hover];

        context.DrawLine(
            new Pen(LabelBrush ?? LineBrush, 1) { DashStyle = new DashStyle(new double[] { 2, 2 }, 0) },
            new Point(hovered.X, 0),
            new Point(hovered.X, plotHeight));

        context.DrawEllipse(LineBrush, null, hovered, 3, 3);

        if (LabelBrush == null)
            return;

        var ago = span * (1 - (double)hover / (points.Length - 1));
        var when = ago < 1 ? "现在" : FormatAgo(ago);

        var reading = Label(
            when + " · " + (values[hover] / maximum * 100).ToString("0", CultureInfo.InvariantCulture) + "%",
            typeface,
            dpi);

        var boxLeft = Math.Max(LabelWidth, Math.Min(ActualWidth - reading.Width - 8, hovered.X - reading.Width / 2 - 4));
        var boxTop = Math.Max(0, hovered.Y - reading.Height - 10);

        if (TooltipBrush != null)
            context.DrawRoundedRectangle(TooltipBrush, null, new Rect(boxLeft, boxTop, reading.Width + 8, reading.Height + 4), 3, 3);

        context.DrawText(reading, new Point(boxLeft + 4, boxTop + 2));
    }

    private int IndexAt(double x)
    {
        var values = Numbers(Values);
        var plotWidth = ActualWidth - LabelWidth;

        if (values.Length < 2 || plotWidth <= 0)
            return -1;

        var ratio = (x - LabelWidth) / plotWidth;

        if (ratio < -0.02 || ratio > 1.02)
            return -1;

        return Math.Max(0, Math.Min(values.Length - 1, (int)Math.Round(ratio * (values.Length - 1))));
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

    /// <summary>
    /// Formats a distance in the past. Seconds are shown below two minutes, because a one-minute window
    /// labelled in whole minutes would read "1 分钟前" at every division.
    /// </summary>
    private static string FormatAgo(double seconds)
    {
        var whole = (int)Math.Round(seconds);

        if (whole < 60)
            return whole.ToString(CultureInfo.InvariantCulture) + " 秒前";

        if (whole >= 3600)
        {
            var hours = whole / 3600;
            var hourRest = whole % 3600 / 60;

            return hourRest == 0
                ? hours.ToString(CultureInfo.InvariantCulture) + " 小时前"
                : hours.ToString(CultureInfo.InvariantCulture) + " 小时 " + hourRest.ToString(CultureInfo.InvariantCulture) + " 分前";
        }

        var minutes = whole / 60;
        var rest = whole % 60;

        return rest == 0
            ? minutes.ToString(CultureInfo.InvariantCulture) + " 分钟前"
            : minutes.ToString(CultureInfo.InvariantCulture) + " 分 " + rest.ToString(CultureInfo.InvariantCulture) + " 秒前";
    }

    private static string FormatPercent(double ratio)
        => (ratio * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static double[] Numbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<double>();

        return text.Split(',')
            .Select(part => double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0d)
            .ToArray();
    }
}
