using MemCleaner.Infrastructure;
using MemCleaner.Localization;
using MemCleaner.Models;
using MemCleaner.Services;

namespace MemCleaner.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private string _autostartMessage = string.Empty;
    private string _hotkeyMessage = string.Empty;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        UpdateValidation();
    }

    public event EventHandler? Changed;
    public event EventHandler? LanguageChanged;

    public bool AutoCleanEnabled
    {
        get => _settings.AutoCleanEnabled;
        set
        {
            if (_settings.AutoCleanEnabled == value)
            {
                return;
            }

            _settings.AutoCleanEnabled = value;

            if (value && !_settings.UseInterval && !_settings.UseThreshold)
            {
                _settings.UseInterval = true;
                _settings.IntervalMinutes = 30;
                OnPropertyChanged(nameof(UseInterval));
                OnPropertyChanged(nameof(IntervalMinutes));
            }

            OnPropertyChanged();
            UpdateValidation();
            NotifyChanged();
        }
    }

    public bool UseInterval
    {
        get => _settings.UseInterval;
        set
        {
            if (_settings.UseInterval == value)
            {
                return;
            }

            _settings.UseInterval = value;
            OnPropertyChanged();
            UpdateValidation();
            NotifyChanged();
        }
    }

    public int IntervalMinutes
    {
        get => _settings.IntervalMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 180);

            if (_settings.IntervalMinutes == clamped)
            {
                return;
            }

            _settings.IntervalMinutes = clamped;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool UseThreshold
    {
        get => _settings.UseThreshold;
        set
        {
            if (_settings.UseThreshold == value)
            {
                return;
            }

            _settings.UseThreshold = value;
            OnPropertyChanged();
            UpdateValidation();
            NotifyChanged();
        }
    }

    public int ThresholdPercent
    {
        get => _settings.ThresholdPercent;
        set
        {
            var clamped = Math.Clamp(value, 40, 99);

            if (_settings.ThresholdPercent == clamped)
            {
                return;
            }

            _settings.ThresholdPercent = clamped;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public int CooldownMinutes
    {
        get => _settings.CooldownMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 120);

            if (_settings.CooldownMinutes == clamped)
            {
                return;
            }

            _settings.CooldownMinutes = clamped;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool AreaWorkingSets
    {
        get => _settings.Areas.HasFlag(CleanupArea.WorkingSets);
        set => SetArea(CleanupArea.WorkingSets, value);
    }

    public bool AreaStandbyList
    {
        get => _settings.Areas.HasFlag(CleanupArea.StandbyList);
        set => SetArea(CleanupArea.StandbyList, value);
    }

    public bool AreaModifiedPageList
    {
        get => _settings.Areas.HasFlag(CleanupArea.ModifiedPageList);
        set => SetArea(CleanupArea.ModifiedPageList, value);
    }

    public bool AreaSystemFileCache
    {
        get => _settings.Areas.HasFlag(CleanupArea.SystemFileCache);
        set => SetArea(CleanupArea.SystemFileCache, value);
    }

    public string ExcludedProcesses
    {
        get => _settings.ExcludedProcesses;
        set
        {
            if (_settings.ExcludedProcesses == value)
            {
                return;
            }

            _settings.ExcludedProcesses = value ?? string.Empty;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows == value)
            {
                return;
            }

            _settings.StartWithWindows = value;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set
        {
            if (_settings.MinimizeToTray == value)
            {
                return;
            }

            _settings.MinimizeToTray = value;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool NotifyOnClean
    {
        get => _settings.NotifyOnClean;
        set
        {
            if (_settings.NotifyOnClean == value)
            {
                return;
            }

            _settings.NotifyOnClean = value;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public bool HotkeyEnabled
    {
        get => _settings.HotkeyEnabled;
        set
        {
            if (_settings.HotkeyEnabled == value)
            {
                return;
            }

            _settings.HotkeyEnabled = value;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public string LanguageMode
    {
        get => _settings.Language;
        set
        {
            var normalized = value?.ToLowerInvariant() is "en" or "ru" ? value.ToLowerInvariant() : "system";

            if (_settings.Language == normalized)
            {
                return;
            }

            _settings.Language = normalized;
            OnPropertyChanged();
            NotifyChanged();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string ValidationMessage { get; private set; } = string.Empty;

    public bool HasValidationError => ValidationMessage.Length > 0;

    public string AreasSummary => Humanize.Areas(_settings.Areas);

    public string AutostartMessage
    {
        get => _autostartMessage;
        private set
        {
            if (SetProperty(ref _autostartMessage, value))
            {
                OnPropertyChanged(nameof(HasAutostartMessage));
            }
        }
    }

    public bool HasAutostartMessage => AutostartMessage.Length > 0;

    public string HotkeyMessage
    {
        get => _hotkeyMessage;
        private set
        {
            if (SetProperty(ref _hotkeyMessage, value))
            {
                OnPropertyChanged(nameof(HasHotkeyMessage));
            }
        }
    }

    public bool HasHotkeyMessage => HotkeyMessage.Length > 0;

    public void SetAutostartMessage(string message) => AutostartMessage = message ?? string.Empty;

    public void SetHotkeyMessage(string message) => HotkeyMessage = message ?? string.Empty;

    public void RefreshFromModel()
    {
        OnPropertyChanged(string.Empty);
        UpdateValidation();
    }

    public void ResetToDefaults()
    {
        _settings.CopyFrom(AppSettings.CreateDefault());
        OnPropertyChanged(string.Empty);
        UpdateValidation();
        NotifyChanged();
    }

    private void SetArea(CleanupArea area, bool value)
    {
        var areas = _settings.Areas;

        if (value)
        {
            areas |= area;
        }
        else
        {
            areas &= ~area;
        }

        if (areas == _settings.Areas)
        {
            return;
        }

        _settings.Areas = areas;
        OnPropertyChanged(nameof(AreaWorkingSets));
        OnPropertyChanged(nameof(AreaStandbyList));
        OnPropertyChanged(nameof(AreaModifiedPageList));
        OnPropertyChanged(nameof(AreaSystemFileCache));
        OnPropertyChanged(nameof(AreasSummary));
        UpdateValidation();
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private void UpdateValidation()
    {
        if (_settings.Areas == CleanupArea.None)
        {
            ValidationMessage = Loc.T("validationAreas");
        }
        else if (_settings.AutoCleanEnabled && !_settings.UseInterval && !_settings.UseThreshold)
        {
            ValidationMessage = Loc.T("validationTriggers");
        }
        else
        {
            ValidationMessage = string.Empty;
        }

        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(AreasSummary));
    }
}
