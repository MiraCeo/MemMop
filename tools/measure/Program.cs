using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Path = System.Windows.Shapes.Path;

internal static class Program
{
    /// <summary>
    /// Reads the chevron's geometry straight out of the shipped template, so the measurement cannot drift from it.
    /// </summary>
    private static (double Width, double Height, double Right, double Top) ReadChevron()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? ".",
            "Styles.xaml");

        if (!System.IO.File.Exists(path))
            path = @"D:\MemClean\MemMop\src\MemMop\Theme\Styles.xaml";

        var text = System.IO.File.ReadAllText(path);
        var block = text.Substring(text.IndexOf("x:Name=\"Chevron\"", StringComparison.Ordinal));

        double Pick(string attribute)
        {
            var match = System.Text.RegularExpressions.Regex.Match(block, attribute + "=\"([0-9.]+)\"");

            return match.Success ? double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        }

        var margin = System.Text.RegularExpressions.Regex.Match(block, "Margin=\"([0-9., ]+)\"");

        var parts = margin.Success
            ? margin.Groups[1].Value.Split(',').Select(p => double.Parse(p.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray()
            : new double[] { 0, 0, 0, 0 };

        return (Pick("Width"), Pick("Height"), parts[2], parts[1]);
    }

    [STAThread]
    private static void Main()
    {
        var app = new System.Windows.Application();

        var (pathWidth, pathHeight, rightMargin, topMargin) = ReadChevron();

        // what the ComboBox template contains: a 10x6 path, right aligned with a 12px margin, vertically centred
        const double boxWidth = 200;
        const double boxHeight = 32;

        var path = new Path
        {
            Width = pathWidth,
            Height = pathHeight,
            Margin = new Thickness(0, topMargin, rightMargin, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform,
            Data = Geometry.Parse("M 0 0 L 5 5 L 10 0"),
            Stroke = Brushes.White,
            StrokeThickness = 1.4,
        };

        var host = new Border
        {
            Width = boxWidth,
            Height = boxHeight,
            Background = Brushes.Black,
            Child = path,
        };

        host.Measure(new System.Windows.Size(boxWidth, boxHeight));
        host.Arrange(new Rect(0, 0, boxWidth, boxHeight));
        host.UpdateLayout();

        var bounds = path.TransformToAncestor(host).TransformBounds(new Rect(0, 0, path.ActualWidth, path.ActualHeight));
        Console.WriteLine("element box   x " + bounds.X.ToString("0.##") + " .. " + bounds.Right.ToString("0.##") +
                          "   y " + bounds.Y.ToString("0.##") + " .. " + bounds.Bottom.ToString("0.##"));

        foreach (var scale in new[] { 1.0, 1.5 })
        {
            var bitmap = new RenderTargetBitmap(
                (int)(boxWidth * scale), (int)(boxHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);

            bitmap.Render(host);

            var pixels = new byte[(int)(boxWidth * scale) * (int)(boxHeight * scale) * 4];

            bitmap.CopyPixels(pixels, (int)(boxWidth * scale) * 4, 0);

            var width = (int)(boxWidth * scale);
            var height = (int)(boxHeight * scale);
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = (y * width + x) * 4;

                    if (pixels[index] < 40)
                        continue;

                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            var glyphCentreX = (minX + maxX) / 2.0 / scale;
            var glyphCentreY = (minY + maxY) / 2.0 / scale;
            var boxCentreX = bounds.X + bounds.Width / 2;
            var boxCentreY = bounds.Y + bounds.Height / 2;

            Console.WriteLine($"scale {scale}: glyph x {minX / scale:0.##} .. {maxX / scale:0.##}  y {minY / scale:0.##} .. {maxY / scale:0.##}");
            Console.WriteLine($"           centre glyph ({glyphCentreX:0.##}, {glyphCentreY:0.##})  box ({boxCentreX:0.##}, {boxCentreY:0.##})");
            Console.WriteLine($"           offset  dx {glyphCentreX - boxCentreX:0.##}  dy {glyphCentreY - boxCentreY:0.##}");
        }

        app.Shutdown();
    }
}
