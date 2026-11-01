namespace MemMop.Core;

/// <summary>
/// One row of the process table: a name, how many processes carry it, and their combined private working set.
/// </summary>
public sealed class ProcessUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessUsage"/> class.
    /// </summary>
    /// <param name="name">The process name as Windows reports it.</param>
    /// <param name="instances">How many processes share the name.</param>
    /// <param name="privateWorkingSet">Combined private working set, in bytes.</param>
    public ProcessUsage(string name, int instances, long privateWorkingSet)
    {
        Name = name;
        Instances = instances;
        PrivateWorkingSet = privateWorkingSet;
    }

    /// <summary>Gets the process name.</summary>
    public string Name { get; }

    /// <summary>Gets how many processes carry the name.</summary>
    public int Instances { get; }

    /// <summary>Gets the combined private working set, in bytes.</summary>
    public long PrivateWorkingSet { get; }

    /// <summary>Gets the formatted size, in MB below one gigabyte.</summary>
    public string Size => PrivateWorkingSet >= 1024L * 1024 * 1024
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} GB", PrivateWorkingSet / (1024d * 1024 * 1024))
        : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0} MB", PrivateWorkingSet / (1024d * 1024));
}
