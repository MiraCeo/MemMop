namespace MemMop.Core;

/// <summary>
/// One reading of the memory counters, kept so the statistics page can draw a line over time. Only the
/// numbers a chart needs are stored; the full snapshot is rebuilt on demand.
/// </summary>
public readonly struct MemorySample
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemorySample"/> struct.
    /// </summary>
    /// <param name="at">When the reading was taken.</param>
    /// <param name="used">Memory in use: total minus available.</param>
    /// <param name="standby">Standby memory, which still counts as available.</param>
    /// <param name="free">Memory that is neither in use nor cached.</param>
    public MemorySample(DateTime at, long used, long standby, long free)
    {
        At = at;
        Used = used;
        Standby = standby;
        Free = free;
    }

    /// <summary>Gets when the reading was taken.</summary>
    public DateTime At { get; }

    /// <summary>Gets the memory in use, in bytes.</summary>
    public long Used { get; }

    /// <summary>Gets the standby memory, in bytes.</summary>
    public long Standby { get; }

    /// <summary>Gets the free memory, in bytes.</summary>
    public long Free { get; }
}
