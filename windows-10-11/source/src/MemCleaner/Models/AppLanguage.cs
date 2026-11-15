namespace MemCleaner.Models;

public enum AppLanguage
{
    System,
    English,
    Russian
}

public static class AppLanguageExtensions
{
    public static AppLanguage Parse(string? value) => value?.ToLowerInvariant() switch
    {
        "en" => AppLanguage.English,
        "ru" => AppLanguage.Russian,
        _ => AppLanguage.System
    };

    public static string ToSetting(this AppLanguage language) => language switch
    {
        AppLanguage.English => "en",
        AppLanguage.Russian => "ru",
        _ => "system"
    };
}
