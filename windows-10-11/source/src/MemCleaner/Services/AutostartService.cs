using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using MemCleaner.Localization;

namespace MemCleaner.Services;

public sealed record AutostartResult(bool Success, string Message);

public sealed class AutostartService
{
    private const string TaskArguments = "--tray";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly string SchtasksPath = Path.Combine(Environment.SystemDirectory, "schtasks.exe");

    private string _lastAppliedPath = string.Empty;
    private bool _lastApplied;

    public string TaskName { get; } = BuildTaskName();

    public async Task<AutostartResult> ApplyAsync(bool enabled, string? executablePath)
    {
        return await Task.Run(async () =>
        {
            await Gate.WaitAsync().ConfigureAwait(false);

            try
            {
                return Apply(enabled, executablePath);
            }
            finally
            {
                Gate.Release();
            }
        }).ConfigureAwait(false);
    }

    public async Task<bool> IsEnabledAsync()
    {
        return await Task.Run(() => Run("/Query /TN \"" + TaskName + "\"").Code == 0).ConfigureAwait(false);
    }

    private AutostartResult Apply(bool enabled, string? executablePath)
    {
        if (!enabled)
        {
            if (!IsTaskPresent())
            {
                _lastApplied = false;
                _lastAppliedPath = string.Empty;
                return new AutostartResult(true, string.Empty);
            }

            var delete = Run("/Delete /TN \"" + TaskName + "\" /F");

            if (delete.Code != 0)
            {
                LogService.Warn("Autostart task delete failed: " + delete.Output);
                return new AutostartResult(false, Loc.T("autostartDeleteFailed"));
            }

            _lastApplied = false;
            _lastAppliedPath = string.Empty;
            return new AutostartResult(true, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new AutostartResult(false, Loc.T("autostartNoPath"));
        }

        string path;

        try
        {
            path = Path.GetFullPath(executablePath);
        }
        catch (Exception)
        {
            return new AutostartResult(false, Loc.T("autostartBadPath"));
        }

        if (!File.Exists(path) || !IsTrustedLocation(path))
        {
            LogService.Warn("Autostart rejected for path " + path);
            return new AutostartResult(false, Loc.T("autostartRejected"));
        }

        if (_lastApplied
            && string.Equals(_lastAppliedPath, path, StringComparison.OrdinalIgnoreCase)
            && IsTaskPresent())
        {
            return new AutostartResult(true, string.Empty);
        }

        string? directory;

        try
        {
            directory = PrepareDirectory();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            LogService.Error("Autostart directory preparation failed", error);
            return new AutostartResult(false, Loc.T("autostartPrepareFailed"));
        }

        if (directory is null)
        {
            return new AutostartResult(false, Loc.T("autostartPrepareFailed"));
        }

        var file = Path.Combine(directory, "task-" + Guid.NewGuid().ToString("N") + ".xml");

        try
        {
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UnicodeEncoding(false, true)))
            {
                writer.Write(BuildTaskXml(path));
            }

            var create = Run("/Create /TN \"" + TaskName + "\" /XML \"" + file + "\" /F");

            if (create.Code != 0)
            {
                LogService.Warn("Autostart task creation failed: " + create.Output);
                return new AutostartResult(false, Loc.T("autostartCreateFailed"));
            }

            _lastApplied = true;
            _lastAppliedPath = path;
            LogService.Info("Autostart task created for " + path);
            return new AutostartResult(true, string.Empty);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            LogService.Error("Autostart task file preparation failed", error);
            return new AutostartResult(false, Loc.T("autostartPrepareFailed"));
        }
        finally
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
            }
        }
    }

    private bool IsTaskPresent() => Run("/Query /TN \"" + TaskName + "\"").Code == 0;

    private static string? PrepareDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "memcleaner",
            "tasks");

        var security = BuildDirectorySecurity();
        DirectoryInfo info;

        if (Directory.Exists(directory))
        {
            info = new DirectoryInfo(directory);

            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                LogService.Warn("Autostart directory is a reparse point, refusing to use it.");
                return null;
            }

            if (!HasTrustedOwner(info))
            {
                LogService.Warn("Autostart directory owner is not an administrator.");
                return null;
            }

            info.SetAccessControl(security);
        }
        else
        {
            info = new DirectoryInfo(directory);
            info.Create(security);

            if (!HasTrustedOwner(info))
            {
                LogService.Warn("Autostart directory owner is not an administrator.");
                return null;
            }
        }

        foreach (var stale in info.GetFiles("task-*.xml"))
        {
            try
            {
                stale.Delete();
            }
            catch (Exception)
            {
            }
        }

        return directory;
    }

    private static DirectorySecurity BuildDirectorySecurity()
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);

        var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        security.AddAccessRule(new FileSystemAccessRule(
            administrators,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            system,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        return security;
    }

    private static bool HasTrustedOwner(DirectoryInfo info)
    {
        try
        {
            var owner = info.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

            if (owner is null)
            {
                return false;
            }

            return owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                   || owner.IsWellKnown(WellKnownSidType.LocalSystemSid)
                   || owner.IsWellKnown(WellKnownSidType.NTAuthoritySid);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string BuildTaskName()
    {
        var account = WindowsIdentity.GetCurrent().Name;
        var slash = account.LastIndexOf('\\');
        var user = slash >= 0 ? account[(slash + 1)..] : account;
        var sanitized = new string(user.Where(char.IsLetterOrDigit).ToArray());

        if (sanitized.Length == 0)
        {
            sanitized = "user";
        }

        if (sanitized.Length > 48)
        {
            sanitized = sanitized[..48];
        }

        return "memcleaner-" + sanitized.ToLowerInvariant();
    }

    private static string BuildTaskXml(string executablePath)
    {
        var user = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name) ?? string.Empty;
        var command = SecurityElement.Escape(executablePath) ?? string.Empty;
        var directory = SecurityElement.Escape(Path.GetDirectoryName(executablePath)) ?? string.Empty;
        var description = SecurityElement.Escape(Loc.T("autostartTaskDescription")) ?? string.Empty;

        return $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>{description}</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <UserId>{user}</UserId>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{user}</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{command}</Command>
      <Arguments>{TaskArguments}</Arguments>
      <WorkingDirectory>{directory}</WorkingDirectory>
    </Exec>
  </Actions>
</Task>";
    }

    private static bool IsTrustedLocation(string path)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return !HasWritableAncestor(path, root);
            }
        }

        return false;
    }

    private static bool HasWritableAncestor(string path, string root)
    {
        var directory = Path.GetDirectoryName(path);

        while (!string.IsNullOrEmpty(directory)
               && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var info = new DirectoryInfo(directory);

                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }

                if (string.Equals(directory.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }
            catch (Exception)
            {
                return true;
            }
        }

        return false;
    }

    private static (int Code, string Output) Run(string arguments)
    {
        if (!File.Exists(SchtasksPath))
        {
            LogService.Warn("schtasks.exe was not found in the system directory.");
            return (-1, "schtasks.exe not found");
        }

        try
        {
            var encoding = ResolveOutputEncoding();
            var startInfo = new ProcessStartInfo(SchtasksPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding
            };

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return (-1, "schtasks did not start");
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(15000))
            {
                try
                {
                    process.Kill(true);
                }
                catch (Exception)
                {
                }

                LogService.Warn("schtasks timed out: " + arguments);
                return (-2, "timeout");
            }

            var text = (output.GetAwaiter().GetResult() + " " + error.GetAwaiter().GetResult()).Trim();
            return (process.ExitCode, text);
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or InvalidOperationException or PlatformNotSupportedException or NotSupportedException)
        {
            LogService.Error("schtasks invocation failed", exception);
            return (-1, exception.Message);
        }
    }

    private static Encoding ResolveOutputEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var codePage = CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
            return Encoding.GetEncoding(codePage);
        }
        catch (Exception)
        {
            return Encoding.UTF8;
        }
    }
}
