using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using SystemMonitorApp.Models;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Concrete hardware monitor. Serializes all LibreHardwareMonitor access via a lock
    /// since LHM's Computer is not thread-safe. WMI calls use a 2-second timeout to prevent
    /// hangs on misconfigured systems.
    /// </summary>
    public sealed class HardwareService : IHardwareService
    {
        private static readonly EnumerationOptions _wmiOptions = new()
        {
            Timeout = TimeSpan.FromSeconds(2),
            ReturnImmediately = false
        };

        private static readonly UpdateVisitor _visitor = new();

        private readonly object _computerLock = new();
        private Computer? _computer;
        private PerformanceCounter? _cpuCounter;
        private List<PerformanceCounter>? _cpuCoreCounters;
        private PerformanceCounter? _memoryCounter;

        private volatile bool _disposed;

        public bool IsInitialized { get; private set; }
        public bool IsRunningAsAdmin { get; private set; }
        public float TotalMemoryGB { get; private set; }
        public string HardwareInfoText { get; private set; } = "";

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (IsInitialized) return;
            if (_disposed) throw new ObjectDisposedException(nameof(HardwareService));

            IsRunningAsAdmin = CheckIsRunningAsAdmin();

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                InitializeCounters();
                TotalMemoryGB = ReadTotalMemoryGB();
                HardwareInfoText = BuildHardwareInfoText();

                // Dump full hardware tree for diagnostics — critical for troubleshooting
                // missing fan/motherboard sensors on unsupported boards.
                if (_computer != null)
                {
                    lock (_computerLock)
                    {
                        HardwareDiagnostics.DumpReport(_computer);
                    }
                }
            }, ct);

            IsInitialized = true;
            Logger.Info($"HardwareService initialized. Admin={IsRunningAsAdmin}, TotalRAM={TotalMemoryGB:F1} GB");
        }

        private void InitializeCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                _cpuCoreCounters = new List<PerformanceCounter>();
                int coreCount = Environment.ProcessorCount;
                for (int i = 0; i < coreCount; i++)
                    _cpuCoreCounters.Add(new PerformanceCounter("Processor", "% Processor Time", i.ToString()));

                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                _cpuCounter.NextValue();
                foreach (var counter in _cpuCoreCounters)
                    counter.NextValue();
                _memoryCounter.NextValue();

                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true,
                    IsPsuEnabled = true,
                    IsBatteryEnabled = true
                };

                lock (_computerLock)
                {
                    _computer.Open();
                    _computer.Accept(_visitor);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("HardwareService InitializeCounters failed", ex);
            }
        }

        public Task<SystemSnapshot> CollectSnapshotAsync(CancellationToken ct = default)
        {
            if (_disposed) return Task.FromResult(EmptySnapshot());

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return CollectSnapshot();
            }, ct);
        }

        private SystemSnapshot CollectSnapshot()
        {
            lock (_computerLock)
            {
                if (_disposed || _computer == null)
                    return EmptySnapshot();

                try { _computer.Accept(_visitor); }
                catch (Exception ex) { Logger.Warn("computer.Accept failed", ex); }

                float cpuUsage = SafeNextValue(_cpuCounter);
                var cores = ReadCoreUsages();
                float availableMemoryMB = SafeNextValue(_memoryCounter);
                float availableGB = availableMemoryMB / 1024f;
                float usedGB = TotalMemoryGB > 0 ? TotalMemoryGB - availableGB : 0f;
                float memPercent = TotalMemoryGB > 0 ? (usedGB / TotalMemoryGB) * 100f : 0f;

                float gpuUsage = ReadGpuUsage();
                string? gpuName = ReadGpuName();
                float avgTemp = ReadAverageTemperature();
                var thermals = ReadThermalData();
                var fans = ReadFanData();
                var cpuCooling = ReadCpuCooling();
                var gpuCooling = ReadGpuCooling();
                string gpuInfoText = BuildGpuInfoText();

                string status = SystemStatusEvaluator.Evaluate(cpuUsage, memPercent, avgTemp);

                return new SystemSnapshot
                {
                    CpuUsagePercent = cpuUsage,
                    CpuCores = cores,
                    AvailableMemoryGB = availableGB,
                    UsedMemoryGB = usedGB,
                    TotalMemoryGB = TotalMemoryGB,
                    MemoryUsagePercent = memPercent,
                    GpuUsagePercent = gpuUsage,
                    GpuName = gpuName,
                    GpuInfoText = gpuInfoText,
                    AverageTemperatureC = avgTemp,
                    Thermals = thermals,
                    Fans = fans,
                    CpuCooling = cpuCooling,
                    GpuCooling = gpuCooling,
                    HardwareInfoText = HardwareInfoText,
                    SystemStatus = status
                };
            }
        }

        private SystemSnapshot EmptySnapshot() => new()
        {
            HardwareInfoText = HardwareInfoText,
            TotalMemoryGB = TotalMemoryGB
        };

        private static bool CheckIsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Logger.Warn("Admin check failed", ex);
                return false;
            }
        }

        private static float ReadTotalMemoryGB()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    null, "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", _wmiOptions);
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalBytes = (ulong)obj["TotalPhysicalMemory"];
                    return totalBytes / (1024f * 1024f * 1024f);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ReadTotalMemoryGB failed", ex);
            }
            return 0f;
        }

        private static float SafeNextValue(PerformanceCounter? counter)
        {
            if (counter == null) return 0f;
            try { return counter.NextValue(); }
            catch (Exception ex) { Logger.Warn($"PerformanceCounter read failed ({counter.CategoryName}\\{counter.CounterName})", ex); return 0f; }
        }

        private IReadOnlyList<CpuCoreInfo> ReadCoreUsages()
        {
            if (_cpuCoreCounters == null) return Array.Empty<CpuCoreInfo>();
            int count = Math.Min(_cpuCoreCounters.Count, 16);
            var result = new List<CpuCoreInfo>(count);
            for (int i = 0; i < count; i++)
                result.Add(new CpuCoreInfo(i, $"Core {i}", SafeNextValue(_cpuCoreCounters[i])));
            return result;
        }

        private float ReadGpuUsage()
        {
            try
            {
                if (_computer == null) return 0f;
                foreach (var hardware in _computer.Hardware)
                {
                    if (!IsGpu(hardware.HardwareType)) continue;
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                            return sensor.Value ?? 0f;
                    }
                }
                return 0f;
            }
            catch (Exception ex) { Logger.Warn("ReadGpuUsage failed", ex); return -1f; }
        }

        private string? ReadGpuName()
        {
            try
            {
                if (_computer == null) return null;
                foreach (var hardware in _computer.Hardware)
                    if (IsGpu(hardware.HardwareType))
                        return hardware.Name;
            }
            catch (Exception ex) { Logger.Warn("ReadGpuName failed", ex); }
            return null;
        }

        private float ReadAverageTemperature()
        {
            try
            {
                if (_computer == null) return 0f;
                var temps = new List<float>();
                foreach (var hardware in _computer.Hardware)
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            temps.Add(sensor.Value.Value);
                return temps.Count > 0 ? temps.Average() : 0f;
            }
            catch (Exception ex) { Logger.Warn("ReadAverageTemperature failed", ex); return 0f; }
        }

        private Dictionary<string, float> ReadThermalData()
        {
            var result = new Dictionary<string, float>();
            try
            {
                if (_computer == null) return result;
                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue) continue;
                        float v = sensor.Value.Value;
                        if (v > 0 && v < 150)
                        {
                            string name = $"{hardware.Name} {sensor.Name}"
                                .Replace("Temperature", "Temp").Replace("Package", "Pkg");
                            result[name] = v;
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("ReadThermalData failed", ex); }
            return result;
        }

        private Dictionary<string, FanReading> ReadFanData()
        {
            var result = new Dictionary<string, FanReading>();
            try
            {
                if (_computer == null) return result;

                foreach (var hardware in _computer.Hardware)
                {
                    bool isGpu = IsGpu(hardware.HardwareType);

                    foreach (var sensor in hardware.Sensors)
                        AddFanSensor(result, hardware.Name, sensor, isGpu);

                    foreach (var subHardware in hardware.SubHardware)
                        foreach (var sensor in subHardware.Sensors)
                            AddFanSensor(result, subHardware.Name, sensor, isGpu);
                }
            }
            catch (Exception ex) { Logger.Warn("ReadFanData failed", ex); }
            return result;
        }

        private static void AddFanSensor(Dictionary<string, FanReading> result, string hwName, ISensor sensor, bool isGpu)
        {
            if (!sensor.Value.HasValue) return;

            string sensorNameLower = sensor.Name.ToLowerInvariant();
            string displayName = $"{hwName} {sensor.Name}";

            // Primary: Fan sensor → RPM
            if (sensor.SensorType == SensorType.Fan)
            {
                result[displayName] = new FanReading(sensor.Value.Value, IsPercent: false, IsGpu: isGpu);
                return;
            }

            // Primary: Control sensor with fan-related name → PWM percent
            if (sensor.SensorType == SensorType.Control)
            {
                bool fanRelated = sensorNameLower.Contains("fan")
                               || sensorNameLower.Contains("pump")
                               || (isGpu && sensorNameLower.Contains("gpu"));
                if (fanRelated)
                    result[displayName] = new FanReading(sensor.Value.Value, IsPercent: true, IsGpu: isGpu);
                return;
            }

            // Fallback: sensor name contains "rpm" or "fan" under another SensorType.
            // Some motherboards (and older SuperIO chips) expose fans as Factor, Data,
            // or other categories — v1.0.8.1 picked these up via name match and we
            // must keep that behavior or real fans disappear on those systems.
            if (sensorNameLower.Contains("rpm") || sensorNameLower.Contains("fan"))
            {
                result[displayName] = new FanReading(sensor.Value.Value, IsPercent: false, IsGpu: isGpu);
            }
        }

        /// <summary>
        /// Builds the CPU cooling panel data — always returns something meaningful
        /// even when motherboard SuperIO fans are unavailable. Pulls from:
        /// - Motherboard Fan/Control sensors (when LHM exposes them)
        /// - CPU hardware: Package temp, Package power, Core voltage, Per-core temps
        /// - CPU MSR data works on every modern AMD/Intel CPU regardless of motherboard.
        /// </summary>
        private List<CoolingReadout> ReadCpuCooling()
        {
            var result = new List<CoolingReadout>();
            if (_computer == null) return result;

            try
            {
                // 1. Motherboard fans (if any). Exclude GPU-routed ones.
                foreach (var kvp in ReadFanData())
                {
                    bool isGpuRouted = kvp.Value.IsGpu
                        || kvp.Key.Contains("gpu", StringComparison.OrdinalIgnoreCase)
                        || kvp.Key.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
                        || kvp.Key.Contains("radeon", StringComparison.OrdinalIgnoreCase)
                        || kvp.Key.Contains("geforce", StringComparison.OrdinalIgnoreCase);
                    if (isGpuRouted) continue;

                    string unit = kvp.Value.IsPercent ? "%" : "RPM";
                    string v = $"{kvp.Value.RawValue:F0} {unit}";
                    CoolingSeverity sev = kvp.Value.RawValue <= 0
                        ? CoolingSeverity.Idle
                        : CoolingSeverity.Healthy;
                    result.Add(new CoolingReadout(ShortName(kvp.Key), v, sev));
                }

                // 2. CPU hardware data — always available via MSR on modern CPUs
                foreach (var hw in _computer.Hardware)
                {
                    if (hw.HardwareType != HardwareType.Cpu) continue;

                    // Package temperature
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue) continue;
                        float v = sensor.Value.Value;
                        if (v <= 0 || v > 150) continue;

                        string label = sensor.Name
                            .Replace("Core (Tctl/Tdie)", "CPU Package (Tctl)")
                            .Replace("CPU Package", "CPU Package");
                        var sev = v > 85 ? CoolingSeverity.Critical
                               : v > 70 ? CoolingSeverity.Warning
                               : CoolingSeverity.Healthy;
                        result.Add(new CoolingReadout(label, $"{v:F0} °C", sev));
                    }

                    // Package power
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Power || !sensor.Value.HasValue) continue;
                        if (!sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)) continue;
                        if (sensor.Value.Value <= 0) continue;
                        result.Add(new CoolingReadout(
                            "CPU Package Power", $"{sensor.Value.Value:F1} W", CoolingSeverity.Info));
                    }

                    // Total CPU load
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Load || !sensor.Value.HasValue) continue;
                        if (sensor.Name != "CPU Total") continue;
                        float v = sensor.Value.Value;
                        var sev = v > 90 ? CoolingSeverity.Warning
                               : v > 70 ? CoolingSeverity.Info
                               : CoolingSeverity.Healthy;
                        result.Add(new CoolingReadout("CPU Load", $"{v:F0} %", sev));
                    }

                    // SoC voltage (AMD) — relevant for thermal stability
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Voltage || !sensor.Value.HasValue) continue;
                        if (!sensor.Name.Contains("SoC", StringComparison.OrdinalIgnoreCase)) continue;
                        if (sensor.Value.Value <= 0) continue;
                        result.Add(new CoolingReadout(
                            "SoC Voltage", $"{sensor.Value.Value:F2} V", CoolingSeverity.Info));
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("ReadCpuCooling failed", ex); }

            return result;
        }

        /// <summary>
        /// Builds GPU cooling panel data — fans, temps, power. Uses NVIDIA/AMD driver
        /// API via LHM which works on virtually all systems with proper GPU drivers.
        /// </summary>
        private List<CoolingReadout> ReadGpuCooling()
        {
            var result = new List<CoolingReadout>();
            if (_computer == null) return result;

            try
            {
                foreach (var hw in _computer.Hardware)
                {
                    if (!IsGpu(hw.HardwareType)) continue;

                    // Fan sensors
                    int fanIndex = 0;
                    foreach (var sensor in hw.Sensors)
                    {
                        if (!sensor.Value.HasValue) continue;
                        if (sensor.SensorType == SensorType.Fan)
                        {
                            fanIndex++;
                            float v = sensor.Value.Value;
                            var sev = v <= 0 ? CoolingSeverity.Idle : CoolingSeverity.Healthy;
                            string label = fanIndex == 1 ? "GPU Fan" : $"GPU Fan {fanIndex}";
                            string display = v <= 0 ? "0 RPM (Zero-RPM)" : $"{v:F0} RPM";
                            result.Add(new CoolingReadout(label, display, sev));
                        }
                    }

                    // Control (PWM) for GPU fans
                    int controlIndex = 0;
                    foreach (var sensor in hw.Sensors)
                    {
                        if (!sensor.Value.HasValue) continue;
                        if (sensor.SensorType == SensorType.Control
                            && sensor.Name.Contains("Fan", StringComparison.OrdinalIgnoreCase))
                        {
                            controlIndex++;
                            float v = sensor.Value.Value;
                            var sev = v >= 70 ? CoolingSeverity.Warning
                                   : v > 0 ? CoolingSeverity.Healthy
                                   : CoolingSeverity.Idle;
                            string label = controlIndex == 1 ? "GPU PWM" : $"GPU PWM {controlIndex}";
                            result.Add(new CoolingReadout(label, $"{v:F0} %", sev));
                        }
                    }

                    // Temperatures
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue) continue;
                        float v = sensor.Value.Value;
                        if (v <= 0 || v > 150) continue;

                        string label = sensor.Name
                            .Replace("GPU ", "GPU ");
                        var sev = v > 85 ? CoolingSeverity.Critical
                               : v > 70 ? CoolingSeverity.Warning
                               : CoolingSeverity.Healthy;
                        result.Add(new CoolingReadout(label, $"{v:F0} °C", sev));
                    }

                    // Power
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType != SensorType.Power || !sensor.Value.HasValue) continue;
                        if (sensor.Value.Value <= 0) continue;
                        result.Add(new CoolingReadout(
                            sensor.Name, $"{sensor.Value.Value:F1} W", CoolingSeverity.Info));
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("ReadGpuCooling failed", ex); }

            return result;
        }

        private static string ShortName(string name)
            => name.Length > 28 ? name.Substring(0, 25) + "..." : name;

        private string BuildGpuInfoText()
        {
            try
            {
                if (_computer == null) return "GPU-information ej tillgänglig";
                var sb = new StringBuilder("GPU Information:\n");
                bool found = false;

                foreach (var hardware in _computer.Hardware)
                {
                    if (!IsGpu(hardware.HardwareType)) continue;
                    found = true;
                    sb.AppendLine(hardware.Name);

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (!sensor.Value.HasValue) continue;
                        if (sensor.SensorType == SensorType.Load)
                            sb.AppendLine($"Load: {sensor.Value:F1}%");
                        else if (sensor.SensorType == SensorType.Temperature)
                            sb.AppendLine($"Temp: {sensor.Value:F0}°C");
                        else if (sensor.SensorType == SensorType.SmallData)
                            sb.AppendLine($"Memory: {sensor.Value:F1} MB");
                    }
                }

                return found
                    ? sb.ToString()
                    : "Ingen GPU detekterad\n\nMöjliga orsaker:\n• LibreHardwareMonitor stöder ej GPU\n• Administratörsbehörighet krävs\n• GPU-drivrutiner saknas";
            }
            catch (Exception ex) { Logger.Warn("BuildGpuInfoText failed", ex); return "GPU-information ej tillgänglig"; }
        }

        private static string BuildHardwareInfoText()
        {
            try
            {
                var info = new StringBuilder();

                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        null, "SELECT Name FROM Win32_Processor", _wmiOptions);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.AppendLine($"CPU: {obj["Name"]}");
                        break;
                    }
                }
                catch (Exception ex) { Logger.Warn("Win32_Processor query failed", ex); }

                info.AppendLine($"Cores: {Environment.ProcessorCount}");
                var os = Environment.OSVersion;
                info.AppendLine($"OS: {OperatingSystemNames.GetFriendlyName(os)} (Build {os.Version.Build})");
                info.AppendLine($"User: {Environment.UserName}");
                return info.ToString();
            }
            catch (Exception ex) { Logger.Warn("BuildHardwareInfoText failed", ex); return "Hårdvaruinformation ej tillgänglig"; }
        }

        private static bool IsGpu(HardwareType type) =>
            type == HardwareType.GpuNvidia ||
            type == HardwareType.GpuAmd ||
            type == HardwareType.GpuIntel;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_computerLock)
            {
                try { _computer?.Close(); }
                catch (Exception ex) { Logger.Warn("Computer close failed", ex); }
                _computer = null;
            }

            if (_cpuCoreCounters != null)
            {
                foreach (var c in _cpuCoreCounters)
                {
                    try { c.Dispose(); }
                    catch (Exception ex) { Logger.Warn("Core counter dispose failed", ex); }
                }
            }

            try { _cpuCounter?.Dispose(); } catch { }
            try { _memoryCounter?.Dispose(); } catch { }

            Logger.Info("HardwareService disposed");
        }
    }

    internal sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
