using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MemMop.Services;

/// <summary>
/// A system-wide hotkey. Registration is attempted once and the outcome reported, because a hotkey that silently
/// belongs to another program would be worse than none.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int Id = 0x4D4D;

    private HwndSource? _source;
    private bool _registered;

    /// <summary>Raised when the hotkey is pressed.</summary>
    public event Action? Pressed;

    /// <summary>
    /// Registers the hotkey on a window.
    /// </summary>
    /// <param name="window">The window that receives the message.</param>
    /// <param name="modifiers">MOD_* flags.</param>
    /// <param name="key">A virtual-key code.</param>
    /// <param name="error">Why it failed, when it did.</param>
    /// <returns><c>true</c> when the hotkey belongs to this process afterwards.</returns>
    public bool Register(Window window, uint modifiers, uint key, out string? error)
    {
        error = null;
        Unregister();

        var handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
        {
            error = "窗口尚未创建";

            return false;
        }

        if (!NativeMethods.RegisterHotKey(handle, Id, modifiers, key))
        {
            error = "快捷键已被其他程序占用";

            return false;
        }

        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(Hook);
        _registered = true;

        return true;
    }

    /// <summary>
    /// Releases the hotkey.
    /// </summary>
    public void Unregister()
    {
        if (!_registered)
            return;

        try
        {
            var handle = _source?.Handle ?? IntPtr.Zero;

            if (handle != IntPtr.Zero)
                NativeMethods.UnregisterHotKey(handle, Id);

            _source?.RemoveHook(Hook);
        }
        catch (Exception)
        {
            // releasing is best effort; the process is going away anyway
        }

        _source = null;
        _registered = false;
    }

    /// <inheritdoc/>
    public void Dispose() => Unregister();

    private IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == Id)
        {
            Pressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(IntPtr window, int id);
    }
}
