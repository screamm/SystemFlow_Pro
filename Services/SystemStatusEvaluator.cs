using SystemMonitorApp.Models;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Pure function over SystemSnapshot — maps metrics to a status label.
    /// Extracted so it can be unit-tested without any UI or hardware dependencies.
    /// </summary>
    public static class SystemStatusEvaluator
    {
        public const string Optimal = "OPTIMAL";
        public const string Medium = "MEDIUM LOAD";
        public const string High = "HIGH LOAD";

        // Thresholds — high wins over medium.
        private const float HighCpu = 80f;
        private const float HighMemory = 85f;
        private const float HighTemperature = 75f;
        private const float MediumCpu = 60f;
        private const float MediumMemory = 70f;
        private const float MediumTemperature = 60f;

        public static string Evaluate(float cpuPercent, float memoryPercent, float temperatureC)
        {
            if (cpuPercent > HighCpu || memoryPercent > HighMemory || temperatureC > HighTemperature)
                return High;
            if (cpuPercent > MediumCpu || memoryPercent > MediumMemory || temperatureC > MediumTemperature)
                return Medium;
            return Optimal;
        }

        public static string Evaluate(SystemSnapshot snapshot)
            => Evaluate(snapshot.CpuUsagePercent, snapshot.MemoryUsagePercent, snapshot.AverageTemperatureC);
    }

    /// <summary>
    /// Pure OS version → friendly name mapping. Testable.
    /// </summary>
    public static class OperatingSystemNames
    {
        public static string GetFriendlyName(System.OperatingSystem os)
        {
            if (os.Platform != System.PlatformID.Win32NT) return os.ToString();

            var v = os.Version;
            if (v.Major == 10)
                return v.Build >= 22000 ? "Windows 11" : "Windows 10";
            if (v.Major == 6)
            {
                if (v.Minor == 3) return "Windows 8.1";
                if (v.Minor == 2) return "Windows 8";
                if (v.Minor == 1) return "Windows 7";
            }
            return $"Windows NT {v.Major}.{v.Minor}";
        }
    }
}
