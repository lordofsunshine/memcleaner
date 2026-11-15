using System.Windows;
using System.Windows.Interop;
using MemCleaner.Interop;

namespace MemCleaner.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x4D43;
    private const uint ModShift = 0x0004;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private const uint KeyM = 0x4D;

    private readonly IntPtr _window;
    private readonly HwndSource? _source;
    private readonly Action _callback;
    private bool _registered;
    private bool _wanted;

    public HotkeyService(Window window, Action callback)
    {
        _callback = callback;
        _window = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(_window);

        if (_source is not null)
        {
            _source.AddHook(WndProc);
        }
    }

    public bool IsRegistered => _registered;

    public bool SetEnabled(bool enabled)
    {
        if (enabled == _wanted)
        {
            return _registered || !enabled;
        }

        _wanted = enabled;

        if (_window == IntPtr.Zero)
        {
            return false;
        }

        if (enabled)
        {
            _registered = NativeMethods.RegisterHotKey(_window, HotkeyId, ModControl | ModShift | ModNoRepeat, KeyM);

            if (!_registered)
            {
                LogService.Warn("Could not register Ctrl+Shift+M, another program probably owns it.");
            }

            return _registered;
        }

        NativeMethods.UnregisterHotKey(_window, HotkeyId);
        _registered = false;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _callback();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_window, HotkeyId);
            _registered = false;
        }

        _source?.RemoveHook(WndProc);
    }
}
