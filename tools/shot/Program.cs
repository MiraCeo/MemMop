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

        try
        {
            Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAILED: " + exception.GetType().FullName);
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(exception.StackTrace);

            for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
            {
                Console.Error.WriteLine("INNER: " + inner.GetType().FullName);
                Console.Error.WriteLine(inner.Message);
            }
            Environment.ExitCode = 1;
        }
    }

    private static void Run(string[] args)
    {
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
        Render(Path.Combine(output, "about.png"), 540, 640, false, 4);

        // 0. the same pages in English, so a translation gap shows up as a screenshot rather than a report.
        // The language is switched inside the window, after it has loaded, so the registry is never written.
        Render(Path.Combine(output, "home-en.png"), 540, 640, false, 0, _ => MemMop.Core.Strings.Current.SetLanguage(true));
        Render(Path.Combine(output, "settings-en.png"), 540, 640, false, 2, _ => MemMop.Core.Strings.Current.SetLanguage(true));
        Render(Path.Combine(output, "about-en.png"), 540, 640, false, 4, _ => MemMop.Core.Strings.Current.SetLanguage(true));
        Render(Path.Combine(output, "statistics-en.png"), 540, 640, false, 1, vm =>
        {
            SeedStatistics(vm);
            MemMop.Core.Strings.Current.SetLanguage(true);
        });
        Render(Path.Combine(output, "whitelist-en.png"), 540, 640, false, 3, vm =>
        {
            SeedExclusions();
            vm.SelectedExclusion = "ChatGPT";
            MemMop.Core.Strings.Current.SetLanguage(true);
        });

        MemMop.Core.Strings.Current.SetLanguage(false);

        // 1a0. the home page with the sparkline filled in
        var homeThreshold = MemMop.Core.Settings.LowMemoryPercent;

        MemMop.Core.Settings.LowMemoryPercent = 75;

        Render(Path.Combine(output, "home-chart.png"), 540, 640, false, 0, SeedStatistics);

        MemMop.Core.Settings.LowMemoryPercent = homeThreshold;

        // 1a. the statistics page with a line worth looking at: a sawtooth, which is what a machine that is
        // being cleaned actually looks like. The readings go through the view model, so the chart is drawn
        // from real data rather than a mock-up.
        // the threshold line only exists when automatic cleaning has a memory trigger, so the preview turns
        // one on for the two statistics renders and puts it back afterwards
        var threshold = MemMop.Core.Settings.LowMemoryPercent;

        MemMop.Core.Settings.LowMemoryPercent = 75;

        Render(Path.Combine(output, "statistics-chart.png"), 540, 640, false, 1, SeedStatistics);

        // 1a2. the same page scrolled to the bottom, so the history card can be checked
        Render(Path.Combine(output, "statistics-bottom.png"), 540, 640, false, 1, SeedStatistics, scrollToEnd: true);

        // 1a3. the pointer resting on the chart, so the hover reading can be checked
        Render(Path.Combine(output, "statistics-hover.png"), 540, 640, false, 1, SeedStatistics, hoverIndex: 30);

        MemMop.Core.Settings.LowMemoryPercent = threshold;

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

    /// <summary>
    /// Fills the statistics page with a sawtooth and two runs. Everything goes through the view model, so the
    /// screenshot shows what the real path produces rather than a mock-up.
    /// </summary>
    private static void SeedStatistics(MainViewModel vm)
    {
        const long total = 33500744909;   // 31.2 GB
        var random = new Random(7);
        var used = total * 0.78;

        // the readings are stamped backwards from now, two seconds apart like the real sampler, so the
        // markers land where the drops are instead of piling up at the right edge
        var now = DateTime.Now;
        const int count = 300;   // ten minutes, which fills the five-minute window

        for (var i = 0; i < count; i++)
        {
            used += random.Next(-16, 36) * 1024L * 1024;

            if (i == 100 || i == 200)
                used -= total * 0.06;

            used = Math.Max(total * 0.55, Math.Min(total * 0.95, used));

            // the three parts have to add up to the total, or the chart's vertical axis is wrong
            var standby = (long)(total * 0.08);

            vm.SampleMemory((long)used, standby, total - (long)used - standby, now.AddSeconds(-2 * (count - 1 - i)));
        }

        // The automatic run comes first so the newest record - the one the "last clean" card shows - is the
        // manual one with all seven areas; an automatic run only touches the conservative subset, which is
        // four, and that is what the card showed before this was reordered.
        vm.RecordClean(true, TimeSpan.FromSeconds(9), new[]
        {
            ("StandbyList", 644245094L),
            ("ModifiedPageList", 0L),
            ("CombinedPageList", 0L),
            ("RegistryCache", 0L),
        }, now.AddSeconds(-2 * (count - 1 - 100)));

        vm.RecordClean(false, TimeSpan.FromSeconds(17), new[]
        {
            ("StandbyList", 1288490189L),
            ("StandbyListLowPriority", 0L),
            ("ModifiedPageList", 429496730L),
            ("CombinedPageList", 92274688L),
            ("SystemFileCache", 0L),
            ("RegistryCache", 12582912L),
            ("WorkingSet", 2254857830L),
        }, now.AddSeconds(-2 * (count - 1 - 200)));
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
        Action<MainViewModel> prepare = null,
        bool scrollToEnd = false,
        int hoverIndex = -1)
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

            // the legend lines are built strings, so they only follow a language switch on the next refresh
            window.RefreshNow();
            window.UpdateLayout();
        }

        if (hoverIndex >= 0)
        {
            var chart = FindChart(window);

            if (chart != null)
            {
                chart.HoverIndex = hoverIndex;
                window.UpdateLayout();
            }
        }

        if (scrollToEnd)
        {
            var scroll = FindScrollViewer(window);

            if (scroll != null)
            {
                scroll.ScrollToEnd();
                window.UpdateLayout();
            }
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

    private static MemMop.Controls.LineChart FindChart(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is MemMop.Controls.LineChart found)
                return found;

            var nested = FindChart(child);

            if (nested != null)
                return nested;
        }

        return null;
    }

    private static System.Windows.Controls.ScrollViewer FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is System.Windows.Controls.ScrollViewer found)
                return found;

            var nested = FindScrollViewer(child);

            if (nested != null)
                return nested;
        }

        return null;
    }
}
