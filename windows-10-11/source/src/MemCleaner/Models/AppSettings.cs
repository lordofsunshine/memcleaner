namespace MemCleaner.Models;

public sealed class AppSettings
{
    public bool AutoCleanEnabled { get; set; }
    public bool UseInterval { get; set; } = true;
    public int IntervalMinutes { get; set; } = 30;
    public bool UseThreshold { get; set; } = true;
    public int ThresholdPercent { get; set; } = 85;
    public int CooldownMinutes { get; set; } = 10;
    public CleanupArea Areas { get; set; } = CleanupArea.WorkingSets | CleanupArea.StandbyList | CleanupArea.SystemFileCache;
    public string ExcludedProcesses { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool NotifyOnClean { get; set; } = true;
    public bool HotkeyEnabled { get; set; } = true;
    public string Language { get; set; } = "system";

    public static AppSettings CreateDefault() => new();

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void CopyFrom(AppSettings source)
    {
        AutoCleanEnabled = source.AutoCleanEnabled;
        UseInterval = source.UseInterval;
        IntervalMinutes = source.IntervalMinutes;
        UseThreshold = source.UseThreshold;
        ThresholdPercent = source.ThresholdPercent;
        CooldownMinutes = source.CooldownMinutes;
        Areas = source.Areas;
        ExcludedProcesses = source.ExcludedProcesses ?? string.Empty;
        StartWithWindows = source.StartWithWindows;
        MinimizeToTray = source.MinimizeToTray;
        NotifyOnClean = source.NotifyOnClean;
        HotkeyEnabled = source.HotkeyEnabled;
        Language = source.Language ?? "system";
    }

    public void Clamp()
    {
        IntervalMinutes = Math.Clamp(IntervalMinutes, 1, 180);
        ThresholdPercent = Math.Clamp(ThresholdPercent, 40, 99);
        CooldownMinutes = Math.Clamp(CooldownMinutes, 1, 120);
        ExcludedProcesses = ExcludedProcesses ?? string.Empty;
        Language = Language?.ToLowerInvariant() is "en" or "ru" ? Language.ToLowerInvariant() : "system";
    }
}
