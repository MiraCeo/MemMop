namespace MemMop.Core;

/// <summary>
/// The last few cleans, newest first. Held in memory only: the program is a single file that writes nothing
/// beside itself, and starting to keep a log on disk is a decision of its own rather than a side effect of
/// adding a page.
/// </summary>
public sealed class CleanHistory
{
    private readonly List<CleanRecord> _records = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanHistory"/> class.
    /// </summary>
    /// <param name="capacity">How many runs to keep; the oldest is dropped once full.</param>
    public CleanHistory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        Capacity = capacity;
    }

    /// <summary>Gets how many runs are kept.</summary>
    public int Capacity { get; }

    /// <summary>Gets the runs, newest first.</summary>
    public IReadOnlyList<CleanRecord> Records => _records;

    /// <summary>Gets the most recent run, or null when nothing has run yet.</summary>
    public CleanRecord? Latest => _records.Count == 0 ? null : _records[0];

    /// <summary>
    /// Records a run, dropping the oldest once full.
    /// </summary>
    /// <param name="record">The run.</param>
    public void Add(CleanRecord record)
    {
        _records.Insert(0, record);

        while (_records.Count > Capacity)
            _records.RemoveAt(_records.Count - 1);
    }

    /// <summary>Drops every run.</summary>
    public void Clear() => _records.Clear();
}
