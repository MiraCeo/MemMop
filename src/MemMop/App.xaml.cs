using System.IO;
using System.Windows;
using MemMop.Core;

namespace MemMop;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        Settings.Load();

        // headless reading of the same code path the window uses - the window needs elevation,
        // this does not, so the numbers can be checked without a desktop session
        if (e.Args.Contains("--dump"))
        {
            Dump();

            Shutdown();

            return;
        }

        new MainWindow().Show();

        base.OnStartup(e);
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        Settings.Save();

        base.OnExit(e);
    }

    private static void Dump()
    {
        var snapshot = MemorySnapshot.Capture();
        var path = Path.Combine(Path.GetTempPath(), "memmop-dump.txt");

        File.WriteAllLines(path, new[]
        {
            $"total={snapshot.Total}",
            $"available={snapshot.Available}",
            $"active={snapshot.Active}",
            $"modified={snapshot.Modified}",
            $"standby={snapshot.Standby}",
            $"free={snapshot.Free}",
            $"commitLimit={snapshot.CommitLimit}",
            $"commitUsed={snapshot.CommitUsed}",
            $"hasSegments={snapshot.HasSegments}",
        });
    }
}
