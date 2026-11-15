using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace MemCleanerLegacy
{
    internal static class L
    {
        private static readonly string[] CisLanguages =
        {
            "ru", "uk", "be", "kk", "ky", "uz", "tg", "hy", "az", "tk", "mo", "ka",
            "tt", "ba", "cv", "ce", "os", "ab", "av", "kbd", "sah"
        };

        private static readonly Dictionary<string, string> English = Load("en");
        private static readonly Dictionary<string, string> Russian = Load("ru");
        private static Dictionary<string, string> _active = English;

        public static CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        public static string Code { get; private set; }

        public static string CurrentSetting { get; private set; }

        public static void Apply(string setting)
        {
            CurrentSetting = Normalize(setting);

            var useRussian = CurrentSetting == "ru"
                || (CurrentSetting == "system" && IsCis(CultureInfo.CurrentUICulture));

            Code = useRussian ? "ru" : "en";
            Culture = CultureInfo.GetCultureInfo(useRussian ? "ru-RU" : "en-US");
            _active = useRussian ? Russian : English;
        }

        public static string T(string key)
        {
            string value;

            if (_active.TryGetValue(key, out value))
            {
                return value;
            }

            if (English.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public static string F(string key, params object[] arguments)
        {
            return string.Format(Culture, T(key), arguments);
        }

        public static string Normalize(string setting)
        {
            var value = (setting ?? string.Empty).ToLowerInvariant();
            return value == "en" || value == "ru" ? value : "system";
        }

        private static bool IsCis(CultureInfo culture)
        {
            var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            return Array.IndexOf(CisLanguages, code) >= 0;
        }

        private static Dictionary<string, string> Load(string language)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var assembly = Assembly.GetExecutingAssembly();
            var suffix = ".Lang." + language + ".ini";
            string name = null;

            foreach (var candidate in assembly.GetManifestResourceNames())
            {
                if (candidate.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = candidate;
                    break;
                }
            }

            if (name == null)
            {
                return values;
            }

            using (var stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    return values;
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var trimmed = line.Trim();

                        if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var index = trimmed.IndexOf('=');

                        if (index <= 0)
                        {
                            continue;
                        }

                        var key = trimmed.Substring(0, index).Trim();
                        var value = trimmed.Substring(index + 1).Trim();

                        if (value.Length >= 2 && value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        values[key] = value;
                    }
                }
            }

            return values;
        }
    }
}
