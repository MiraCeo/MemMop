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
    [STAThread]
    private static void Main()
    {
        var app = new System.Windows.Application();

        // what the ComboBox template contains: a 10x6 path, right aligned with a 12px margin, vertically centred
        const double boxWidth = 200;
        const double boxHeight = 32;
        const double pathWidth = 10;
        const double pathHeight = 6;
        const double rightMargin = 12;
        const double topMargin = 1;

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
