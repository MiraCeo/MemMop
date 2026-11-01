using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Purges the standby list: cached file pages become free pages. Available memory barely moves, because
/// standby pages already counted as available - what changes is that the cache has to be rebuilt.
/// </summary>
public sealed class StandbyListReclaimer : IMemoryReclaimer
{
    private readonly bool _lowPriorityOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandbyListReclaimer"/> class.
    /// </summary>
    /// <param name="lowPriorityOnly">Purge only the lowest-priority pages.</param>
    public StandbyListReclaimer(bool lowPriorityOnly = false)
    {
        _lowPriorityOnly = lowPriorityOnly;
    }

    /// <inheritdoc/>
    public string Name => _lowPriorityOnly ? "StandbyListLowPriority" : "StandbyList";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        if (!Privileges.TryEnable(NativeMethods.SeProfileSingleProcess, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;
        var command = _lowPriorityOnly
            ? NativeMethods.MemoryPurgeLowPriorityStandbyList
            : NativeMethods.MemoryPurgeStandbyList;

        var buffer = Marshal.AllocHGlobal(sizeof(int));

        try
        {
            Marshal.WriteInt32(buffer, command);

            var status = NativeMethods.NtSetSystemInformation(NativeMethods.SystemMemoryListInformation, buffer, sizeof(int));

            if (status != 0)
                return ReclaimResult.Failure(new Win32Exception(status).Message);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        var freed = MemorySnapshot.Capture().Available - before;

        return new ReclaimResult(true, freed, null);
    }
}
