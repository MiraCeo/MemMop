namespace MemMop.Core;

/// <summary>
/// What one reclaimer actually did. The point of this type is that a failure is reportable, not silent.
/// </summary>
/// <param name="Succeeded">Whether the operation was carried out.</param>
/// <param name="FreedBytes">Measured change in available memory; zero or less is reported as no change.</param>
/// <param name="Message">Why it failed, or null on success.</param>
public sealed record ReclaimResult(bool Succeeded, long FreedBytes, string? Message)
{
    /// <summary>
    /// Gets a value indicating whether the measurement showed any release at all.
    /// </summary>
    public bool ReleasedAnything => FreedBytes > 0;

    /// <summary>
    /// Builds a failure result.
    /// </summary>
    /// <param name="message">The reason.</param>
    /// <returns>The result.</returns>
    public static ReclaimResult Failure(string message) => new(false, 0, message);
}
