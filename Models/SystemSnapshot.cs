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

        /// <summary>
        /// Cooling/thermal readouts grouped for the CPU panel. Includes Fan sensors
        /// (when motherboard exposes them), CPU package temperature, per-core temps,
        /// package power and voltage — whatever is available via CPU MSR. Designed to
        /// always surface meaningful data even when SuperIO fans cannot be read.
        /// </summary>
        public IReadOnlyList<CoolingReadout> CpuCooling { get; init; } = Array.Empty<CoolingReadout>();

        /// <summary>GPU thermal readouts: fan RPM/percent, temperature, memory temp, power.</summary>
        public IReadOnlyList<CoolingReadout> GpuCooling { get; init; } = Array.Empty<CoolingReadout>();

        public string HardwareInfoText { get; init; } = "";

        /// <summary>Derived status: "OPTIMAL" | "MEDIUM LOAD" | "HIGH LOAD".</summary>
        public string SystemStatus { get; init; } = "OPTIMAL";
    }

    public sealed record CpuCoreInfo(int Index, string Name, float UsagePercent);

    /// <summary>
    /// A fan sensor reading. RawValue is either an RPM (when IsPercent=false)
    /// or a PWM percent (when IsPercent=true). The heuristic `value * 30f`-converter
    /// that v1.0.8.1 used is replaced by storing both flavors discriminated by SensorType.
    /// </summary>
    public readonly record struct FanReading(float RawValue, bool IsPercent, bool IsGpu);

    /// <summary>
    /// A single cooling/thermal readout — fan, temp, power, voltage.
    /// Rendered as "Label: Value Unit" in the UI with color-coded severity.
    /// </summary>
    public sealed record CoolingReadout(
        string Label,
        string DisplayValue,
        CoolingSeverity Severity = CoolingSeverity.Info);

    public enum CoolingSeverity
    {
        /// <summary>Neutral informational value (default).</summary>
        Info,
        /// <summary>Value is healthy/good (green).</summary>
        Healthy,
        /// <summary>Elevated but acceptable (yellow).</summary>
        Warning,
        /// <summary>Critical — user attention needed (red).</summary>
        Critical,
        /// <summary>Passive/idle state — not alarming (muted).</summary>
        Idle
    }
}
