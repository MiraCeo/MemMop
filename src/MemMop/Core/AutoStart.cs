using System.Diagnostics;

namespace MemMop.Core;

/// <summary>
/// Starts the program at logon through a scheduled task. A task with the highest available rights does not raise
/// UAC, which a Run entry would.
/// </summary>
public static class AutoStart
{
    private const string TaskName = "MemMop";

    /// <summary>
    /// Gets whether the logon task exists.
    /// </summary>
    /// <returns><c>true</c> when the scheduler reports the task.</returns>
    public static bool IsEnabled() => Run("schtasks", "/Query /TN \"" + TaskName + "\"") == 0;

    /// <summary>
    /// Creates or removes the logon task and reports whether the scheduler agrees afterwards.
    /// </summary>
    /// <param name="enable">Whether the program should start at logon.</param>
    /// <returns><c>true</c> when the resulting state matches the request.</returns>
    public static bool Set(bool enable)
    {
        var executable = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        if (string.IsNullOrEmpty(executable))
            return false;

        var arguments = enable
            ? "/Create /F /SC ONLOGON /RL HIGHEST /TN \"" + TaskName + "\" /TR \"\"" + executable + "\"\""
            : "/Delete /F /TN \"" + TaskName + "\"";

        Run("schtasks", arguments);

        Log.Info("autostart " + (enable ? "enabled" : "disabled") + ": " + executable);

        // the scheduler is asked rather than trusted
        return IsEnabled() == enable;
    }

    private static int Run(string file, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(file, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            if (process == null)
                return -1;

            process.StandardError.ReadToEnd();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode;
        }
        catch (Exception e)
        {
            Log.Error("schtasks failed: " + arguments, e);

            return -1;
        }
    }
}
