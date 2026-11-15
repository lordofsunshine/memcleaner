using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using MemCleaner.Interop;
using MemCleaner.Localization;
using MemCleaner.Models;

namespace MemCleaner.Services;

public sealed class MemoryCleanerService
{
    private readonly MemoryProbe _probe;

    public MemoryCleanerService(MemoryProbe probe)
    {
        _probe = probe;
    }

    public Task<CleanResult> CleanAsync(CleanupArea areas, string exclusions, CancellationToken token = default)
        => Task.Run(() => Clean(areas, exclusions, token), token);

    private CleanResult Clean(CleanupArea areas, string exclusions, CancellationToken token)
    {
        var before = _probe.Read();
        var workingSetBefore = TotalWorkingSetBytes();
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var excluded = ParseExclusions(exclusions);
        var quotaEnabled = EnablePrivilege("SeProfileSingleProcessPrivilege");
        EnablePrivilege("SeIncreaseQuotaPrivilege");

        try
        {
            if (areas.HasFlag(CleanupArea.WorkingSets) && !token.IsCancellationRequested)
            {
                RunArea(errors, "areaWorkingSets", () => TrimWorkingSets(excluded, token));
            }

            if (areas.HasFlag(CleanupArea.StandbyList) && !token.IsCancellationRequested)
            {
                RunArea(errors, "areaStandby", () => SetMemoryList(NativeMethods.MemoryPurgeStandbyList));
            }

            if (areas.HasFlag(CleanupArea.ModifiedPageList) && !token.IsCancellationRequested)
            {
                RunArea(errors, "areaModified", () => SetMemoryList(NativeMethods.MemoryFlushModifiedList));
            }

            if (areas.HasFlag(CleanupArea.SystemFileCache) && !token.IsCancellationRequested)
            {
                RunArea(errors, "areaFileCache", FlushFileCache);
            }
        }
        finally
        {
            DisablePrivilege("SeIncreaseQuotaPrivilege");
            DisablePrivilege("SeProfileSingleProcessPrivilege");
        }

        stopwatch.Stop();
        var after = _probe.Read();
        var workingSetAfter = TotalWorkingSetBytes();
        var availableGain = (double)after.AvailableBytes - before.AvailableBytes;
        var workingSetGain = (double)workingSetBefore - workingSetAfter;
        var freed = Math.Max(0, availableGain) + Math.Max(0, workingSetGain);

        foreach (var error in errors)
        {
            LogService.Warn("Cleanup area failed: " + error);
        }

        return new CleanResult
        {
            FreedBytes = (ulong)freed,
            Duration = stopwatch.Elapsed,
            Succeeded = errors.Count == 0,
            Errors = errors
        };
    }

    private static void RunArea(ICollection<string> errors, string areaKey, Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or UnauthorizedAccessException or IOException or SecurityException)
        {
            errors.Add(Loc.F("errorAreaPrefix", Loc.T(areaKey), error.Message));
        }
    }

    private static void TrimWorkingSets(HashSet<string> excluded, CancellationToken token)
    {
        if (excluded.Count == 0)
        {
            SetMemoryList(NativeMethods.MemoryEmptyWorkingSets);
            return;
        }

        if (!EnablePrivilege("SeDebugPrivilege"))
        {
            throw new InvalidOperationException(Loc.T("errorDebugPrivilege"));
        }

        try
        {
            var current = Environment.ProcessId;

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (process.Id == current || process.Id <= 4 || excluded.Contains(process.ProcessName))
                    {
                        continue;
                    }

                    var handle = NativeMethods.OpenProcess(
                        NativeMethods.ProcessSetQuota | NativeMethods.ProcessQueryLimitedInformation,
                        false,
                        process.Id);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        NativeMethods.EmptyWorkingSet(handle);
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(handle);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (Win32Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        finally
        {
            DisablePrivilege("SeDebugPrivilege");
        }
    }

    private static ulong TotalWorkingSetBytes()
    {
        ulong total = 0;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                total += (ulong)process.WorkingSet64;
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return total;
    }

    private static HashSet<string> ParseExclusions(string value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var part in value.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var name = part.Trim().Trim('"').Trim();

            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^4];
            }

            if (name.Length > 0)
            {
                result.Add(name);
            }
        }

        return result;
    }

    private static void SetMemoryList(int command)
    {
        var buffer = Marshal.AllocHGlobal(sizeof(int));

        try
        {
            Marshal.WriteInt32(buffer, command);
            var status = NativeMethods.NtSetSystemInformation(NativeMethods.SystemMemoryListInformation, buffer, sizeof(int));

            if (status < 0)
            {
                throw new InvalidOperationException(Loc.F("errorNtStatus", status));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void FlushFileCache()
    {
        var minusOne = new UIntPtr(ulong.MaxValue);
        var info = new NativeMethods.FileCacheInformation
        {
            MinimumWorkingSet = minusOne,
            MaximumWorkingSet = minusOne
        };

        var buffer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.FileCacheInformation>());

        try
        {
            Marshal.StructureToPtr(info, buffer, false);
            var status = NativeMethods.NtSetSystemInformation(
                NativeMethods.SystemFileCacheInformation,
                buffer,
                Marshal.SizeOf<NativeMethods.FileCacheInformation>());

            if (status < 0)
            {
                LogService.Warn($"File cache limits reset returned 0x{status:X8}, applying flush only.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        if (!NativeMethods.SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.T("errorFileCache"));
        }
    }

    private static void DisablePrivilege(string name)
    {
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeMethods.TokenAdjustPrivileges | NativeMethods.TokenQuery,
                out var token))
        {
            return;
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, name, out var luid))
            {
                return;
            }

            var state = new NativeMethods.TokenPrivileges
            {
                Count = 1,
                Luid = luid,
                Attributes = 0
            };

            NativeMethods.AdjustTokenPrivileges(token, false, ref state, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }

    private static bool EnablePrivilege(string name)
    {
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeMethods.TokenAdjustPrivileges | NativeMethods.TokenQuery,
                out var token))
        {
            LogService.Warn("Could not open the process token for " + name);
            return false;
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, name, out var luid))
            {
                LogService.Warn("Privilege " + name + " is not available on this system.");
                return false;
            }

            var state = new NativeMethods.TokenPrivileges
            {
                Count = 1,
                Luid = luid,
                Attributes = NativeMethods.SePrivilegeEnabled
            };

            if (!NativeMethods.AdjustTokenPrivileges(token, false, ref state, 0, IntPtr.Zero, IntPtr.Zero))
            {
                LogService.Warn("Could not enable " + name);
                return false;
            }

            if (Marshal.GetLastWin32Error() == NativeMethods.ErrorNotAllAssigned)
            {
                LogService.Warn("Privilege " + name + " is not assigned to this process.");
                return false;
            }

            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }
}
