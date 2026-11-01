using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Writes modified (dirty) pages back to disk so they can move to standby. This is the slow one - it waits
/// on the disk - and the only honest way to report it is to say how long it took.
/// </summary>
public sealed class ModifiedPageListReclaimer : IMemoryReclaimer
{
    /// <inheritdoc/>
    public string Name => "ModifiedPageList";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        if (!Privileges.TryEnable(NativeMethods.SeProfileSingleProcess, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;
        var buffer = Marshal.AllocHGlobal(sizeof(int));

        try
        {
            Marshal.WriteInt32(buffer, NativeMethods.MemoryFlushModifiedList);

            var status = NativeMethods.NtSetSystemInformation(NativeMethods.SystemMemoryListInformation, buffer, sizeof(int));

            if (status != 0)
                return ReclaimResult.Failure(new Win32Exception(status).Message);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return new ReclaimResult(true, MemorySnapshot.Capture().Available - before, null);
    }
}
