using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemMop;

internal static class Program
{
    private static readonly string[] Themes = { "Ceobe" };

    private static readonly (string Name, double Accent, double Highlight, string Surface)[] Variants =
    {
        ("L1", 0.6, 0, "#1C150F"),
        ("L2", 0.6, 0, "#221A13"),
        ("L3", 0.6, 0, "#281F16"),
        ("L4", 0.6, 0, "#2E241A"),
    };

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

            foreach (var variant in Variants)
            {
                System.Windows.Application.Current.Resources["TitleBarAccentOpacity"] = variant.Accent;
                System.Windows.Application.Current.Resources["TitleBarSurface"] =
                    new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(variant.Surface));
                System.Windows.Application.Current.Resources["TitleBarHighlightOpacity"] = variant.Highlight;

                Render(Path.Combine(output, theme.ToLowerInvariant() + "-" + variant.Name + ".png"), 540, 640, false, 0);
            }


            Render(Path.Combine(output, theme.ToLowerInvariant() + "-compact.png"), 320, 190, true, 0);
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

    private static void Render(string path, double width, double height, bool compact, int tab)
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
        window.ShowTab(tab);
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
