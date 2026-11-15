namespace MemCleaner.Models;

[Flags]
public enum CleanupArea
{
    None = 0,
    WorkingSets = 1,
    StandbyList = 2,
    ModifiedPageList = 4,
    SystemFileCache = 8
}
