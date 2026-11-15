using System;

namespace MemCleanerLegacy
{
    internal static class Platform
    {
        private static readonly Version Version = Environment.OSVersion.Version;

        public static bool IsXp
        {
            get { return Version.Major == 5; }
        }

        public static bool SupportsMemoryLists
        {
            get { return Version.Major >= 6; }
        }

        public static bool SupportsFileCache
        {
            get { return Version.Major >= 6; }
        }

        public static string Name
        {
            get
            {
                if (Version.Major == 5)
                {
                    if (Version.Minor == 1)
                    {
                        return "Windows XP";
                    }

                    return Version.Minor == 2 ? "Windows XP x64 / Server 2003" : "Windows 2000";
                }

                if (Version.Major == 6 && Version.Minor == 0)
                {
                    return "Windows Vista";
                }

                if (Version.Major == 6 && Version.Minor == 1)
                {
                    return "Windows 7";
                }

                if (Version.Major == 6 && Version.Minor == 2)
                {
                    return "Windows 8";
                }

                if (Version.Major == 6 && Version.Minor == 3)
                {
                    return "Windows 8.1";
                }

                return "Windows " + Version.Major + "." + Version.Minor;
            }
        }
    }
}

namespace MemCleanerLegacy
{
    internal static class AreaExtensions
    {
        public static bool Has(this CleanAreas value, CleanAreas area)
        {
            return (value & area) == area;
        }
    }
}
