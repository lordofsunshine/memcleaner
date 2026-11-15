using System.Diagnostics;
using MemCleaner.Models;

namespace MemCleaner.Services;

public sealed class AutoCleanService
{
    private const double MinimumSecondsBeforeFirstClean = 45;
    private const double ThresholdHysteresis = 5;
    private const ulong MinimumReclaimableForThreshold = 128UL * 1024 * 1024;
    private const ulong MinimumReclaimableForInterval = 64UL * 1024 * 1024;

    private long _lastCleanTimestamp = Stopwatch.GetTimestamp();
    private bool _thresholdArmed = true;

    public AppSettings Settings { get; set; } = AppSettings.CreateDefault();

    public bool IsBusy { get; set; }

    public CleanupTrigger? Evaluate(MemorySnapshot snapshot)
    {
        if (!Settings.AutoCleanEnabled || IsBusy)
        {
            return null;
        }

        var elapsed = Stopwatch.GetElapsedTime(_lastCleanTimestamp);

        if (elapsed.TotalSeconds < MinimumSecondsBeforeFirstClean)
        {
            return null;
        }

        if (Settings.UseThreshold)
        {
            if (!_thresholdArmed && snapshot.UsedPercent <= Settings.ThresholdPercent - ThresholdHysteresis)
            {
                _thresholdArmed = true;
            }

            if (_thresholdArmed
                && snapshot.UsedPercent >= Settings.ThresholdPercent
                && snapshot.CachedBytes >= MinimumReclaimableForThreshold
                && elapsed.TotalMinutes >= Settings.CooldownMinutes)
            {
                _thresholdArmed = false;
                return CleanupTrigger.Threshold;
            }
        }

        if (Settings.UseInterval
            && elapsed.TotalMinutes >= Settings.IntervalMinutes
            && snapshot.CachedBytes >= MinimumReclaimableForInterval)
        {
            return CleanupTrigger.Interval;
        }

        return null;
    }

    public void NotifyCleaned()
    {
        _lastCleanTimestamp = Stopwatch.GetTimestamp();
        _thresholdArmed = false;
    }
}
