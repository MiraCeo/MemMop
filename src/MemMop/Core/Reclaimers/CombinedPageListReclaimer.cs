using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemMop.Core.Reclaimers;

/// <summary>
/// Asks the kernel to combine physical pages with identical content. Nothing is deleted - the pages are
/// shared until one side writes, at which point it gets a private copy again.
/// </summary>
public sealed class CombinedPageListReclaimer : IMemoryReclaimer
{
    /// <inheritdoc/>
    public string Name => "CombinedPageList";

    /// <inheritdoc/>
    public ReclaimResult Reclaim()
    {
        if (!Privileges.TryEnable(NativeMethods.SeProfileSingleProcess, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;
        var combine = new NativeMethods.MemoryCombineInformation { Flags = 0 };

        var buffer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.MemoryCombineInformation>());

        try
        {
            Marshal.StructureToPtr(combine, buffer, false);

            var status = NativeMethods.NtSetSystemInformation(
                NativeMethods.CombinePhysicalMemoryInformation,
                buffer,
                (uint)Marshal.SizeOf<NativeMethods.MemoryCombineInformation>());

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
