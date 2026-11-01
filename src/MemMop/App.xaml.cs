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
    private MainWindow? _window;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);

        Settings.Load();

        Strings.Current.SetLanguage(Settings.Language == "en");

        _tray = new TrayService();
        _tray.Initialize();
        _tray.ShowRequested += ShowWindow;
        _tray.ViewRequested += ShowView;
        _tray.CleanRequested += () => (MainWindow?.DataContext as MainViewModel)?.CleanCommand.Execute(null);
        _tray.ExitRequested += Quit;

        _window = new MainWindow();
        _window.Closing += OnWindowClosing;
        _window.MemoryRead += OnMemoryRead;

        // the very first run has nothing in the tray the user could recognise yet, so show the window
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
        _tray?.Dispose();

        base.OnExit(e);
    }

    private MainWindow? MainWindow => _window;

    private static void LogCrash(Exception? exception)
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

        _tray?.Notify("已最小化到托盘");
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
