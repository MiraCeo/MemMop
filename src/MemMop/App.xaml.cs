using System.Windows;
using Application = System.Windows.Application;
using MemMop.Core;
using MemMop.Services;
using MemMop.ViewModel;

namespace MemMop;

/// <summary>
/// Application entry point: owns the tray icon and the window's relationship with it.
/// </summary>
public partial class App : Application
{
    private TrayService? _tray;
    private System.Windows.Threading.DispatcherTimer? _autoCleanTimer;
    private System.Threading.Mutex? _instanceMutex;
    private System.Threading.EventWaitHandle? _showInstance;
    private MainWindow? _window;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        // one instance only: a second launch asks the first to show itself, then leaves
        _instanceMutex = new System.Threading.Mutex(true, "MemMop.SingleInstance", out var isFirst);

        if (!isFirst)
        {
            try
            {
                System.Threading.EventWaitHandle.OpenExisting("MemMop.ShowInstance").Set();
            }
            catch (Exception)
            {
                // the first instance is not listening yet; nothing else to do
            }

            Shutdown();

            return;
        }

        try
        {
            _showInstance = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "MemMop.ShowInstance");

            var listener = new System.Threading.Thread(() =>
            {
                while (_showInstance.WaitOne())
                    Dispatcher.BeginInvoke(new Action(ShowWindow));
            })
            { IsBackground = true };

            listener.Start();
        }
        catch (Exception)
        {
            // without the event the second instance simply exits; the window is still reachable from the tray
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);

        Settings.Load();

        Strings.Current.SetLanguage(Settings.Language == "en");

        _tray = new TrayService();
        _tray.Initialize();
        _tray.ShowRequested += ShowWindow;
        _tray.ViewRequested += ShowView;
        _tray.CleanRequested += () => (Window?.DataContext as MainViewModel)?.CleanCommand.Execute(null);
        _tray.ExitRequested += Quit;

        Log.Info("started, version " + (typeof(App).Assembly.GetName().Version?.ToString() ?? "?"));

        _window = new MainWindow();
        _window.Closing += OnWindowClosing;
        _window.MemoryRead += OnMemoryRead;

        // the very first run has nothing in the tray the user could recognise yet, so show the window
        var viewModel = _window.DataContext as MainViewModel;

        if (viewModel != null)
        {
            viewModel.CleanCompleted += summary =>
            {
                if (Settings.ShowNotifications)
                    _tray?.Notify(summary);
            };

            // the policy decides whether anything is due; this only asks it regularly
            _autoCleanTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoCleanTimer.Tick += async (_, _) =>
            {
                var message = await viewModel.TryAutoCleanAsync(DateTime.Now);

                if (!string.IsNullOrEmpty(message) && Settings.ShowNotifications)
                    _tray?.Notify(message);
            };

            _autoCleanTimer.Start();
        }

        if (!Settings.StartMinimized || !Settings.ShownTrayHint)
            _window.Show();
        else
            ShowTrayHintOnce();

        base.OnStartup(e);
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        Settings.Save();
        _autoCleanTimer?.Stop();
        _tray?.Dispose();

        base.OnExit(e);
    }

    private MainWindow? Window => _window;

    private static void LogCrash(Exception? exception)
    {
        Log.Error("unhandled exception", exception);

        WriteCrashFile(exception);
    }

    private static void WriteCrashFile(Exception? exception)
    {
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "memmop-crash.txt");

            System.IO.File.WriteAllText(path, DateTime.Now + Environment.NewLine + exception);
        }
        catch
        {
            // a crash log must never itself crash
        }
    }

    private void OnMemoryRead(string tooltip)
    {
        _tray?.UpdateTooltip(tooltip);

        var first = tooltip.Split('\n')[0];
        var last = (_window?.DataContext as MainViewModel)?.LastCleanText ?? string.Empty;

        _tray?.SetStatus(first, last);
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!Settings.CloseToTray)
            return;

        // close means "get out of the way", not "quit" - quitting lives in the tray menu
        e.Cancel = true;
        _window?.Hide();
        _tray?.SetCleanEnabled(false);

        ShowTrayHintOnce();
    }

    private void ShowTrayHintOnce()
    {
        if (Settings.ShownTrayHint)
            return;

        Settings.ShownTrayHint = true;
        Settings.Save();

        _tray?.Notify(MemMop.Core.Strings.Current.MinimizedToTray);
    }

    private void ShowWindow()
    {
        if (_window == null)
            return;

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
        _tray?.SetCleanEnabled(true);
    }

    private void ShowView(int index)
    {
        ShowWindow();

        _window?.ShowTab(index);
    }

    private void Quit()
    {
        if (_window != null)
            _window.Closing -= OnWindowClosing;

        Shutdown();
    }
}
