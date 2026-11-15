using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace MemCleaner.Services;

public static class ElevationService
{
    public static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool TryStartElevated(string arguments)
    {
        var path = Environment.ProcessPath;

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(path, arguments)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = AppContext.BaseDirectory
            };

            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
