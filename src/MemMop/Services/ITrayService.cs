using MemMop.Core;

namespace MemMop.Services;

/// <summary>
/// The notification-area presence. Kept behind an interface so the window never touches the implementation.
/// </summary>
public interface ITrayService : IDisposable
{
    /// <summary>Raised when the user asks for the window.</summary>
    event Action? ShowRequested;

    /// <summary>Raised when the user asks to open a specific page.</summary>
    event Action<AppPage>? ViewRequested;

    /// <summary>Raised when the user asks for a clean.</summary>
    event Action? CleanRequested;

    /// <summary>Raised when the user asks to quit.</summary>
    event Action? ExitRequested;

    /// <summary>
    /// Creates the icon and shows it.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Updates the tooltip.
    /// </summary>
    /// <param name="text">The text to show on hover.</param>
    void UpdateTooltip(string text);

    /// <summary>
    /// Shows a balloon once.
    /// </summary>
    /// <param name="message">The message.</param>
    void Notify(string? message);
}
