using System.IO;

namespace MemCleaner.Services;

public static class AppPaths
{
    public static string Root { get; } = ResolveRoot();

    public static string SettingsFile => Path.Combine(Root, "settings.json");

    public static string HistoryFile => Path.Combine(Root, "history.json");

    public static string LogFile => Path.Combine(Root, "log.txt");

    private static string ResolveRoot()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(local, "memcleaner");

        try
        {
            if (Directory.Exists(root))
            {
                var info = new DirectoryInfo(root);

                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    LogService.Warn("Data directory is a reparse point, using the temporary folder.");
                    return UseFallback();
                }
            }
            else
            {
                Directory.CreateDirectory(root);
            }

            MigrateFromRoaming(root);
            return root;
        }
        catch (Exception error)
        {
            LogService.Error("Could not create the data directory", error);
            return UseFallback();
        }
    }

    private static string UseFallback()
    {
        var fallback = Path.Combine(Path.GetTempPath(), "memcleaner");

        try
        {
            Directory.CreateDirectory(fallback);
            return fallback;
        }
        catch (Exception)
        {
            return Path.GetTempPath();
        }
    }

    private static void MigrateFromRoaming(string root)
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrEmpty(roaming))
        {
            return;
        }

        var legacy = Path.Combine(roaming, "MemCleaner");

        if (!Directory.Exists(legacy)
            || string.Equals(legacy, root, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var name in new[] { "settings.json", "history.json" })
        {
            try
            {
                var source = Path.Combine(legacy, name);
                var target = Path.Combine(root, name);

                if (File.Exists(source) && !File.Exists(target))
                {
                    File.Move(source, target);
                }
            }
            catch (Exception error)
            {
                LogService.Warn("Could not migrate " + name + ": " + error.Message);
            }
        }
    }
}
