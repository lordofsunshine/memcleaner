using MemCleaner.Localization;
using MemCleaner.Models;

namespace MemCleaner.Services;

public static class Humanize
{
    public static string Bytes(double bytes)
    {
        var absolute = Math.Abs(bytes);
        var culture = Loc.Current.Culture;

        if (absolute >= 1024d * 1024 * 1024)
        {
            return Loc.F("unitGb", (bytes / (1024d * 1024 * 1024)).ToString("0.0", culture));
        }

        if (absolute >= 1024 * 1024)
        {
            return Loc.F("unitMb", (bytes / (1024d * 1024)).ToString("0", culture));
        }

        if (absolute >= 1024)
        {
            return Loc.F("unitKb", (bytes / 1024d).ToString("0", culture));
        }

        return Loc.F("unitBytes", bytes.ToString("0", culture));
    }

    public static string Percent(double value)
        => Loc.F("unitPercent", value.ToString("0", Loc.Current.Culture));

    public static string Seconds(double value)
        => Loc.F("unitSeconds", value.ToString("0.0", Loc.Current.Culture));

    public static string Minutes(int value)
        => Loc.F("unitMinutes", value.ToString(Loc.Current.Culture));

    public static string TotalOf(string total) => Loc.F("totalOf", total);

    public static string Areas(CleanupArea areas)
    {
        var parts = new List<string>();

        if (areas.HasFlag(CleanupArea.WorkingSets))
        {
            parts.Add(Loc.T("areaWorkingSets"));
        }

        if (areas.HasFlag(CleanupArea.StandbyList))
        {
            parts.Add(Loc.T("areaStandby"));
        }

        if (areas.HasFlag(CleanupArea.ModifiedPageList))
        {
            parts.Add(Loc.T("areaModified"));
        }

        if (areas.HasFlag(CleanupArea.SystemFileCache))
        {
            parts.Add(Loc.T("areaFileCache"));
        }

        return Join(parts);
    }

    public static string Join(IReadOnlyList<string> parts)
    {
        if (parts.Count == 0)
        {
            return Loc.T("areaNone");
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        var separator = Loc.T("listSeparator");
        var conjunction = Loc.T("listAnd");
        return string.Join(separator, parts.Take(parts.Count - 1)) + conjunction + parts[^1];
    }

    public static string Time(DateTime value)
    {
        var local = value.ToLocalTime();
        var culture = Loc.Current.Culture;
        var pattern = local.Date == DateTime.Today
            ? Loc.T("timeToday")
            : Loc.T("timeOther");

        return local.ToString(pattern, culture);
    }

    public static string Trigger(CleanupTrigger trigger) => trigger switch
    {
        CleanupTrigger.Threshold => Loc.T("triggerThreshold"),
        CleanupTrigger.Interval => Loc.T("triggerInterval"),
        _ => Loc.T("triggerManual")
    };
}
