using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemMop.Core;

/// <summary>
/// Reads per-process private working sets and aggregates them by name, so the table lines up with the
/// exclusion list, which is also keyed by name.
/// </summary>
public static class ProcessUsageReader
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;
    private const uint QueryLimitedInformation = 0x1000;

    /// <summary>
    /// Reads the processes and returns the largest consumers.
    /// </summary>
    /// <param name="top">How many rows to return; 0 or less returns everything.</param>
    /// <returns>The rows, largest first.</returns>
    public static IReadOnlyList<ProcessUsage> Read(int top)
    {
        var byName = new Dictionary<string, (int Count, long Bytes)>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var bytes = PrivateWorkingSet(process);

                if (bytes <= 0)
                    continue;

                if (byName.TryGetValue(process.ProcessName, out var existing))
                    byName[process.ProcessName] = (existing.Count + 1, existing.Bytes + bytes);
                else
                    byName[process.ProcessName] = (1, bytes);
            }
            catch (Exception)
            {
                // protected processes cannot be opened; skipping them is expected
            }
            finally
            {
                process.Dispose();
            }
        }

        var rows = byName
            .Select(pair => new ProcessUsage(pair.Key, pair.Value.Count, pair.Value.Bytes))
            .OrderByDescending(row => row.PrivateWorkingSet)
            .AsEnumerable();

        return (top > 0 ? rows.Take(top) : rows).ToList();
    }

    private static long PrivateWorkingSet(Process process)
    {
        // GetProcessMemoryInfo documents QUERY_INFORMATION | VM_READ; LIMITED is the fallback for processes
        // whose full handle we are not allowed to open
        var handle = NativeMethods.OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.Id);

        if (handle == IntPtr.Zero)
            handle = NativeMethods.OpenProcess(QueryLimitedInformation, false, process.Id);

        if (handle == IntPtr.Zero)
            return 0;

        try
        {
            var counters = new NativeMethods.ProcessMemoryCounters();
            var size = (uint)Marshal.SizeOf<NativeMethods.ProcessMemoryCounters>();

            counters.Size = size;

            return NativeMethods.GetProcessMemoryInfo(handle, out counters, size)
                ? counters.PrivateWorkingSet.ToInt64()
                : 0;
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }
}
