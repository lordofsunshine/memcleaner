using System.Windows;
using System.Windows.Interop;
using MemCleaner.Interop;

namespace MemCleaner.Services;

public static class WindowAppearance
{
    public static void ApplyDarkFrame(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        var dark = 1;

        if (NativeMethods.DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
        }

        var rounded = 2;
        NativeMethods.DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }
}
