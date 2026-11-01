using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemMop;

internal static class Program
{
    private static readonly string[] Themes = { "Ceobe", "Amber", "Mono", "Slate" };

    [STAThread]
    private static void Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "memmop-shots");
        Directory.CreateDirectory(output);

        MemMop.Core.Settings.Load();

        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        foreach (var theme in Themes)
        {
            ApplyTheme(theme);

            Render(Path.Combine(output, theme.ToLowerInvariant() + "-normal.png"), 540, 640, false);
            Render(Path.Combine(output, theme.ToLowerInvariant() + "-compact.png"), 320, 190, true);
        }

        Console.WriteLine(output);

        app.Shutdown();
    }

    private static void ApplyTheme(string name)
    {
        var resources = System.Windows.Application.Current.Resources;

        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MemMop;component/Theme/Tokens." + name + ".xaml"),
        });
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MemMop;component/Theme/Styles.xaml"),
        });
    }

    private static void Render(string path, double width, double height, bool compact)
    {
        var window = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -30000,
            Top = -30000,
            ShowInTaskbar = false,
            Width = width,
            Height = height,
            CompactMode = compact,
        };

        window.Show();
        window.UpdateLayout();

        const double scale = 1.5;
        var bitmap = new RenderTargetBitmap(
            (int)(width * scale),
            (int)(height * scale),
            96 * scale,
            96 * scale,
            PixelFormats.Pbgra32);

        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(path);
        encoder.Save(stream);

        window.Close();
    }
}
