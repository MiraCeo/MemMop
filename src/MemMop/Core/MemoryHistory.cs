namespace MemMop.Core;

/// <summary>
/// A fixed-size ring of memory readings. The window already reads the counters every second; storing every
/// reading would only add noise to the line, so the caller samples less often and this keeps the last hour.
/// </summary>
public sealed class MemoryHistory
{
    private readonly MemorySample[] _samples;
    private int _next;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryHistory"/> class.
    /// </summary>
    /// <param name="capacity">How many readings to keep; the oldest is overwritten once full.</param>
    public MemoryHistory(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _samples = new MemorySample[capacity];
    }

    /// <summary>Gets how many readings are held.</summary>
    public int Count => _count;

    /// <summary>Gets how many readings fit before the oldest is overwritten.</summary>
    public int Capacity => _samples.Length;

    /// <summary>
    /// Gets the reading at an index, oldest first.
    /// </summary>
    /// <param name="index">0 is the oldest reading still held.</param>
    public MemorySample this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var start = _count == _samples.Length ? _next : 0;

            return _samples[(start + index) % _samples.Length];
        }
    }

    /// <summary>
    /// Appends a reading, dropping the oldest once full.
    /// </summary>
    /// <param name="sample">The reading.</param>
    public void Add(MemorySample sample)
    {
        _samples[_next] = sample;
        _next = (_next + 1) % _samples.Length;

        if (_count < _samples.Length)
            _count++;
    }

    /// <summary>Drops every reading.</summary>
    public void Clear()
    {
        _next = 0;
        _count = 0;
    }

    /// <summary>
    /// Copies the last <paramref name="take"/> readings, oldest first.
    /// </summary>
    /// <param name="take">How many to take; more than are held returns everything.</param>
    /// <returns>The readings.</returns>
    public IReadOnlyList<MemorySample> Tail(int take)
    {
        var count = Math.Min(take, _count);
        var result = new List<MemorySample>(count);

        for (var i = _count - count; i < _count; i++)
            result.Add(this[i]);

        return result;
    }
}
