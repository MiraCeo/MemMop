using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemMop;
using MemMop.ViewModel;

/// <summary>
/// Renders the pages to PNG, including the states that need a seeded list or a switched-on setting.
/// Everything is seeded in memory: the tool never calls <c>Settings.Save</c>, so the user's registry is
/// left alone - and it checks that afterwards, because the protection is a convention rather than a
/// guarantee. Rebuild this project after any XAML change: it compiles the app's markup into its own exe.
/// </summary>
internal static class Program
{
    /// <summary>The key <see cref="MemMop.Core.Settings"/> writes to; duplicated here so the check can read it.</summary>
    private const string SettingsKey = @"SOFTWARE\MemMop";

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--self-test"))
        {
            SelfTest();
            return;
        }

        var output = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "memmop-shots");
        Directory.CreateDirectory(output);

        var before = ReadSettingsKey();

        MemMop.Core.Settings.Load();

        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        ApplyTheme("Ceobe");

        // 1. the default states, with whatever the user actually has configured
        Render(Path.Combine(output, "home.png"), 540, 640, false, 0);
        Render(Path.Combine(output, "statistics.png"), 540, 640, false, 1);
        Render(Path.Combine(output, "settings.png"), 540, 640, false, 2);
        Render(Path.Combine(output, "compact.png"), 320, 212, true, 0);

        // 1a. the statistics page with a line worth looking at: a sawtooth, which is what a machine that is
        // being cleaned actually looks like. The readings go through the view model, so the chart is drawn
        // from real data rather than a mock-up.
        Render(Path.Combine(output, "statistics-chart.png"), 540, 640, false, 1, vm =>
        {
            const long total = 33500744909;   // 31.2 GB
            var random = new Random(7);
            var used = total * 0.72;

            for (var i = 0; i < 180; i++)
            {
                used += random.Next(-40, 90) * 1024L * 1024;

                if (i == 60 || i == 120)
                    used -= total * 0.06;

                used = Math.Max(total * 0.55, Math.Min(total * 0.95, used));

                vm.SampleMemory((long)used, (long)(total * 0.08), (long)(total * 0.02));
            }

            // two runs, so the "last clean" and "history" cards have something in them and the chart gets
            // its markers. The values go through the same path a real run takes.
            vm.RecordClean(false, TimeSpan.FromSeconds(17), new[]
            {
                ("清理待机缓存", 1288490189L),
                ("仅清理低优先级待机缓存", 0L),
                ("写回已修改内存", 429496730L),
                ("合并重复内存页", 92274688L),
                ("收缩系统缓存", 0L),
                ("注册表缓存整理", 12582912L),
                ("收缩程序驻留内存", 2254857830L),
            });

            vm.RecordClean(true, TimeSpan.FromSeconds(9), new[]
            {
                ("清理待机缓存", 644245094L),
                ("写回已修改内存", 0L),
                ("合并重复内存页", 0L),
                ("注册表缓存整理", 0L),
            });
        });

        // 1b. the home page as it looks after a run: each row reports what it released. The values go
        // through AreaItem.Apply, so the screenshot shows the real formatting rather than a mock-up.
        Render(Path.Combine(output, "home-results.png"), 540, 640, false, 0, vm =>
        {
            long[] released = { 1288490189, 0, 429496730, 92274688, 0, 12582912, 2254857830 };

            for (var i = 0; i < vm.Areas.Count && i < released.Length; i++)
                vm.Areas[i].Apply(new MemMop.Core.ReclaimResult(true, released[i], null));
        });

        // 2. the whitelist, with a list worth looking at
        SeedExclusions();
        Render(Path.Combine(output, "whitelist.png"), 540, 640, false, 3, vm => vm.SelectedExclusion = "ChatGPT");
        Render(
            Path.Combine(output, "whitelist-open.png"),
            540,
            640,
            false,
            3,
            vm =>
            {
                vm.SelectedExclusion = "ChatGPT";
                vm.IsPickerOpen = true;
                vm.ProcessSearchText = "c";
            });

        // 3. automatic cleaning switched on, which the home page reports
        MemMop.Core.Settings.AutoCleanMinutes = 30;
        MemMop.Core.Settings.LowMemoryPercent = 80;
        Render(Path.Combine(output, "settings-autoclean.png"), 540, 640, false, 2);
        Render(Path.Combine(output, "home-autoclean.png"), 540, 640, false, 0);

        Console.WriteLine(output);

        app.Shutdown();

        VerifyRegistryUntouched(before);
    }

    /// <summary>
    /// Proves the check can fail: hands it a snapshot that differs from the live key by one value and
    /// expects it to notice. Run with <c>--self-test</c>; it renders nothing.
    /// </summary>
    private static void SelfTest()
    {
        var doctored = new Dictionary<string, string>(ReadSettingsKey(), StringComparer.Ordinal)
        {
            ["AutoCleanMinutes"] = "999",
        };

        VerifyRegistryUntouched(doctored);

        var caught = Environment.ExitCode == 1;
        Environment.ExitCode = caught ? 0 : 1;

        Console.WriteLine(caught ? "self-test: the check caught the change" : "self-test: THE CHECK MISSED IT");
    }

    /// <summary>
    /// Reads the settings key as a name/value map, so the check compares every value rather than the three
    /// the tool happens to seed.
    /// </summary>
    private static Dictionary<string, string> ReadSettingsKey()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SettingsKey);

        if (key == null)
            return values;

        foreach (var name in key.GetValueNames())
            values[name] = key.GetValue(name)?.ToString() ?? string.Empty;

        return values;
    }

    /// <summary>
    /// Fails the run when the tool wrote to the registry. Seeding in memory is a convention, not a
    /// guarantee: one call to a view-model setter that saves would silently overwrite the user's settings.
    /// </summary>
    private static void VerifyRegistryUntouched(Dictionary<string, string> before)
    {
        var after = ReadSettingsKey();
        var changed = new List<string>();

        foreach (var name in before.Keys.Union(after.Keys, StringComparer.Ordinal))
        {
            before.TryGetValue(name, out var was);
            after.TryGetValue(name, out var now);

            if (was != now)
                changed.Add($"  {name}: [{was}] -> [{now}]");
        }

        if (changed.Count == 0)
        {
            Console.WriteLine("registry: untouched");
            return;
        }

        Console.Error.WriteLine($"the tool wrote to HKCU\\{SettingsKey}, which it must not:");
        changed.ForEach(Console.Error.WriteLine);
        Environment.ExitCode = 1;
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
