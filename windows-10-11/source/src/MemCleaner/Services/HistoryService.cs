using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemCleaner.Models;

namespace MemCleaner.Services;

public sealed class HistoryService
{
    private const int Limit = 40;
    private const long MaxBytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<CleanupRecord> Load()
    {
        var path = AppPaths.HistoryFile;

        try
        {
            if (!File.Exists(path))
            {
                return new List<CleanupRecord>();
            }

            var info = new FileInfo(path);

            if (info.Length > MaxBytes)
            {
                LogService.Warn("Файл истории слишком большой, журнал очищен.");
                Backup(path);
                return new List<CleanupRecord>();
            }

            var records = JsonSerializer.Deserialize<List<CleanupRecord>>(File.ReadAllText(path, Encoding.UTF8), Options);

            if (records is null)
            {
                return new List<CleanupRecord>();
            }

            return records
                .Where(record => record is not null)
                .Where(record => record.Timestamp.Year > 2000)
                .Take(Limit)
                .ToList();
        }
        catch (JsonException error)
        {
            LogService.Error("Файл истории повреждён", error);
            Backup(path);
            return new List<CleanupRecord>();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LogService.Error("Не удалось прочитать историю", error);
            return new List<CleanupRecord>();
        }
    }

    public bool Append(CleanupRecord record)
    {
        var records = Load();
        records.Insert(0, record);

        if (records.Count > Limit)
        {
            records.RemoveRange(Limit, records.Count - Limit);
        }

        return Save(records);
    }

    private static bool Save(List<CleanupRecord> records)
    {
        var path = AppPaths.HistoryFile;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(records, Options), new UTF8Encoding(false));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LogService.Error("Не удалось сохранить историю", error);

            try
            {
                File.Delete(temporary);
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

    private static void Backup(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Move(path, path + ".bak", true);
            }
        }
        catch (Exception error)
        {
            LogService.Warn("Не удалось сохранить резервную копию истории: " + error.Message);
        }
    }
}
