namespace MemMop.Core;

/// <summary>
/// What one reclaimer actually did. The point of this type is that a failure is reportable, not silent.
/// </summary>
public sealed class ReclaimResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReclaimResult"/> class.
    /// </summary>
    /// <param name="succeeded">Whether the operation was carried out.</param>
    /// <param name="freedBytes">Measured change in available memory.</param>
    /// <param name="message">Why it failed, or null on success.</param>
    public ReclaimResult(bool succeeded, long freedBytes, string? message)
    {
        Succeeded = succeeded;
        FreedBytes = freedBytes;
        Message = message;
    }

    /// <summary>Gets a value indicating whether the operation was carried out.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the measured change in available memory.</summary>
    public long FreedBytes { get; }

    /// <summary>Gets why it failed, or null on success.</summary>
    public string? Message { get; }

    /// <summary>Gets a value indicating whether the measurement showed any release at all.</summary>
    public bool ReleasedAnything => FreedBytes > 0;

    /// <summary>
    /// Builds a failure result.
    /// </summary>
    /// <param name="message">The reason.</param>
    /// <returns>The result.</returns>
    public static ReclaimResult Failure(string? message) => new ReclaimResult(false, 0, message);
}
