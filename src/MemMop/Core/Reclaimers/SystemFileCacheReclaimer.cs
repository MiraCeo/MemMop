using System.ComponentModel;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Pushes the kernel file cache out. The effect is not one-off: the cache grows back, so the cost is paid
/// again on every file access until it does.
/// </summary>
public sealed class SystemFileCacheReclaimer : IMemoryReclaimer
{
    /// <inheritdoc/>
    public string Name => "SystemFileCache";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        if (!Privileges.TryEnable(NativeMethods.SeIncreaseQuotaPrivilege, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;

        // nine SIZE_T fields; -1 in the working-set bounds flushes rather than re-caps
        var size = sizeof(long) * 9;
        var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

        try
        {
            for (var offset = 0; offset < size; offset += sizeof(long))
                System.Runtime.InteropServices.Marshal.WriteInt64(buffer, offset, 0);

            System.Runtime.InteropServices.Marshal.WriteInt64(buffer, sizeof(long) * 3, -1);
            System.Runtime.InteropServices.Marshal.WriteInt64(buffer, sizeof(long) * 4, -1);

            var status = NativeMethods.NtSetSystemInformation(NativeMethods.SystemFileCacheInformation, buffer, (uint)size);

            if (status != 0)
                return ReclaimResult.Failure(new Win32Exception(status).Message);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }

        var flush = new IntPtr(-1);

        if (!NativeMethods.SetSystemFileCacheSize(flush, flush, 0))
            return ReclaimResult.Failure(new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()).Message);

        return new ReclaimResult(true, MemorySnapshot.Capture().Available - before, null);
    }
}
