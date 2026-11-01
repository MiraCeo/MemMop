namespace MemMop.Core;

/// <summary>
/// One row of the process table: a name, how many processes carry it, and their combined private working set.
/// </summary>
/// <param name="Name">The process name as Windows reports it.</param>
/// <param name="Instances">How many processes share the name.</param>
/// <param name="PrivateWorkingSet">Combined private working set, in bytes.</param>
public sealed record ProcessUsage(string Name, int Instances, long PrivateWorkingSet)
{
    /// <summary>
    /// Gets the formatted size.
    /// </summary>
    public string Size => PrivateWorkingSet >= 1024L * 1024 * 1024
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} GB", PrivateWorkingSet / (1024d * 1024 * 1024))
        : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0} MB", PrivateWorkingSet / (1024d * 1024));
}
