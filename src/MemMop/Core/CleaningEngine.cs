namespace MemMop.Core;

/// <summary>
/// Runs reclaimers and collects their results. It does not decide which ones to run - that is the policy
/// layer's job - and it does not know whether the run was manual or automatic.
/// </summary>
public sealed class CleaningEngine
{
    private readonly IReadOnlyDictionary<string, IMemoryReclaimer> _reclaimers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleaningEngine"/> class.
    /// </summary>
    /// <param name="reclaimers">The available reclaimers, keyed by name.</param>
    public CleaningEngine(IEnumerable<IMemoryReclaimer> reclaimers)
    {
        _reclaimers = reclaimers.ToDictionary(r => r.Name, r => r);
    }

    /// <summary>
    /// Runs the requested areas in order.
    /// </summary>
    /// <param name="areas">The area names to run.</param>
    /// <returns>One result per known area, in the order requested.</returns>
    public IReadOnlyList<(string Name, ReclaimResult Result)> Run(IEnumerable<string> areas)
    {
        var results = new List<(string, ReclaimResult)>();

        foreach (var area in areas)
        {
            if (!_reclaimers.TryGetValue(area, out var reclaimer))
                continue;

            try
            {
                results.Add((area, reclaimer.Reclaim()));
            }
            catch (Exception e)
            {
                results.Add((area, ReclaimResult.Failure(e.Message)));
            }
        }

        return results;
    }
}
