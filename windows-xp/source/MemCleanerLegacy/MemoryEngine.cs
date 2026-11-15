using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MemCleanerLegacy
{
    [Flags]
    internal enum CleanAreas
    {
        None = 0,
        WorkingSets = 1,
        Standby = 2,
        Modified = 4,
        FileCache = 8
    }

    internal sealed class MemorySnapshot
    {
        public ulong TotalBytes;
        public ulong FreeBytes;
        public ulong CommitBytes;
        public ulong CommitLimitBytes;
        public ulong CacheBytes;
        public double UsedPercent;
    }

    internal sealed class CleanOutcome
    {
        public ulong FreedBytes;
        public TimeSpan Duration;
        public List<string> Errors = new List<string>();
        public bool Succeeded
        {
            get { return Errors.Count == 0; }
        }
    }

    internal static class MemoryEngine
    {
        public static MemorySnapshot Read()
        {
            var status = new NativeMethods.MemoryStatusEx();
            status.Length = (uint)Marshal.SizeOf(typeof(NativeMethods.MemoryStatusEx));

            if (!NativeMethods.GlobalMemoryStatusEx(ref status))
            {
                throw new InvalidOperationException(L.T("errorRead"));
            }

            var used = status.TotalPhysical - status.AvailablePhysical;
            var snapshot = new MemorySnapshot
            {
                TotalBytes = status.TotalPhysical,
                FreeBytes = status.AvailablePhysical,
                CommitBytes = status.TotalPageFile - status.AvailablePageFile,
                CommitLimitBytes = status.TotalPageFile,
                UsedPercent = status.TotalPhysical == 0 ? 0 : used * 100.0 / status.TotalPhysical
            };

            var info = new NativeMethods.PerformanceInformation();
            info.Size = (uint)Marshal.SizeOf(typeof(NativeMethods.PerformanceInformation));

            if (NativeMethods.GetPerformanceInfo(ref info, info.Size))
            {
                var pageSize = info.PageSize.ToUInt64();

                if (pageSize == 0)
                {
                    pageSize = 4096;
                }

                snapshot.CacheBytes = info.SystemCache.ToUInt64() * pageSize;

                if (snapshot.CacheBytes > snapshot.TotalBytes)
                {
                    snapshot.CacheBytes = snapshot.TotalBytes;
                }
            }

            return snapshot;
        }

        public static CleanAreas SupportedAreas
        {
            get
            {
                var areas = CleanAreas.WorkingSets;

                if (Platform.SupportsMemoryLists)
                {
                    areas |= CleanAreas.Standby | CleanAreas.Modified;
                }

                if (Platform.SupportsFileCache)
                {
                    areas |= CleanAreas.FileCache;
                }

                return areas;
            }
        }

        public static CleanOutcome Clean(CleanAreas areas, string excluded)
        {
            var watch = Stopwatch.StartNew();
            var outcome = new CleanOutcome();
            var before = Read();

            EnablePrivilege("SeDebugPrivilege");
            EnablePrivilege("SeProfileSingleProcessPrivilege");
            EnablePrivilege("SeIncreaseQuotaPrivilege");

            if (areas.Has(CleanAreas.WorkingSets))
            {
                RunArea(outcome, "areaWorkingSets", delegate { TrimWorkingSets(excluded, outcome); });
            }

            if (areas.Has(CleanAreas.Standby) && Platform.SupportsMemoryLists)
            {
                RunArea(outcome, "areaStandby", delegate { SetMemoryList(NativeMethods.MemoryPurgeStandbyList); });
            }

            if (areas.Has(CleanAreas.Modified) && Platform.SupportsMemoryLists)
            {
                RunArea(outcome, "areaModified", delegate { SetMemoryList(NativeMethods.MemoryFlushModifiedList); });
            }

            if (areas.Has(CleanAreas.FileCache) && Platform.SupportsFileCache)
            {
                RunArea(outcome, "areaFileCache", FlushFileCache);
            }

            DisablePrivilege("SeIncreaseQuotaPrivilege");
            DisablePrivilege("SeProfileSingleProcessPrivilege");
            DisablePrivilege("SeDebugPrivilege");

            watch.Stop();
            outcome.Duration = watch.Elapsed;

            var after = Read();
            outcome.FreedBytes = after.FreeBytes > before.FreeBytes ? after.FreeBytes - before.FreeBytes : 0;

            return outcome;
        }

        private static void RunArea(CleanOutcome outcome, string key, Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                outcome.Errors.Add(L.F("errorArea", L.T(key), error.Message));
            }
        }

        private static void TrimWorkingSets(string excluded, CleanOutcome outcome)
        {
            var names = new List<string>();

            foreach (var raw in (excluded ?? string.Empty).Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var name = raw.Trim().ToLowerInvariant();

                if (name.Length == 0)
                {
                    continue;
                }

                if (name.EndsWith(".exe", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - 4);
                }

                names.Add(name);
            }

            var current = Process.GetCurrentProcess().Id;
            var trimmed = 0;
            var failed = 0;

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id == current || process.Id <= 4)
                    {
                        continue;
                    }

                    if (names.Contains(process.ProcessName.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var handle = NativeMethods.OpenProcess(
                        NativeMethods.ProcessSetQuota | NativeMethods.ProcessQueryLimitedInformation,
                        false,
                        process.Id);

                    if (handle == IntPtr.Zero)
                    {
                        failed++;
                        continue;
                    }

                    try
                    {
                        if (NativeMethods.EmptyWorkingSet(handle))
                        {
                            trimmed++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(handle);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (trimmed == 0 && failed == 0)
            {
                return;
            }

            if (failed > 0 && trimmed == 0)
            {
                outcome.Errors.Add(L.F("errorWorkingSets", failed));
            }
        }

        private static void SetMemoryList(int command)
        {
            var value = command;
            var status = NativeMethods.NtSetSystemInformation(0x50, ref value, 4);

            if (status < 0)
            {
                throw new InvalidOperationException(L.F("errorNtStatus", status));
            }

            System.Threading.Thread.Sleep(120);
        }

        private static void FlushFileCache()
        {
            var flush = new IntPtr(-1);

            if (!NativeMethods.SetSystemFileCacheSize(flush, flush, 0))
            {
                throw new InvalidOperationException(L.T("errorFileCache"));
            }
        }

        private static void DisablePrivilege(string name)
        {
            SetPrivilege(name, false);
        }

        private static void EnablePrivilege(string name)
        {
            SetPrivilege(name, true);
        }

        private static void SetPrivilege(string name, bool enabled)
        {
            IntPtr token;

            if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), 0x28, out token))
            {
                return;
            }

            try
            {
                NativeMethods.Luid luid;

                if (!NativeMethods.LookupPrivilegeValue(null, name, out luid))
                {
                    return;
                }

                var state = new NativeMethods.TokenPrivileges
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = enabled ? 0x00000002 : 0
                };

                NativeMethods.AdjustTokenPrivileges(token, false, ref state, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                NativeMethods.CloseHandle(token);
            }
        }
    }

    internal static class Humanize
    {
        public static string Bytes(double bytes)
        {
            var absolute = Math.Abs(bytes);

            if (absolute >= 1024d * 1024 * 1024)
            {
                return L.F("unitGb", (bytes / (1024d * 1024 * 1024)).ToString("0.0", L.Culture));
            }

            if (absolute >= 1024 * 1024)
            {
                return L.F("unitMb", (bytes / (1024d * 1024)).ToString("0", L.Culture));
            }

            if (absolute >= 1024)
            {
                return L.F("unitKb", (bytes / 1024d).ToString("0", L.Culture));
            }

            return L.F("unitBytes", bytes.ToString("0", L.Culture));
        }

        public static string Percent(double value)
        {
            return L.F("unitPercent", value.ToString("0", L.Culture));
        }

        public static string Seconds(double value)
        {
            return L.F("unitSeconds", value.ToString("0.0", L.Culture));
        }
    }
}
