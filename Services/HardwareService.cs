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
