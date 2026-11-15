namespace MemCleaner.Models;

public sealed class CleanupRecord
{
    public DateTime Timestamp { get; set; }
    public double FreedBytes { get; set; }
    public double DurationSeconds { get; set; }
    public CleanupTrigger Trigger { get; set; }
}
