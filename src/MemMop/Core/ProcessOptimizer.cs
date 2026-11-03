using System.Diagnostics;

namespace MemMop.Core;

/// <summary>
/// Trims one process by name. The same call the bulk reclaimer makes, narrowed to a single name, so the
/// user can act on the one program that is actually holding memory instead of every program at once.
/// </summary>
public static class ProcessOptimizer
{
    /// <summary>
    /// Trims every process carrying the name.
    /// </summary>
    /// <param name="name">The process name, without the extension; comparison ignores case.</param>
    /// <returns>What the trim released, or why it could not run.</returns>
    public static ReclaimResult Trim(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ReclaimResult.Failure("no process name");

        if (!Privileges.TryEnable(NativeMethods.SeDebugPrivilege, out var privilegeError))
            return ReclaimResult.Failure(privilegeError);

        var before = MemorySnapshot.Capture().Available;
        var trimmed = 0;

        foreach (var process in Process.GetProcessesByName(name))
        {
            try
            {
                if (NativeMethods.EmptyWorkingSet(process.Handle))
                    trimmed++;
            }
            catch (Exception)
            {
                // protected processes refuse the handle; that is expected, not an error to surface
            }
            finally
            {
                process.Dispose();
            }
        }

        if (trimmed == 0)
            return ReclaimResult.Failure("no process could be trimmed");

        return new ReclaimResult(true, MemorySnapshot.Capture().Available - before, null);
    }
}
