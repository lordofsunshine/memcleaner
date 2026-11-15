using System.Runtime.InteropServices;
using MemCleaner.Interop;
using MemCleaner.Models;

namespace MemCleaner.Services;

public sealed class MemoryProbe
{
    private const ulong DefaultPageSize = 4096;

    public MemorySnapshot Read()
    {
        var status = new NativeMethods.MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>()
        };

        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            LogService.Warn("Не удалось получить состояние физической памяти.");
        }

        var info = new NativeMethods.PerformanceInformation
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.PerformanceInformation>()
        };

        var hasInfo = NativeMethods.GetPerformanceInfo(ref info, info.Size);
        var pageSize = ResolvePageSize(info.PageSize, hasInfo);
        var cached = 0UL;
        var commit = 0UL;
        var limit = 0UL;

        if (hasInfo)
        {
            cached = Multiply(info.SystemCache.ToUInt64(), pageSize);
            commit = Multiply(info.CommitTotal.ToUInt64(), pageSize);
            limit = Multiply(info.CommitLimit.ToUInt64(), pageSize);

            if (status.TotalPhys > 0 && cached > status.TotalPhys)
            {
                cached = status.TotalPhys;
            }

            if (limit > 0 && commit > limit)
            {
                commit = limit;
            }
        }

        return new MemorySnapshot
        {
            TotalBytes = status.TotalPhys,
            AvailableBytes = status.AvailPhys,
            CachedBytes = cached,
            CommitBytes = commit,
            CommitLimitBytes = limit
        };
    }

    private static ulong ResolvePageSize(UIntPtr raw, bool hasInfo)
    {
        if (!hasInfo || raw == UIntPtr.Zero)
        {
            return DefaultPageSize;
        }

        var value = raw.ToUInt64();

        if (value < DefaultPageSize || value > 1024 * 1024 || (value & (value - 1)) != 0)
        {
            return DefaultPageSize;
        }

        return value;
    }

    private static ulong Multiply(ulong pages, ulong pageSize)
    {
        if (pageSize == 0 || pages > ulong.MaxValue / pageSize)
        {
            return 0;
        }

        return pages * pageSize;
    }
}
