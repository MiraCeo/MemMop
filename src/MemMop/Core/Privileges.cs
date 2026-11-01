using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemMop.Core;

/// <summary>
/// Enables the privileges the clean operations need. Failure is returned, never thrown.
/// </summary>
internal static class Privileges
{
    /// <summary>
    /// Enables a privilege on the current process token.
    /// </summary>
    /// <param name="name">The privilege name, for example SeProfileSingleProcessPrivilege.</param>
    /// <param name="error">Why it failed, when it did.</param>
    /// <returns><c>true</c> when the privilege is enabled afterwards.</returns>
    public static bool TryEnable(string name, out string? error)
    {
        error = null;

        if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), NativeMethods.TokenAdjustPrivileges | NativeMethods.TokenQuery, out var token))
        {
            error = new Win32Exception(Marshal.GetLastWin32Error()).Message;

            return false;
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, name, out var luid))
            {
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;

                return false;
            }

            var state = new NativeMethods.TokenPrivileges
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SePrivilegeEnabled,
            };

            if (!NativeMethods.AdjustTokenPrivileges(token, false, ref state, 0, IntPtr.Zero, IntPtr.Zero))
            {
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;

                return false;
            }

            // AdjustTokenPrivileges succeeds even when it changed nothing
            if (Marshal.GetLastWin32Error() != 0)
            {
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;

                return false;
            }

            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }
}
