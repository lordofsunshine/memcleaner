using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MemCleaner.Models;
using MemCleaner.Services;

namespace MemCleaner.Localization;

public sealed class Loc : INotifyPropertyChanged
{
    private static readonly string[] CisLanguages =
    {
        "ru", "uk", "be", "kk", "ky", "uz", "tg", "tk", "mo",
        "tt", "ba", "cv", "ce", "os", "ab", "av", "kbd", "sah"
    };

    private readonly Dictionary<string, string> _english;
    private readonly Dictionary<string, string> _russian;
    private Dictionary<string, string> _active;

    private Loc()
    {
        _english = Load("en");
        _russian = Load("ru");
        _active = _english;
        Culture = CultureInfo.GetCultureInfo("en-US");
        ApplyCore(AppLanguage.System);
    }

    public static Loc Current { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture { get; private set; }

    public string LanguageCode { get; private set; } = "en";

    public string this[string key]
    {
        get
        {
            if (_active.TryGetValue(key, out var value))
            {
                return value;
            }

            return _english.TryGetValue(key, out var fallback) ? fallback : key;
        }
    }

    public static string T(string key) => Current[key];

    public static string F(string key, params object?[] arguments)
        => string.Format(Current.Culture, Current[key], arguments);

    public static void Apply(AppLanguage language) => Current.ApplyCore(language);

    private void ApplyCore(AppLanguage language)
    {
        var system = CultureInfo.CurrentUICulture;
        var useRussian = language == AppLanguage.Russian
                         || (language == AppLanguage.System && IsCisLanguage(system));

        LanguageCode = useRussian ? "ru" : "en";
        Culture = CultureInfo.GetCultureInfo(useRussian ? "ru-RU" : "en-US");
        _active = useRussian ? _russian : _english;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
    }

    public static AppLanguage ParseSetting(string? value) => value?.ToLowerInvariant() switch
    {
        "en" => AppLanguage.English,
        "ru" => AppLanguage.Russian,
        _ => AppLanguage.System
    };

    public static bool IsCisLanguage(CultureInfo culture)
        => CisLanguages.Contains(culture.TwoLetterISOLanguageName, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> Load(string language)
    {
        try
        {
            var assembly = typeof(Loc).Assembly;
            var name = "MemCleaner.Localization.Lang." + language + ".json";

            using var stream = assembly.GetManifestResourceStream(name);

            if (stream is null)
            {
                LogService.Warn("Файл локализации не найден: " + name);
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            using var reader = new StreamReader(stream);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());

            return values is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(values, StringComparer.Ordinal);
        }
        catch (Exception error)
        {
            LogService.Error("Не удалось загрузить файл локализации " + language, error);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
