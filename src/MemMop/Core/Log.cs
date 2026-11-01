using System.Diagnostics;
using System.IO;
using System.Text;

namespace MemMop.Core;

/// <summary>
/// Writes what the program did. The event log is the primary sink; when it is unavailable - no rights to create
/// the source, a locked log, anything - messages go to a file instead. Nothing here throws.
/// </summary>
public static class Log
{
    private const string Source = "MemMop";
    private const string LogName = "Application";

    private static readonly object Gate = new object();
    private static bool _eventLogAvailable;
    private static bool _checked;

    /// <summary>
    /// Records an informational message.
    /// </summary>
    /// <param name="message">The message.</param>
    public static void Info(string message) => Write(EventLogEntryType.Information, message, null);

    /// <summary>
    /// Records a failure.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception, when there is one.</param>
    public static void Error(string message, Exception? exception = null) => Write(EventLogEntryType.Error, message, exception);

    private static void Write(EventLogEntryType type, string message, Exception? exception)
    {
        var text = exception == null ? message : message + Environment.NewLine + exception;

        lock (Gate)
        {
            try
            {
                if (!_checked)
                {
                    _checked = true;
                    _eventLogAvailable = TryPrepareEventLog();
                }

                if (_eventLogAvailable)
                {
                    EventLog.WriteEntry(Source, text, type);

                    return;
                }
            }
            catch (Exception)
            {
                _eventLogAvailable = false;
            }

            WriteFallback(type, text);
        }
    }

    private static bool TryPrepareEventLog()
    {
        if (!EventLog.SourceExists(Source))
            EventLog.CreateEventSource(Source, LogName);

        return true;
    }

    private static void WriteFallback(EventLogEntryType type, string text)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "memmop.log");

            File.AppendAllText(path, DateTime.Now.ToString("s") + " [" + type + "] " + text + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception)
        {
            // nowhere left to write; staying silent beats taking the program down
        }
    }
}
