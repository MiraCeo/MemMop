using System.Runtime.InteropServices;

namespace MemMop.Core;

/// <summary>
/// One reading of physical and committed memory.
/// </summary>
public sealed class MemorySnapshot
{
    private const int SizeOfSizeT = 8;

    /// <summary>Gets the total physical memory in bytes.</summary>
    public long Total { get; private init; }

    /// <summary>Gets the available physical memory in bytes (free + standby, as Windows reports it).</summary>
    public long Available { get; private init; }

    /// <summary>Gets the active pages in bytes.</summary>
    public long Active { get; private init; }

    /// <summary>Gets the modified (dirty) pages in bytes.</summary>
    public long Modified { get; private init; }

    /// <summary>Gets the standby (cached) pages in bytes.</summary>
    public long Standby { get; private init; }

    /// <summary>Gets the free (zeroed) pages in bytes.</summary>
    public long Free { get; private init; }

    /// <summary>Gets the commit limit in bytes.</summary>
    public long CommitLimit { get; private init; }

    /// <summary>Gets the committed memory in bytes.</summary>
    public long CommitUsed { get; private init; }

    /// <summary>Gets a value indicating whether the four-way breakdown could be read and cross-checked.</summary>
    public bool HasSegments { get; private init; }

    /// <summary>
    /// Reads the current memory state.
    /// </summary>
    /// <returns>A snapshot; segments are marked unavailable when the kernel reply does not add up.</returns>
    public static MemorySnapshot Capture()
    {
        var status = new NativeMethods.MemoryStatusEx();
        status.Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>();

        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
            return new MemorySnapshot();

        var performance = new NativeMethods.PerformanceInformation();
        var pageSize = 4096L;
        long commitLimit = 0;
        long commitUsed = 0;

        if (NativeMethods.GetPerformanceInfo(out performance, (uint)Marshal.SizeOf<NativeMethods.PerformanceInformation>()))
        {
            pageSize = performance.PageSize.ToInt64();
            commitLimit = performance.CommitLimit.ToInt64() * pageSize;
            commitUsed = performance.CommitTotal.ToInt64() * pageSize;
        }

        var total = (long)status.TotalPhysical;
        var available = (long)status.AvailablePhysical;

        var lists = ReadLists(available, pageSize);

        if (lists == null)
        {
            return new MemorySnapshot
            {
                Total = total,
                Available = available,
                CommitLimit = commitLimit,
                CommitUsed = commitUsed,
                HasSegments = false,
            };
        }

        var (free, standby, modified) = lists.Value;

        return new MemorySnapshot
        {
            Total = total,
            Available = available,
            Active = total - free - standby - modified,
            Modified = modified,
            Standby = standby,
            Free = free,
            CommitLimit = commitLimit,
            CommitUsed = commitUsed,
            HasSegments = true,
        };
    }

    /// <summary>
    /// Reads free / standby / modified from the kernel page lists and validates them.
    /// </summary>
    /// <param name="available">Available physical memory, used for the cross-check.</param>
    /// <param name="pageSize">The system page size.</param>
    /// <returns>The three figures, or null when the reply cannot be trusted.</returns>
    private static (long Free, long Standby, long Modified)? ReadLists(long available, long pageSize)
    {
        var buffer = IntPtr.Zero;

        try
        {
            // the reply is 22 slots on current builds, not the documented 21; read only the front
            var length = SizeOfSizeT * 32;

            buffer = Marshal.AllocHGlobal(length);

            if (NativeMethods.NtQuerySystemInformation(NativeMethods.SystemMemoryListInformation, buffer, (uint)length, out _) != 0)
                return null;

            var zero = Marshal.ReadIntPtr(buffer, 0 * SizeOfSizeT).ToInt64();
            var free = Marshal.ReadIntPtr(buffer, 1 * SizeOfSizeT).ToInt64();
            var modified = Marshal.ReadIntPtr(buffer, 2 * SizeOfSizeT).ToInt64();

            long standby = 0;

            for (var i = 5; i < 13; i++)
                standby += Marshal.ReadIntPtr(buffer, i * SizeOfSizeT).ToInt64();

            var freeBytes = (zero + free) * pageSize;
            var standbyBytes = standby * pageSize;
            var modifiedBytes = modified * pageSize;

            // free + standby must approximate what GlobalMemoryStatusEx reports as available;
            // a wider gap means the struct layout is off and the numbers would be fiction
            if (available > 0 && Math.Abs(freeBytes + standbyBytes - available) > available / 20)
                return null;

            return (freeBytes, standbyBytes, modifiedBytes);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }
}
