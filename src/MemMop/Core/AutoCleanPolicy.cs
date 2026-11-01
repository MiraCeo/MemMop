namespace MemMop.Core;

/// <summary>
/// Decides what an automatic run may touch and when it is due. The engine below it knows nothing about
/// manual versus automatic; that distinction lives here and only here.
/// </summary>
public sealed class AutoCleanPolicy
{
    /// <summary>
    /// The areas an automatic run may use when the conservative switch is on: everything that does not
    /// evict working sets or cap the system cache, because both are felt by the user while he is away.
    /// </summary>
    public static readonly IReadOnlySet<string> SafeAreas = new HashSet<string>(StringComparer.Ordinal)
    {
        "StandbyList",
        "ModifiedPageList",
        "CombinedPageList",
        "RegistryCache",
    };

    private DateTime _lastIntervalRun = DateTime.Now;

    /// <summary>
    /// Narrows the selection for the run about to happen.
    /// </summary>
    /// <param name="selected">The areas the user ticked.</param>
    /// <param name="automatic">Whether this is an automatic run.</param>
    /// <param name="safeOnly">Whether automatic runs stay conservative.</param>
    /// <returns>The areas to execute, in the order given.</returns>
    public static IReadOnlyList<string> SelectForRun(IEnumerable<string> selected, bool automatic, bool safeOnly)
    {
        var areas = selected as IReadOnlyList<string> ?? selected.ToList();

        if (!automatic || !safeOnly)
            return areas;

        return areas.Where(a => SafeAreas.Contains(a)).ToList();
    }

    /// <summary>
    /// Marks the interval as satisfied, so the next run waits for another full period.
    /// </summary>
    /// <param name="now">The current time.</param>
    public void MarkIntervalRun(DateTime now) => _lastIntervalRun = now;

    /// <summary>
    /// Gets whether the interval has elapsed.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="minutes">The configured interval; 0 means disabled.</param>
    /// <returns><c>true</c> when a run is due.</returns>
    public bool IsIntervalDue(DateTime now, int minutes)
    {
        if (minutes <= 0)
            return false;

        return (now - _lastIntervalRun).TotalMinutes >= minutes;
    }

    /// <summary>
    /// Gets whether available memory has fallen below the threshold.
    /// </summary>
    /// <param name="snapshot">The current memory state.</param>
    /// <param name="percent">The configured threshold; 0 means disabled.</param>
    /// <returns><c>true</c> when a run is due.</returns>
    public static bool IsMemoryDue(MemorySnapshot snapshot, int percent)
    {
        if (percent <= 0 || snapshot.Total <= 0)
            return false;

        var availablePercent = snapshot.Available * 100.0 / snapshot.Total;

        return availablePercent < percent;
    }
}
