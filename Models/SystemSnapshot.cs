using System;
using System.Collections.Generic;

namespace SystemMonitorApp.Models
{
    /// <summary>
    /// Immutable snapshot of system state at a single tick.
    /// Produced by HardwareService on a background thread, consumed by the view layer
    /// on the UI thread. Records are safe to pass across thread boundaries.
    /// </summary>
    public sealed record SystemSnapshot
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;

        public float CpuUsagePercent { get; init; }
        public IReadOnlyList<CpuCoreInfo> CpuCores { get; init; } = Array.Empty<CpuCoreInfo>();

        public float AvailableMemoryGB { get; init; }
        public float UsedMemoryGB { get; init; }
        public float TotalMemoryGB { get; init; }
        public float MemoryUsagePercent { get; init; }

        /// <summary>-1 indicates sensor unavailable.</summary>
        public float GpuUsagePercent { get; init; } = -1f;
        public string? GpuName { get; init; }
        public string GpuInfoText { get; init; } = "";

        /// <summary>Average of all temperature sensors across all hardware. 0 if none found.</summary>
        public float AverageTemperatureC { get; init; }

        public IReadOnlyDictionary<string, float> Thermals { get; init; }
            = new Dictionary<string, float>();

        public IReadOnlyDictionary<string, FanReading> Fans { get; init; }
            = new Dictionary<string, FanReading>();

        public string HardwareInfoText { get; init; } = "";

        /// <summary>Derived status: "OPTIMAL" | "MEDIUMBELASTNING" | "HÖG BELASTNING".</summary>
        public string SystemStatus { get; init; } = "OPTIMAL";
    }

    public sealed record CpuCoreInfo(int Index, string Name, float UsagePercent);

    /// <summary>
    /// A fan sensor reading. RawValue is either an RPM (when IsPercent=false)
    /// or a PWM percent (when IsPercent=true). The heuristic `value * 30f`-converter
    /// that v1.0.8.1 used is replaced by storing both flavors discriminated by SensorType.
    /// </summary>
    public readonly record struct FanReading(float RawValue, bool IsPercent, bool IsGpu);
}
