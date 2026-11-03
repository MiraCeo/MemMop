namespace MemMop.Core;

/// <summary>
/// What one clean did, area by area. The window shows the last run; the statistics page keeps a list of
/// them, which is the only place the user can check what automatic cleaning has been doing.
/// </summary>
public sealed class CleanRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CleanRecord"/> class.
    /// </summary>
    /// <param name="at">When the run finished.</param>
    /// <param name="automatic">Whether the policy started it rather than the user.</param>
    /// <param name="duration">How long it took.</param>
    /// <param name="areas">What each area released, in the order they ran.</param>
    public CleanRecord(DateTime at, bool automatic, TimeSpan duration, IReadOnlyList<(string Area, long Freed)> areas)
    {
        At = at;
        Automatic = automatic;
        Duration = duration;
        Areas = areas;
    }

    /// <summary>Gets when the run finished.</summary>
    public DateTime At { get; }

    /// <summary>Gets a value indicating whether the policy started the run.</summary>
    public bool Automatic { get; }

    /// <summary>Gets how long the run took.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets what each area released, in bytes.</summary>
    public IReadOnlyList<(string Area, long Freed)> Areas { get; }

    /// <summary>Gets the total released, counting only the areas that released something.</summary>
    public long TotalFreed
    {
        get
        {
            var total = 0L;

            foreach (var (_, freed) in Areas)
                total += Math.Max(0, freed);

            return total;
        }
    }
}
