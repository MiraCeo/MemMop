using System.ComponentModel;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Triggers registry hive reconciliation, which releases the in-memory cache the registry keeps.
/// Its benefit is hard to observe and it is reported as such rather than promised.
/// </summary>
public sealed class RegistryCacheReclaimer : IMemoryReclaimer
{
    /// <inheritdoc/>
    public string Name => "RegistryCache";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        var before = MemorySnapshot.Capture().Available;
        var status = NativeMethods.NtSetSystemInformation(NativeMethods.SystemRegistryReconciliationInformation, IntPtr.Zero, 0);

        if (status != 0)
            return ReclaimResult.Failure(new Win32Exception(status).Message);

        return new ReclaimResult(true, MemorySnapshot.Capture().Available - before, null);
    }
}
