using MemCleaner.Localization;
using MemCleaner.Models;
using MemCleaner.Services;

namespace MemCleaner.ViewModels;

public sealed class HistoryRow
{
    public HistoryRow(CleanupRecord record)
    {
        Time = Humanize.Time(record.Timestamp);
        Freed = record.FreedBytes >= 1024 * 1024 ? Humanize.Bytes(record.FreedBytes) : Loc.T("historyLessThanMb");
        Trigger = Humanize.Trigger(record.Trigger);
        IsAutomatic = record.Trigger != CleanupTrigger.Manual;
    }

    public string Time { get; }
    public string Freed { get; }
    public string Trigger { get; }
    public bool IsAutomatic { get; }
}
