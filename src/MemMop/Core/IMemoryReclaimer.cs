namespace MemMop.Core;

/// <summary>
/// One thing the program can do to memory. Implementations never decide whether to run - the policy layer
/// does - and they never format their result; they report measured facts.
/// </summary>
public interface IMemoryReclaimer
{
    /// <summary>Gets the stable name of the area, used for logging and settings keys.</summary>
    string Name { get; }

    /// <summary>
    /// Carries out the operation and measures what changed.
    /// </summary>
    /// <returns>The measured result.</returns>
    ReclaimResult Reclaim();
}
