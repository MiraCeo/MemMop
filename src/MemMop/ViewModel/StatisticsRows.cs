using System.Globalization;
using MemMop.Core;

namespace MemMop.ViewModel;

/// <summary>
/// One row of the "last clean" card: what an area released, already formatted for display.
/// </summary>
public sealed class AreaResultRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AreaResultRow"/> class.
    /// </summary>
    /// <param name="area">The area's name.</param>
    /// <param name="freed">What it released, in bytes.</param>
    public AreaResultRow(string area, long freed)
    {
        Area = area;
        Freed = freed > 0 ? Strings.Size(freed) : Strings.Current.NoChange;
    }

    /// <summary>Gets the area's name.</summary>
    public string Area { get; }

    /// <summary>Gets what it released, formatted; the "no change" wording when nothing was measured.</summary>
    public string Freed { get; }
}

/// <summary>
/// One row of the history card: when a run happened, what started it, and what it released.
/// </summary>
public sealed class CleanRecordRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CleanRecordRow"/> class.
    /// </summary>
    /// <param name="at">When the run finished.</param>
    /// <param name="automatic">Whether the policy started it.</param>
    /// <param name="freed">What it released, in bytes.</param>
    public CleanRecordRow(DateTime at, bool automatic, long freed)
    {
        When = at.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
        Trigger = automatic ? Strings.Current.Automatic : Strings.Current.Manual;
        Freed = freed > 0 ? Strings.Size(freed) : Strings.Current.NoChange;
    }

    /// <summary>Gets when the run finished.</summary>
    public string When { get; }

    /// <summary>Gets what started the run.</summary>
    public string Trigger { get; }

    /// <summary>Gets what it released, formatted.</summary>
    public string Freed { get; }
}
