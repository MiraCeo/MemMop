using System.Runtime.InteropServices;

namespace MemMop.Core;

/// <summary>
/// The Windows calls this program needs.
/// </summary>
internal static class NativeMethods
{
    /// <summary>SystemMemoryListInformation - the kernel's page list counters.</summary>
    internal const int SystemMemoryListInformation = 80;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PerformanceInformation
    {
        public uint Size;
        public IntPtr CommitTotal;
        public IntPtr CommitLimit;
        public IntPtr CommitPeak;
        public IntPtr PhysicalTotal;
        public IntPtr PhysicalAvailable;
        public IntPtr SystemCache;
        public IntPtr KernelTotal;
        public IntPtr KernelPaged;
        public IntPtr KernelNonpaged;
        public IntPtr PageSize;
        public uint HandleCount;
        public uint ProcessCount;
        public uint ThreadCount;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPerformanceInfo(out PerformanceInformation information, uint size);

    [DllImport("ntdll.dll")]
    internal static extern int NtQuerySystemInformation(int informationClass, IntPtr buffer, uint length, out uint returned);
}
