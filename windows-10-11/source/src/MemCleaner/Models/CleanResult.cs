using MemCleaner.Models;

namespace MemCleaner.Models;

public sealed class CleanResult
{
    public double FreedBytes { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
