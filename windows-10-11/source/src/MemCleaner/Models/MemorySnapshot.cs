namespace MemCleaner.Models;

public sealed class MemorySnapshot
{
    public ulong TotalBytes { get; init; }
    public ulong AvailableBytes { get; init; }
    public ulong CachedBytes { get; init; }
    public ulong CommitBytes { get; init; }
    public ulong CommitLimitBytes { get; init; }

    public ulong UsedBytes => TotalBytes > AvailableBytes ? TotalBytes - AvailableBytes : 0;

    public double UsedPercent => TotalBytes == 0 ? 0 : UsedBytes * 100.0 / TotalBytes;

    public double CommitPercent => CommitLimitBytes == 0 ? 0 : CommitBytes * 100.0 / CommitLimitBytes;
}
