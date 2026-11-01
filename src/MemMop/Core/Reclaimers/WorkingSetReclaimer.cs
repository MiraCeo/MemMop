using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Trims every process's working set. The pages are not freed - they return to standby or the page file -
/// and programs that need them again will fault them back in, which is why this is the option that users
/// can actually feel.
/// </summary>
public sealed class WorkingSetReclaimer : IMemoryReclaimer
{
    private readonly IReadOnlyCollection<string> _excluded;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkingSetReclaimer"/> class.
    /// </summary>
    /// <param name="excluded">Process names to leave alone; comparison ignores case.</param>
    public WorkingSetReclaimer(IReadOnlyCollection<string>? excluded = null)
    {
        _excluded = excluded ?? Array.Empty<string>();
    }

    /// <inheritdoc/>
    public string Name => "WorkingSet";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        if (!Privileges.TryEnable(NativeMethods.SeDebugPrivilege, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;
        var failures = 0;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (_excluded.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!NativeMethods.EmptyWorkingSet(process.Handle))
                    failures++;
            }
            catch (Exception)
            {
                // protected processes refuse the handle; that is expected, not an error to surface
                failures++;
            }
            finally
            {
                process.Dispose();
            }
        }

        var freed = MemorySnapshot.Capture().Available - before;

        // a handful of failures is normal on a desktop; report only when nothing could be trimmed
        return failures > 0 && failures >= before / 1024
            ? ReclaimResult.Failure($"{failures} process(es) could not be trimmed")
            : new ReclaimResult(true, freed, null);
    }
}
