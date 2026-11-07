using System.IO;
using System.Text;

namespace MemMop.Core;

/// <summary>
/// Writes what the program did, to a file under the user's local application data. The event log was the
/// first sink and it was the wrong one: creating a source needs rights the program does not always have, and
/// reading it back means opening the event viewer. A file is something a user can find, read, and send.
/// </summary>
/// <remarks>
/// Nothing here throws. A logger that takes the program down is worse than no logger.
/// </remarks>
public static class Log
{
    /// <summary>How large the file may grow before it is rotated.</summary>
    private const long MaxBytes = 1024 * 1024;

    private static readonly object Gate = new();

    /// <summary>Gets the file the program writes to.</summary>
    public static string Path => System.IO.Path.Combine(Folder, "memmop.log");

    /// <summary>Gets the folder the program writes to.</summary>
    public static string Folder => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MemMop");

    /// <summary>
    /// Records an informational message.
    /// </summary>
    /// <param name="message">The message.</param>
    public static void Info(string message) => Write("info ", message, null);

    /// <summary>
    /// Records a failure.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception, when there is one.</param>
    public static void Error(string message, Exception? exception = null) => Write("error", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                Rotate();

                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message;

                if (exception != null)
                    line += Environment.NewLine + exception;

                File.AppendAllText(Path, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
            // nowhere left to write; staying silent beats taking the program down
        }
    }

    /// <summary>
    /// Keeps one previous file. A log that grows without bound is a log nobody keeps.
    /// </summary>
    private static void Rotate()
    {
        var file = new FileInfo(Path);

        if (!file.Exists || file.Length < MaxBytes)
            return;

        var previous = Path + ".1";

        if (File.Exists(previous))
            File.Delete(previous);

        File.Move(Path, previous);
    }
}
