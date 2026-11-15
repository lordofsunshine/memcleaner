using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace MemCleanerLegacy
{
    internal static class Program
    {
        private static Mutex _mutex;
        private static EventWaitHandle _activate;

        [STAThread]
        private static void Main(string[] args)
        {
            var hidden = args != null && Array.IndexOf(args, "--tray") >= 0;
            var settings = LegacySettings.Load();
            L.Apply(settings.Language);

            var user = UserKey();
            var instanceName = "Local\\memcleaner-legacy." + user;
            var eventName = "Local\\memcleaner-legacy-activate." + user;

            _activate = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                eventName,
                out _,
                BuildEventSecurity());

            _mutex = new Mutex(true, instanceName, out var first, BuildMutexSecurity());

            if (!first)
            {
                SignalActivation();
                return;
            }

            if (!IsElevated())
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }

                ((IDisposable)_mutex).Dispose();
                _mutex = null;

                var arguments = hidden ? "--elevated --tray" : "--elevated";

                if (StartElevated(arguments))
                {
                    return;
                }

                MessageBox.Show(
                    L.T("adminRequired"),
                    "memcleaner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (sender, e) => ReportFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => ReportFatal(e.ExceptionObject as Exception);

            try
            {
                Application.Run(new MainForm(settings, hidden, _activate));
            }
            finally
            {
                ((IDisposable)_activate).Dispose();

                if (_mutex != null)
                {
                    try
                    {
                        _mutex.ReleaseMutex();
                    }
                    catch (ApplicationException)
                    {
                    }

                    ((IDisposable)_mutex).Dispose();
                }
            }
        }

        private static bool IsElevated()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string UserKey()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    return identity.User == null ? "default" : identity.User.Value;
                }
            }
            catch (Exception)
            {
                return "default";
            }
        }

        private static void SignalActivation()
        {
            try
            {
                _activate.Set();
            }
            catch (Exception)
            {
            }
        }

        private static bool StartElevated(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo(Application.ExecutablePath, arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                Process.Start(info);
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private static MutexSecurity BuildMutexSecurity()
        {
            var security = new MutexSecurity();
            var user = WindowsIdentity.GetCurrent().User;

            if (user != null)
            {
                security.AddAccessRule(new MutexAccessRule(user, MutexRights.FullControl, AccessControlType.Allow));
            }

            var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            security.AddAccessRule(new MutexAccessRule(system, MutexRights.FullControl, AccessControlType.Allow));
            return security;
        }

        private static EventWaitHandleSecurity BuildEventSecurity()
        {
            var security = new EventWaitHandleSecurity();
            var user = WindowsIdentity.GetCurrent().User;

            if (user != null)
            {
                security.AddAccessRule(new EventWaitHandleAccessRule(user, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            }

            var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            security.AddAccessRule(new EventWaitHandleAccessRule(system, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            return security;
        }

        private static void ReportFatal(Exception error)
        {
            if (error != null)
            {
                try
                {
                    var folder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "memcleaner");
                    System.IO.Directory.CreateDirectory(folder);
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(folder, "legacy-log.txt"),
                        DateTime.Now.ToString("s", CultureInfo.InvariantCulture) + " " + error + Environment.NewLine);
                }
                catch (Exception)
                {
                }
            }

            MessageBox.Show(L.T("unexpectedError"), "memcleaner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
