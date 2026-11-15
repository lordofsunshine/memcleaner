using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace MemCleanerLegacy
{
    internal static class Autostart
    {
        private const string TaskArguments = "--tray";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "memcleaner";

        public static string Apply(bool enabled, string executable)
        {
            if (!enabled)
            {
                return Remove();
            }

            if (string.IsNullOrEmpty(executable) || !File.Exists(executable))
            {
                return L.T("autostartNoPath");
            }

            var path = Path.GetFullPath(executable);

            if (!IsTrustedLocation(path))
            {
                return L.T("autostartRejected");
            }

            try
            {
                return Platform.IsXp ? ApplyRunKey(path) : ApplyTask(path);
            }
            catch (Exception error)
            {
                return L.F("autostartFailed", error.Message);
            }
        }

        public static string TaskName
        {
            get
            {
                var account = WindowsIdentity.GetCurrent().Name;
                var slash = account.LastIndexOf('\\');
                var user = slash >= 0 ? account.Substring(slash + 1) : account;
                var builder = new StringBuilder();

                foreach (var symbol in user)
                {
                    if (char.IsLetterOrDigit(symbol))
                    {
                        builder.Append(char.ToLowerInvariant(symbol));
                    }
                }

                var name = builder.Length == 0 ? "user" : builder.ToString();

                if (name.Length > 48)
                {
                    name = name.Substring(0, 48);
                }

                return "memcleaner-" + name;
            }
        }

        private static string Remove()
        {
            if (Platform.IsXp)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(RunValue, false);
                    }
                }

                return string.Empty;
            }

            RunSchtasks("/Delete /TN \"" + TaskName + "\" /F");
            return string.Empty;
        }

        private static string ApplyRunKey(string path)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key == null)
                {
                    return L.T("autostartFailedEmpty");
                }

                key.SetValue(RunValue, "\"" + path + "\" " + TaskArguments);
            }

            return string.Empty;
        }

        private static string ApplyTask(string path)
        {
            string directory;

            try
            {
                directory = PrepareDirectory();
            }
            catch (Exception error)
            {
                return L.F("autostartFailed", error.Message);
            }

            var file = Path.Combine(directory, "task-" + Guid.NewGuid().ToString("N") + ".xml");

            try
            {
                var bytes = BuildTaskXml(path);
                var preamble = Encoding.Unicode.GetPreamble();
                var content = Encoding.Unicode.GetBytes(bytes);
                var payload = new byte[preamble.Length + content.Length];
                Buffer.BlockCopy(preamble, 0, payload, 0, preamble.Length);
                Buffer.BlockCopy(content, 0, payload, preamble.Length, content.Length);
                File.WriteAllBytes(file, payload);
                var result = RunSchtasks("/Create /TN \"" + TaskName + "\" /XML \"" + file + "\" /F");

                if (result != 0)
                {
                    return L.T("autostartCreateFailed");
                }

                return string.Empty;
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

        private static string PrepareDirectory()
        {
            var directory = Path.Combine(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "memcleaner"),
                "tasks");

            var security = BuildDirectorySecurity();
            DirectoryInfo info;

            if (Directory.Exists(directory))
            {
                info = new DirectoryInfo(directory);

                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new IOException(L.T("autostartPrepareFailed"));
                }

                var owner = info.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

                if (!HasTrustedOwner(owner))
                {
                    throw new UnauthorizedAccessException(L.T("autostartPrepareFailed"));
                }

                info.SetAccessControl(security);
            }
            else
            {
                info = new DirectoryInfo(directory);
                info.Create(security);
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

        private static bool HasTrustedOwner(SecurityIdentifier owner)
        {
            return owner != null
                   && (owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                       || owner.IsWellKnown(WellKnownSidType.LocalSystemSid)
                       || owner.IsWellKnown(WellKnownSidType.NTAuthoritySid));
        }

        private static bool IsTrustedLocation(string path)
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)")
            };

            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(root))
                {
                    continue;
                }

                var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string BuildTaskXml(string executablePath)
        {
            var user = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name);
            var command = SecurityElement.Escape(executablePath);
            var directory = SecurityElement.Escape(Path.GetDirectoryName(executablePath));
            var description = SecurityElement.Escape(L.T("autostartTaskDescription"));

            return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\n"
                + "<Task version=\"1.4\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\n"
                + "  <RegistrationInfo>\n"
                + "    <Description>" + description + "</Description>\n"
                + "  </RegistrationInfo>\n"
                + "  <Triggers>\n"
                + "    <LogonTrigger>\n"
                + "      <Enabled>true</Enabled>\n"
                + "      <UserId>" + user + "</UserId>\n"
                + "    </LogonTrigger>\n"
                + "  </Triggers>\n"
                + "  <Principals>\n"
                + "    <Principal id=\"Author\">\n"
                + "      <UserId>" + user + "</UserId>\n"
                + "      <LogonType>InteractiveToken</LogonType>\n"
                + "      <RunLevel>HighestAvailable</RunLevel>\n"
                + "    </Principal>\n"
                + "  </Principals>\n"
                + "  <Settings>\n"
                + "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\n"
                + "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\n"
                + "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\n"
                + "    <AllowHardTerminate>true</AllowHardTerminate>\n"
                + "    <StartWhenAvailable>true</StartWhenAvailable>\n"
                + "    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>\n"
                + "    <AllowStartOnDemand>true</AllowStartOnDemand>\n"
                + "    <Enabled>true</Enabled>\n"
                + "    <Hidden>false</Hidden>\n"
                + "    <RunOnlyIfIdle>false</RunOnlyIfIdle>\n"
                + "    <WakeToRun>false</WakeToRun>\n"
                + "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\n"
                + "    <Priority>7</Priority>\n"
                + "  </Settings>\n"
                + "  <Actions Context=\"Author\">\n"
                + "    <Exec>\n"
                + "      <Command>" + command + "</Command>\n"
                + "      <Arguments>" + TaskArguments + "</Arguments>\n"
                + "      <WorkingDirectory>" + directory + "</WorkingDirectory>\n"
                + "    </Exec>\n"
                + "  </Actions>\n"
                + "</Task>";
        }

        private static int RunSchtasks(string arguments)
        {
            var path = Path.Combine(Environment.SystemDirectory, "schtasks.exe");

            if (!File.Exists(path))
            {
                return -1;
            }

            var info = new ProcessStartInfo(path, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Environment.SystemDirectory
            };

            using (var process = Process.Start(info))
            {
                if (process == null)
                {
                    return -1;
                }

                process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();

                if (!process.WaitForExit(15000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }

                    return -2;
                }

                return process.ExitCode;
            }
        }
    }
}
