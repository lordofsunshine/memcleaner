using System.IO;
using System.Text;

namespace MemCleaner.Services;

public static class LogService
{
    private const long MaxBytes = 512 * 1024;

    private static readonly object Sync = new();

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warn(string message) => Write("WARN", message, null);

    public static void Error(string message, Exception? error = null) => Write("ERROR", message, error);

    private static void Write(string level, string message, Exception? error)
    {
        try
        {
            lock (Sync)
            {
                var path = AppPaths.LogFile;
                var info = new FileInfo(path);

                if (info.Exists && info.Length > MaxBytes)
                {
                    File.Move(path, path + ".old", true);
                }

                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + level + " " + message;

                if (error is not null)
                {
                    line += " | " + error;
                }

                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
        }
    }
}
