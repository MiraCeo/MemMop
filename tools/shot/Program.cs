using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemMop;
using MemMop.Core;
using MemMop.ViewModel;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            Console.WriteLine("FIRST: " + e.Exception.GetType().FullName + " :: " + e.Exception.Message);
        };

        var output = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "memmop-shots");
        Directory.CreateDirectory(output);

        Settings.Load();

        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var viewModel = new MainViewModel();

        Render(new MainWindow(), viewModel, Path.Combine(output, "normal.png"), 540, 640);
        Render(new MainWindow(), viewModel, Path.Combine(output, "compact.png"), 320, 190, compact: true);

        Console.WriteLine(output);

        app.Shutdown();
    }

    private static void Render(MainWindow window, MainViewModel viewModel, string path, double width, double height, bool compact = false)
    {
        window.DataContext = viewModel;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -30000;
        window.Top = -30000;
        window.ShowInTaskbar = false;
        window.Width = width;
        window.Height = height;
        window.CompactMode = compact;

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
