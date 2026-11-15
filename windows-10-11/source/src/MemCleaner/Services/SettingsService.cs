using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemCleaner.Models;

namespace MemCleaner.Services;

public sealed class SettingsService
{
    private const long MaxBytes = 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Load()
    {
        var path = AppPaths.SettingsFile;

        try
        {
            if (!File.Exists(path))
            {
                return AppSettings.CreateDefault();
            }

            var info = new FileInfo(path);

            if (info.Length > MaxBytes)
            {
                LogService.Warn("Файл настроек слишком большой, используется конфигурация по умолчанию.");
                Backup(path);
                return AppSettings.CreateDefault();
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);

            if (settings is null)
            {
                Backup(path);
                return AppSettings.CreateDefault();
            }

            settings.Clamp();
            return settings;
        }
        catch (JsonException error)
        {
            LogService.Error("Файл настроек повреждён", error);
            Backup(path);
            return AppSettings.CreateDefault();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LogService.Error("Не удалось прочитать настройки", error);
            return AppSettings.CreateDefault();
        }
    }

    public bool Save(AppSettings settings)
    {
        var path = AppPaths.SettingsFile;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            var snapshot = settings.Clone();
            snapshot.Clamp();
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot, Options), new UTF8Encoding(false));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LogService.Error("Не удалось сохранить настройки", error);

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
            LogService.Warn("Не удалось сохранить резервную копию настроек: " + error.Message);
        }
    }
}
