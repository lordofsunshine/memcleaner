using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MemCleaner.Infrastructure;
using MemCleaner.Localization;
using MemCleaner.Models;
using MemCleaner.Services;

namespace MemCleaner.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private static readonly SolidColorBrush AccentBrush = Frozen(0x3D, 0xDC, 0x84);
    private static readonly SolidColorBrush WarningBrush = Frozen(0xE2, 0xB4, 0x5F);
    private static readonly SolidColorBrush MutedBrush = Frozen(0x94, 0xA3, 0x9B);

    private readonly MemoryProbe _probe;
    private readonly MemoryCleanerService _cleaner;
    private readonly SettingsService _settingsService;
    private readonly HistoryService _historyService;
    private readonly AutoCleanService _autoClean;
    private readonly AutostartService _autostart;
    private readonly List<double> _samples = new();
    private readonly List<CleanupRecord> _records = new();
    private readonly AnimatedChannel _percentChannel = new();
    private readonly AnimatedChannel _usedChannel = new();
    private readonly AnimatedChannel _freeChannel = new();
    private readonly AnimatedChannel _cachedChannel = new();
    private readonly AnimatedChannel _commitChannel = new();
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _saveTimer;
    private readonly DispatcherTimer _statusTimer;

    private bool _isSettingsPage;
    private bool _isBusy;
    private bool _uiVisible;
    private bool _showStatus;
    private bool _channelsReady;
    private bool _lastSaveSucceeded = true;
    private bool _autostartApplied;
    private double _commitLimit;
    private string _statusText = string.Empty;
    private Brush _statusBrush = MutedBrush;
    private string _lastTooltip = string.Empty;
    private double _usedPercent;
    private double[] _samplesView = Array.Empty<double>();
    private string _usedPercentText = "0%";
    private string _usedText = "0";
    private string _freeText = "0";
    private string _cachedText = "0";
    private string _totalText = "0";
    private string _commitText = "0";
    private string _lastResult = string.Empty;
    private Brush _lastResultBrush = MutedBrush;
    private bool _hasLastResult;

    public MainViewModel(
        MemoryProbe probe,
        MemoryCleanerService cleaner,
        SettingsService settingsService,
        HistoryService historyService,
        AutoCleanService autoClean,
        AutostartService autostart)
    {
        _probe = probe;
        _cleaner = cleaner;
        _settingsService = settingsService;
        _historyService = historyService;
        _autoClean = autoClean;
        _autostart = autostart;

        ActiveSettings = settingsService.Load();
        Loc.Apply(Loc.ParseSetting(ActiveSettings.Language));
        _autoClean.Settings = ActiveSettings;

        Settings = new SettingsViewModel(ActiveSettings);
        Settings.Changed += OnSettingsChanged;
        Settings.LanguageChanged += OnLanguageChanged;

        History = new ObservableCollection<HistoryRow>();
        _records.AddRange(historyService.Load());
        RebuildHistory();

        CleanCommand = new AsyncRelayCommand(() => RunCleanAsync(CleanupTrigger.Manual), () => !IsBusy);
        NavigateCommand = new RelayCommand(Navigate);
        ResetSettingsCommand = new RelayCommand(_ => Settings.ResetToDefaults());

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();

        _animationTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _animationTimer.Tick += (_, _) => StepAnimation();

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            PersistSettings();
        };

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3200) };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            ShowStatus = false;
        };

        Refresh();

        _autostartApplied = ActiveSettings.StartWithWindows;
    }

    public event EventHandler? SettingsApplied;
    public event EventHandler<string>? NotificationRequested;
    public event EventHandler<string>? TooltipUpdated;
    public event EventHandler? LanguageChanged;

    public AppSettings ActiveSettings { get; }
    public SettingsViewModel Settings { get; }
    public ObservableCollection<HistoryRow> History { get; }
    public ICommand CleanCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand ResetSettingsCommand { get; }

    public bool IsOverviewPage => !_isSettingsPage;

    public bool IsSettingsPage
    {
        get => _isSettingsPage;
        private set
        {
            if (SetProperty(ref _isSettingsPage, value))
            {
                OnPropertyChanged(nameof(IsOverviewPage));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CleanButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string CleanButtonText => Loc.T(IsBusy ? "cleanButtonBusy" : "cleanButton");

    public bool ShowStatus
    {
        get => _showStatus;
        private set => SetProperty(ref _showStatus, value);
    }

    public string SaveStatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public Brush SaveStatusBrush
    {
        get => _statusBrush;
        private set => SetProperty(ref _statusBrush, value);
    }

    public double UsedPercent
    {
        get => _usedPercent;
        private set => SetProperty(ref _usedPercent, value);
    }

    public double[] Samples
    {
        get => _samplesView;
        private set => SetProperty(ref _samplesView, value);
    }

    public string UsedPercentText
    {
        get => _usedPercentText;
        private set => SetProperty(ref _usedPercentText, value);
    }

    public string UsedText
    {
        get => _usedText;
        private set => SetProperty(ref _usedText, value);
    }

    public string FreeText
    {
        get => _freeText;
        private set => SetProperty(ref _freeText, value);
    }

    public string CachedText
    {
        get => _cachedText;
        private set => SetProperty(ref _cachedText, value);
    }

    public string TotalText
    {
        get => _totalText;
        private set => SetProperty(ref _totalText, value);
    }

    public string CommitText
    {
        get => _commitText;
        private set => SetProperty(ref _commitText, value);
    }

    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    public Brush LastResultBrush
    {
        get => _lastResultBrush;
        private set => SetProperty(ref _lastResultBrush, value);
    }

    public bool HasLastResult
    {
        get => _hasLastResult;
        private set => SetProperty(ref _hasLastResult, value);
    }

    public bool IsAutoEnabled
    {
        get => ActiveSettings.AutoCleanEnabled;
        set
        {
            if (ActiveSettings.AutoCleanEnabled == value)
            {
                return;
            }

            ActiveSettings.AutoCleanEnabled = value;

            if (value && !ActiveSettings.UseInterval && !ActiveSettings.UseThreshold)
            {
                ActiveSettings.UseInterval = true;
                ActiveSettings.IntervalMinutes = 30;
            }

            Settings.RefreshFromModel();
            OnPropertyChanged();
            OnPropertyChanged(nameof(AutoSummary));
            _saveTimer.Stop();
            PersistSettings();
        }
    }

    public string AutoSummary
    {
        get
        {
            if (!ActiveSettings.AutoCleanEnabled)
            {
                return Loc.T("autoOff");
            }

            var parts = new List<string>();

            if (ActiveSettings.UseInterval)
            {
                parts.Add(Loc.F("autoEvery", ActiveSettings.IntervalMinutes));
            }

            if (ActiveSettings.UseThreshold)
            {
                parts.Add(Loc.F("autoAtThreshold", ActiveSettings.ThresholdPercent));
            }

            if (parts.Count == 0)
            {
                return Loc.T("autoNone");
            }

            var summary = Humanize.Join(parts);
            return char.ToUpper(summary[0], Loc.Current.Culture) + summary[1..];
        }
    }

    public string AutoDetails
    {
        get
        {
            if (History.Count == 0)
            {
                return Loc.T("autoLastNone");
            }

            var last = History[0];
            return Loc.F("autoLast", last.Time, last.Freed);
        }
    }

    public string AreasText => Humanize.Areas(ActiveSettings.Areas);

    public string AreasLine => Loc.F("overviewAreasPrefix", AreasText);

    public string TotalLabel => Humanize.TotalOf(TotalText);

    public bool HasHistory => History.Count > 0;

    public bool MinimizeToTray => ActiveSettings.MinimizeToTray;

    public string VersionText => BuildVersionText();

    public void Navigate(object? parameter)
    {
        switch (parameter as string)
        {
            case "settings":
                IsSettingsPage = true;
                break;
            case "overview":
                IsSettingsPage = false;
                break;
        }
    }

    public void SetUiVisible(bool visible)
    {
        if (_uiVisible == visible)
        {
            return;
        }

        _uiVisible = visible;

        if (visible)
        {
            Refresh();
        }
        else
        {
            _animationTimer.Stop();
        }
    }

    public void SetHotkeyRegistered(bool registered)
    {
        Settings.SetHotkeyMessage(registered ? string.Empty : Loc.T("hotkeyTaken"));
    }

    public async Task RunCleanAsync(CleanupTrigger trigger)
    {
        try
        {
            await RunCleanCoreAsync(trigger);
        }
        catch (Exception error)
        {
            LogService.Error("Cleanup command failed", error);
            IsBusy = false;
            _autoClean.IsBusy = false;
        }
    }

    private async Task RunCleanCoreAsync(CleanupTrigger trigger)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _autoClean.IsBusy = true;
        HasLastResult = true;
        LastResultBrush = MutedBrush;
        LastResult = Loc.T(trigger == CleanupTrigger.Manual ? "resultCleaning" : "resultAutoStarted");

        CleanResult? result = null;
        Exception? failure = null;

        try
        {
            result = await _cleaner.CleanAsync(ActiveSettings.Areas, ActiveSettings.ExcludedProcesses);
        }
        catch (Exception error)
        {
            failure = error;
            LogService.Error("Очистка памяти завершилась ошибкой", error);
        }
        finally
        {
            _autoClean.NotifyCleaned();
            IsBusy = false;
            _autoClean.IsBusy = false;
        }

        if (failure is not null)
        {
            LastResultBrush = WarningBrush;
            LastResult = Loc.F("resultFailed", failure.Message);
            return;
        }

        if (result is null)
        {
            return;
        }

        var meaningful = result.FreedBytes >= 1024 * 1024;

        if (result.Errors.Count > 0)
        {
            LastResultBrush = WarningBrush;
            var prefix = meaningful
                ? Loc.F("resultPartial", Humanize.Bytes(result.FreedBytes))
                : Loc.T("resultPartialNoFreed");
            var rest = result.Errors.Count > 1 ? Loc.F("resultPartialMore", result.Errors.Count - 1) : string.Empty;
            LastResult = prefix + result.Errors[0] + "." + rest;
        }
        else if (!meaningful)
        {
            LastResultBrush = MutedBrush;
            LastResult = Loc.T("resultNothing");
        }
        else
        {
            LastResultBrush = AccentBrush;
            LastResult = Loc.F("resultFreed", Humanize.Bytes(result.FreedBytes), Humanize.Seconds(result.Duration.TotalSeconds));
        }

        if (meaningful)
        {
            var record = new CleanupRecord
            {
                Timestamp = DateTime.Now,
                FreedBytes = result.FreedBytes,
                DurationSeconds = result.Duration.TotalSeconds,
                Trigger = trigger
            };

            _historyService.Append(record);
            _records.Insert(0, record);

            while (_records.Count > 40)
            {
                _records.RemoveAt(_records.Count - 1);
            }

            RebuildHistory();
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(AutoDetails));
        }

        if (trigger != CleanupTrigger.Manual
            && ActiveSettings.NotifyOnClean
            && result.FreedBytes >= 32UL * 1024 * 1024)
        {
            NotificationRequested?.Invoke(this, Loc.F("notifyFreed", Humanize.Bytes(result.FreedBytes)));
        }

        Refresh();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAutoEnabled));
        OnPropertyChanged(nameof(AutoSummary));
        OnPropertyChanged(nameof(AreasText));
        OnPropertyChanged(nameof(AreasLine));
        OnPropertyChanged(nameof(MinimizeToTray));
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Loc.Apply(Loc.ParseSetting(ActiveSettings.Language));
        RebuildHistory();
        ApplyAnimatedTexts();
        Settings.RefreshFromModel();
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(string.Empty);

        if (ShowStatus && SaveStatusText.Length > 0)
        {
            SaveStatusText = Loc.T(_lastSaveSucceeded ? "savedStatus" : "saveFailedStatus");
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PersistSettings()
    {
        _lastSaveSucceeded = _settingsService.Save(ActiveSettings);

        if (_lastSaveSucceeded)
        {
            SaveStatusText = Loc.T("savedStatus");
            SaveStatusBrush = AccentBrush;
            SettingsApplied?.Invoke(this, EventArgs.Empty);

            if (_autostartApplied != ActiveSettings.StartWithWindows)
            {
                _autostartApplied = ActiveSettings.StartWithWindows;
                _ = ApplyAutostartAsync();
            }
        }
        else
        {
            SaveStatusText = Loc.T("saveFailedStatus");
            SaveStatusBrush = WarningBrush;
        }

        ShowStatus = true;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private async Task ApplyAutostartAsync()
    {
        var result = await _autostart.ApplyAsync(ActiveSettings.StartWithWindows, Environment.ProcessPath);

        if (result.Success)
        {
            Settings.SetAutostartMessage(string.Empty);
            return;
        }

        Settings.SetAutostartMessage(result.Message);
        SaveStatusText = result.Message;
        SaveStatusBrush = WarningBrush;
        ShowStatus = true;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void Refresh()
    {
        try
        {
            var snapshot = _probe.Read();
            var tooltip = Loc.F(
                "tooltipLine",
                Humanize.Percent(snapshot.UsedPercent),
                Humanize.Bytes(snapshot.AvailableBytes));

            if (tooltip != _lastTooltip)
            {
                _lastTooltip = tooltip;
                TooltipUpdated?.Invoke(this, tooltip);
            }

            var trigger = _autoClean.Evaluate(snapshot);

            if (trigger.HasValue)
            {
                _ = RunCleanAsync(trigger.Value);
            }

            if (!_uiVisible)
            {
                return;
            }

            UsedPercent = snapshot.UsedPercent;
            TotalText = Humanize.Bytes(snapshot.TotalBytes);
            _commitLimit = snapshot.CommitLimitBytes;

            if (!_channelsReady)
            {
                _percentChannel.Jump(snapshot.UsedPercent);
                _usedChannel.Jump(snapshot.UsedBytes);
                _freeChannel.Jump(snapshot.AvailableBytes);
                _cachedChannel.Jump(snapshot.CachedBytes);
                _commitChannel.Jump(snapshot.CommitBytes);
                _channelsReady = true;
                ApplyAnimatedTexts();
            }
            else
            {
                var stamp = Stopwatch.GetTimestamp();
                _percentChannel.AnimateTo(snapshot.UsedPercent, stamp);
                _usedChannel.AnimateTo(snapshot.UsedBytes, stamp);
                _freeChannel.AnimateTo(snapshot.AvailableBytes, stamp);
                _cachedChannel.AnimateTo(snapshot.CachedBytes, stamp);
                _commitChannel.AnimateTo(snapshot.CommitBytes, stamp);
                _animationTimer.Start();
            }

            _samples.Add(snapshot.UsedPercent);

            if (_samples.Count > 60)
            {
                _samples.RemoveAt(0);
            }

            Samples = _samples.ToArray();
        }
        catch (Exception error)
        {
            LogService.Error("Не удалось обновить состояние памяти", error);
        }
    }

    private void StepAnimation()
    {
        var stamp = Stopwatch.GetTimestamp();
        var active = _percentChannel.Step(stamp);
        active |= _usedChannel.Step(stamp);
        active |= _freeChannel.Step(stamp);
        active |= _cachedChannel.Step(stamp);
        active |= _commitChannel.Step(stamp);

        if (active)
        {
            ApplyAnimatedTexts();
        }
        else
        {
            _animationTimer.Stop();
        }
    }

    private void ApplyAnimatedTexts()
    {
        UsedPercentText = Humanize.Percent(_percentChannel.Value);
        UsedText = Humanize.Bytes(_usedChannel.Value);
        FreeText = Humanize.Bytes(_freeChannel.Value);
        CachedText = Humanize.Bytes(_cachedChannel.Value);
        CommitText = Humanize.Bytes(_commitChannel.Value) + " " + Humanize.TotalOf(Humanize.Bytes(_commitLimit));
    }

    private void RebuildHistory()
    {
        History.Clear();

        foreach (var record in _records.Take(6))
        {
            History.Add(new HistoryRow(record));
        }
    }

    private static string BuildVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var text = version is null ? "1.0.0" : version.ToString(3);
        return Loc.F("versionLabel", text) + " · lordofsunshine";
    }

    private static SolidColorBrush Frozen(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
