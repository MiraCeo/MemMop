using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemMop;
using MemMop.ViewModel;

/// <summary>
/// Renders the pages to PNG, including the states that need a seeded list or a switched-on setting.
/// Everything is seeded in memory: the tool never calls <c>Settings.Save</c>, so the user's registry is
/// left alone. Rebuild this project after any XAML change - it compiles the app's markup into its own exe.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "memmop-shots");
        Directory.CreateDirectory(output);

        MemMop.Core.Settings.Load();

        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        ApplyTheme("Ceobe");

        // 1. the default states, with whatever the user actually has configured
        Render(Path.Combine(output, "home.png"), 540, 640, false, 0);
        Render(Path.Combine(output, "settings.png"), 540, 640, false, 1);
        Render(Path.Combine(output, "compact.png"), 320, 212, true, 0);

        // 2. the whitelist, with a list worth looking at
        SeedExclusions();
        Render(Path.Combine(output, "whitelist.png"), 540, 640, false, 2, vm => vm.SelectedExclusion = "ChatGPT");
        Render(
            Path.Combine(output, "whitelist-open.png"),
            540,
            640,
            false,
            2,
            vm =>
            {
                vm.SelectedExclusion = "ChatGPT";
                vm.IsPickerOpen = true;
                vm.ProcessSearchText = "c";
            });

        // 3. automatic cleaning switched on, which the home page reports
        MemMop.Core.Settings.AutoCleanMinutes = 30;
        MemMop.Core.Settings.LowMemoryPercent = 80;
        Render(Path.Combine(output, "settings-autoclean.png"), 540, 640, false, 1);
        Render(Path.Combine(output, "home-autoclean.png"), 540, 640, false, 0);

        Console.WriteLine(output);

        app.Shutdown();
    }

    private static void SeedExclusions()
    {
        MemMop.Core.Settings.ExcludedProcesses.Clear();
        MemMop.Core.Settings.ExcludedProcesses.Add("chrome");
        MemMop.Core.Settings.ExcludedProcesses.Add("ChatGPT");
        MemMop.Core.Settings.ExcludedProcesses.Add("DSH Desktop");
        MemMop.Core.Settings.ExcludedProcesses.Add("msedgewebview2");
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

        // the title bar variant the screenshots have always used
        resources["TitleBarAccentOpacity"] = 0.6;
        resources["TitleBarSurface"] = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#281F16"));
        resources["TitleBarHighlightOpacity"] = 0.0;
    }

    private static void Render(
        string path,
        double width,
        double height,
        bool compact,
        int tab,
        Action<MainViewModel> prepare = null)
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

        if (prepare != null && window.DataContext is MainViewModel viewModel)
        {
            prepare(viewModel);
            window.UpdateLayout();
        }

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
