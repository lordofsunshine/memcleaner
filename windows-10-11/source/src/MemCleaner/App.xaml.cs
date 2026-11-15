using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MemCleaner.Localization;
using MemCleaner.Models;
using MemCleaner.Services;
using MemCleaner.ViewModels;

namespace MemCleaner;

public partial class App : Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _activateEvent;
    private TrayService? _tray;
    private HotkeyService? _hotkey;
    private MainWindow? _window;
    private Thread? _listener;
    private volatile bool _exiting;
    private bool _errorReported;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterExceptionHandlers();

        var startHidden = e.Args.Any(argument => string.Equals(argument, "--tray", StringComparison.OrdinalIgnoreCase));
        var settingsService = new SettingsService();
        Loc.Apply(Loc.ParseSetting(settingsService.Load().Language));

        var mutexName = BuildInstanceName();
        _activateEvent = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, BuildEventName(), out _, BuildEventSecurity());

        if (!TryCreateMutex(mutexName))
        {
            SignalActivation();
            Shutdown();
            return;
        }

        if (!ElevationService.IsElevated())
        {
            ReleaseHandoff();

            var arguments = startHidden ? "--elevated --tray" : "--elevated";

            if (ElevationService.TryStartElevated(arguments))
            {
                Shutdown();
                return;
            }

            MessageBox.Show(
                Loc.T("adminRequired"),
                "memcleaner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Shutdown();
            return;
        }

        var probe = new MemoryProbe();
        var historyService = new HistoryService();
        var cleaner = new MemoryCleanerService(probe);
        var autoClean = new AutoCleanService();
        var autostart = new AutostartService();
        var viewModel = new MainViewModel(probe, cleaner, settingsService, historyService, autoClean, autostart);

        _window = new MainWindow { DataContext = viewModel };
        MainWindow = _window;
        new WindowInteropHelper(_window).EnsureHandle();
        WindowAppearance.ApplyDarkFrame(_window);

        _window.IsVisibleChanged += (_, _) => UpdateUiVisibility(viewModel);
        _window.StateChanged += (_, _) => UpdateUiVisibility(viewModel);

        _tray = new TrayService();
        _tray.OpenRequested += ShowWindow;
        _tray.CleanRequested += () => _ = viewModel.RunCleanAsync(CleanupTrigger.Manual);
        _tray.SettingsRequested += () =>
        {
            ShowWindow();
            viewModel.Navigate("settings");
        };
        _tray.ExitRequested += ExitApplication;
        _tray.AutoCleanToggled += () => viewModel.IsAutoEnabled = !viewModel.IsAutoEnabled;
        _tray.AutoCleanChecked = viewModel.IsAutoEnabled;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.IsAutoEnabled))
            {
                _tray.AutoCleanChecked = viewModel.IsAutoEnabled;
            }
        };
        viewModel.TooltipUpdated += (_, text) => _tray.UpdateTooltip(text);
        viewModel.NotificationRequested += (_, message) => _tray.Notify(message);
        viewModel.LanguageChanged += (_, _) => _tray.RefreshLanguage();

        _hotkey = new HotkeyService(_window, () => _ = viewModel.RunCleanAsync(CleanupTrigger.Manual));
        var hotkeyRegistered = _hotkey.SetEnabled(viewModel.ActiveSettings.HotkeyEnabled);
        viewModel.SetHotkeyRegistered(!viewModel.ActiveSettings.HotkeyEnabled || hotkeyRegistered);

        viewModel.SettingsApplied += (_, _) =>
        {
            var enabled = viewModel.ActiveSettings.HotkeyEnabled;
            var registered = _hotkey?.SetEnabled(enabled) ?? false;
            viewModel.SetHotkeyRegistered(!enabled || registered);
        };

        StartActivationListener();
        UpdateUiVisibility(viewModel);

        if (!startHidden)
        {
            _window.Show();
            UpdateUiVisibility(viewModel);
        }

        LogService.Info("memcleaner started");
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        if (_window is not null)
        {
            _window.ForceClose = true;
        }

        base.OnSessionEnding(e);
    }

    private bool TryCreateMutex(string name)
    {
        try
        {
            _mutex = MutexAcl.Create(true, name, out var created, BuildMutexSecurity());

            if (created)
            {
                return true;
            }

            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    private void ReleaseHandoff()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _mutex?.Dispose();
        _mutex = null;
    }

    private void SignalActivation()
    {
        try
        {
            _activateEvent?.Set();
        }
        catch (Exception error) when (error is ObjectDisposedException or UnauthorizedAccessException)
        {
            LogService.Warn("Could not signal the running instance: " + error.Message);
        }
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogService.Error("Unhandled interface error", args.Exception);
            args.Handled = true;

            if (_errorReported)
            {
                return;
            }

            _errorReported = true;
            MessageBox.Show(
                Loc.T("unexpectedError"),
                "memcleaner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            LogService.Error("Unhandled error", args.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogService.Error("Background task failed", args.Exception);
            args.SetObserved();
        };
    }

    private void UpdateUiVisibility(MainViewModel viewModel)
    {
        if (_window is null)
        {
            return;
        }

        viewModel.SetUiVisible(_window.IsVisible && _window.WindowState != WindowState.Minimized);
    }

    private void StartActivationListener()
    {
        _listener = new Thread(() =>
        {
            while (!_exiting)
            {
                try
                {
                    if (_activateEvent is null || !_activateEvent.WaitOne(500))
                    {
                        continue;
                    }

                    if (_exiting)
                    {
                        break;
                    }

                    Dispatcher.BeginInvoke(ShowWindow);
                }
                catch (Exception error) when (error is ObjectDisposedException or TaskCanceledException or InvalidOperationException)
                {
                    break;
                }
            }
        })
        {
            IsBackground = true
        };

        _listener.Start();
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
    }

    private void ExitApplication()
    {
        if (_window is not null)
        {
            _window.ForceClose = true;
            _window.Close();
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _exiting = true;
        SignalActivation();
        _listener?.Join(600);
        _hotkey?.Dispose();
        _tray?.Dispose();
        _activateEvent?.Dispose();

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _mutex?.Dispose();
        LogService.Info("memcleaner stopped");
        base.OnExit(e);
    }

    private static MutexSecurity BuildMutexSecurity()
    {
        var security = new MutexSecurity();
        var user = WindowsIdentity.GetCurrent().User;

        if (user is not null)
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

        if (user is not null)
        {
            security.AddAccessRule(new EventWaitHandleAccessRule(user, EventWaitHandleRights.FullControl, AccessControlType.Allow));
        }

        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        security.AddAccessRule(new EventWaitHandleAccessRule(system, EventWaitHandleRights.FullControl, AccessControlType.Allow));
        return security;
    }

    private static string BuildInstanceName() => "Local\\MemCleaner.Instance." + UserKey();

    private static string BuildEventName() => "Local\\MemCleaner.Activate." + UserKey();

    private static string UserKey()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? "default";
        }
        catch (Exception)
        {
            return "default";
        }
    }
}
