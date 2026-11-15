using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MemCleanerLegacy
{
    internal sealed class LegacySettings
    {
        private static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "memcleaner");

        private static readonly string FilePath = Path.Combine(Folder, "legacy-settings.ini");

        public bool AutoEnabled;
        public bool UseInterval = true;
        public bool UseThreshold;
        public int IntervalMinutes = 30;
        public int ThresholdPercent = 80;
        public int CooldownMinutes = 5;
        public CleanAreas Areas = CleanAreas.WorkingSets | CleanAreas.Standby | CleanAreas.Modified | CleanAreas.FileCache;
        public string Language = "system";
        public bool MinimizeToTray = true;
        public bool NotifyOnClean = true;
        public bool StartWithWindows;
        public string Excluded = string.Empty;

        public static LegacySettings Load()
        {
            var settings = new LegacySettings();
            var supported = MemoryEngine.SupportedAreas;

            try
            {
                if (!File.Exists(FilePath))
                {
                    settings.Areas &= supported;
                    return settings;
                }

                var info = new FileInfo(FilePath);

                if (info.Length > 64 * 1024)
                {
                    settings.Areas &= supported;
                    return settings;
                }

                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
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
                    settings.Apply(key, value);
                }
            }
            catch (Exception)
            {
            }

            settings.IntervalMinutes = Clamp(settings.IntervalMinutes, 1, 180);
            settings.ThresholdPercent = Clamp(settings.ThresholdPercent, 40, 99);
            settings.CooldownMinutes = Clamp(settings.CooldownMinutes, 1, 120);
            settings.Language = L.Normalize(settings.Language);
            settings.Areas &= supported;

            if (settings.Areas == CleanAreas.None)
            {
                settings.Areas = CleanAreas.WorkingSets;
            }

            return settings;
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);

                var builder = new StringBuilder();
                builder.AppendLine("# memcleaner legacy settings");
                builder.AppendLine("AutoEnabled=" + (AutoEnabled ? "1" : "0"));
                builder.AppendLine("UseInterval=" + (UseInterval ? "1" : "0"));
                builder.AppendLine("UseThreshold=" + (UseThreshold ? "1" : "0"));
                builder.AppendLine("IntervalMinutes=" + IntervalMinutes.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("ThresholdPercent=" + ThresholdPercent.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("CooldownMinutes=" + CooldownMinutes.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("Areas=" + ((int)Areas).ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("Language=" + Language);
                builder.AppendLine("MinimizeToTray=" + (MinimizeToTray ? "1" : "0"));
                builder.AppendLine("NotifyOnClean=" + (NotifyOnClean ? "1" : "0"));
                builder.AppendLine("StartWithWindows=" + (StartWithWindows ? "1" : "0"));
                builder.AppendLine("Excluded=" + Excluded.Replace("\r", " ").Replace("\n", " "));

                var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temporary, builder.ToString(), Encoding.UTF8);

                if (File.Exists(FilePath))
                {
                    File.Replace(temporary, FilePath, null);
                }
                else
                {
                    File.Move(temporary, FilePath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Apply(string key, string value)
        {
            int number;

            switch (key)
            {
                case "AutoEnabled":
                    AutoEnabled = value == "1";
                    break;
                case "UseInterval":
                    UseInterval = value == "1";
                    break;
                case "UseThreshold":
                    UseThreshold = value == "1";
                    break;
                case "IntervalMinutes":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    {
                        IntervalMinutes = number;
                    }
                    break;
                case "ThresholdPercent":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    {
                        ThresholdPercent = number;
                    }
                    break;
                case "CooldownMinutes":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    {
                        CooldownMinutes = number;
                    }
                    break;
                case "Areas":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    {
                        Areas = (CleanAreas)number;
                    }
                    break;
                case "Language":
                    Language = L.Normalize(value);
                    break;
                case "MinimizeToTray":
                    MinimizeToTray = value == "1";
                    break;
                case "NotifyOnClean":
                    NotifyOnClean = value == "1";
                    break;
                case "StartWithWindows":
                    StartWithWindows = value == "1";
                    break;
                case "Excluded":
                    Excluded = value;
                    break;
            }
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
