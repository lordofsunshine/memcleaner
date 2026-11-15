using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MemCleanerLegacy
{
    internal sealed class HistoryEntry
    {
        public DateTime Time;
        public ulong Freed;
        public string Trigger;
    }

    internal static class HistoryStore
    {
        private static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "memcleaner");

        private static readonly string FilePath = Path.Combine(Folder, "legacy-history.txt");

        public static List<HistoryEntry> Load()
        {
            var entries = new List<HistoryEntry>();

            try
            {
                if (!File.Exists(FilePath))
                {
                    return entries;
                }

                var info = new FileInfo(FilePath);

                if (info.Length > 128 * 1024)
                {
                    TrimFile();
                }

                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    var parts = line.Split('|');

                    if (parts.Length != 3)
                    {
                        continue;
                    }

                    DateTime time;
                    ulong freed;

                    if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                    {
                        continue;
                    }

                    if (!ulong.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out freed))
                    {
                        continue;
                    }

                    entries.Insert(0, new HistoryEntry { Time = time, Freed = freed, Trigger = parts[2] });
                }
            }
            catch (Exception)
            {
            }

            if (entries.Count > 6)
            {
                entries.RemoveRange(6, entries.Count - 6);
            }

            return entries;
        }

        public static void Append(HistoryEntry entry)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var line = entry.Time.ToString("s", CultureInfo.InvariantCulture)
                    + "|" + entry.Freed.ToString(CultureInfo.InvariantCulture)
                    + "|" + entry.Trigger + Environment.NewLine;

                File.AppendAllText(FilePath, line, Encoding.UTF8);
                TrimFile();
            }
            catch (Exception)
            {
            }
        }

        private static void TrimFile()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }

                var lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                var keep = Math.Min(200, lines.Length);
                var start = lines.Length - keep;
                var trimmed = new string[keep];
                Array.Copy(lines, start, trimmed, 0, keep);
                File.WriteAllLines(FilePath, trimmed, Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }
    }
}
