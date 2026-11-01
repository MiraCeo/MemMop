using System.Runtime.InteropServices;

namespace MemMop.Core;

/// <summary>
/// The Windows calls this program needs.
/// </summary>
internal static class NativeMethods
{
    /// <summary>SystemMemoryListInformation - the kernel's page list counters.</summary>
    internal const int SystemMemoryListInformation = 80;

    /// <summary>Purge the whole standby list.</summary>
    internal const int MemoryPurgeStandbyList = 4;

    /// <summary>Purge only the lowest-priority standby pages.</summary>
    internal const int MemoryPurgeLowPriorityStandbyList = 5;

    /// <summary>SystemRegistryReconciliationInformation - registry hive reconciliation.</summary>
    internal const int SystemRegistryReconciliationInformation = 155;

    /// <summary>MemoryFlushModifiedList - writes dirty pages back to disk.</summary>
    internal const int MemoryFlushModifiedList = 3;

    /// <summary>SeDebugPrivilege - needed to trim other processes' working sets.</summary>
    internal const string SeDebugPrivilege = "SeDebugPrivilege";

    /// <summary>SeProfileSingleProcessPrivilege - needed to purge the standby list.</summary>
    internal const string SeProfileSingleProcess = "SeProfileSingleProcessPrivilege";

    internal const uint TokenAdjustPrivileges = 0x0020;
    internal const uint TokenQuery = 0x0008;
    internal const uint SePrivilegeEnabled = 0x00000002;

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public Luid Luid;
        public uint Attributes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPerformanceInfo(out PerformanceInformation information, uint size);

    [DllImport("ntdll.dll")]
    internal static extern int NtQuerySystemInformation(int informationClass, IntPtr buffer, uint length, out uint returned);

    [DllImport("ntdll.dll")]
    internal static extern int NtSetSystemInformation(int informationClass, IntPtr buffer, uint length);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EmptyWorkingSet(IntPtr process);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AdjustTokenPrivileges(IntPtr token, [MarshalAs(UnmanagedType.Bool)] bool disableAll, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr handle);
}
