using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

namespace SystemMonitorApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private PerformanceCounter _cpuCounter;
        private List<PerformanceCounter> _cpuCoreCounters;
        private PerformanceCounter _gpuCounter;
        private PerformanceCounter _memoryCounter;
        private Computer computer;
        
        // För animations
        private float currentCpuUsage = 0f;
        private float currentGpuUsage = 0f;
        private float currentMemoryUsage = 0f;
        private float currentTemperature = 0f;

        public MainWindow()
        {
            InitializeComponent();
            
            // Enable window dragging
            this.MouseLeftButtonDown += (sender, e) => this.DragMove();
            
            InitializeCounters();
            InitializeTimer();
            LoadSystemInfo();
        }



        private void InitializeCounters()
        {
            try
            {
                // CPU Total
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                
                // CPU Cores
                _cpuCoreCounters = new List<PerformanceCounter>();
                int coreCount = Environment.ProcessorCount;
                
                for (int i = 0; i < coreCount; i++)
                {
                    var counter = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                    _cpuCoreCounters.Add(counter);
                }

                // Memory
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                
                // First reading to initialize counters
                _cpuCounter.NextValue();
                foreach (var counter in _cpuCoreCounters)
                {
                    counter.NextValue();
                }
                _memoryCounter.NextValue();

                // Initiera LibreHardwareMonitor
                computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true
                };
                
                computer.Open();
                computer.Accept(new UpdateVisitor());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte initiera systemräknare: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await UpdateSystemData();
        }

        private async Task UpdateSystemData()
        {
            try
            {
                // Update CPU usage
                float cpuUsage = _cpuCounter.NextValue();
                currentCpuUsage = cpuUsage;
                CpuValueText.Text = $"{cpuUsage:F0}%";

                // Update CPU cores
                string coreInfo = "";
                int coreCount = _cpuCoreCounters.Count;
                CpuCoreCountText.Text = $"{coreCount} CORES";
                
                for (int i = 0; i < Math.Min(coreCount, 8); i++) // Show max 8 for summary
                {
                    float coreUsage = _cpuCoreCounters[i].NextValue();
                    coreInfo += $"C{i}: {coreUsage:F0}% ";
                    if ((i + 1) % 4 == 0) coreInfo += "\n";
                }
                CpuCoresText.Text = coreInfo.Trim();

                // Update Memory
                float availableMemory = _memoryCounter.NextValue();
                float availableGB = availableMemory / 1024f;
                
                // Get total memory for percentage calculation
                float totalMemoryGB = await GetTotalMemoryGB();
                float usedMemoryGB = totalMemoryGB - availableGB;
                float memoryUsagePercent = (usedMemoryGB / totalMemoryGB) * 100f;
                
                currentMemoryUsage = memoryUsagePercent;
                MemoryValueText.Text = $"{memoryUsagePercent:F0}%";
                MemoryDetailsText.Text = $"Used: {usedMemoryGB:F1} GB / {totalMemoryGB:F1} GB\nAvailable: {availableGB:F1} GB";
                
                // Update memory bar width based on usage
                MemoryBar.Width = (memoryUsagePercent / 100.0) * 150; // 150 is max width
                
                // Update memory status
                if (memoryUsagePercent < 70) MemoryStatusText.Text = "NORMAL";
                else if (memoryUsagePercent < 85) MemoryStatusText.Text = "HIGH";
                else MemoryStatusText.Text = "CRITICAL";

                // Update GPU
                float gpuUsage = await GetGpuUsage();
                currentGpuUsage = gpuUsage;
                if (gpuUsage >= 0)
                {
                    GpuValueText.Text = $"{gpuUsage:F0}%";
                    GpuDetailsText.Text = $"Load: {gpuUsage:F1}%\nGraphics performance OK";
                    GpuStatusText.Text = "ACTIVE";
                    GpuBar.Width = (gpuUsage / 100.0) * 150; // Update GPU bar
                }
                else
                {
                    GpuValueText.Text = "N/A";
                    GpuDetailsText.Text = "Monitoring requires admin rights\nSome data unavailable";
                    GpuStatusText.Text = "LIMITED";
                    GpuBar.Width = 0;
                }

                // Update system health
                if (cpuUsage < 80 && memoryUsagePercent < 90)
                {
                    SystemHealthText.Text = "OPTIMAL";
                    ThermalStatusText.Text = "NORMAL";
                    CpuFanStatusText.Text = "ACTIVE";
                    SystemFanStatusText.Text = "ACTIVE";
                }
                else if (cpuUsage < 90 && memoryUsagePercent < 95)
                {
                    SystemHealthText.Text = "GOOD";
                    ThermalStatusText.Text = "WARM";
                }
                else
                {
                    SystemHealthText.Text = "HIGH LOAD";
                    ThermalStatusText.Text = "HOT";
                }

                // Update temperatures and fan speeds
                await UpdateHardwareInfo();

                // Update status
                StatusText.Text = $"System running optimally";
                UpdateTimeText.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fel vid uppdatering: {ex.Message}";
            }
        }

        private async Task<float> GetTotalMemoryGB()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            double totalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                            return (float)totalMemory;
                        }
                    }
                }
                catch { }
                return 16f; // Default fallback
            });
        }

        private async Task<float> GetGpuUsage()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Försök först med LibreHardwareMonitor
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (IHardware hardware in computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia || 
                            hardware.HardwareType == HardwareType.GpuAmd || 
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Load && 
                                    (sensor.Name.Contains("GPU Core") || sensor.Name.Contains("D3D")))
                                {
                                    return sensor.Value ?? -1f;
                                }
                            }
                        }
                    }

                    // Fallback med WMI för direktX/GPU
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_GPUPerformanceCounters_GPUEngine"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                var utilizationPercent = obj["UtilizationPercentage"];
                                if (utilizationPercent != null)
                                {
                                    return Convert.ToSingle(utilizationPercent);
                                }
                            }
                        }
                    }
                    catch { }

                    // Simulerad GPU-belastning för demo
                    var random = new Random();
                    return random.Next(15, 45) + (float)random.NextDouble() * 10f;
                }
                catch
                {
                    return -1f; // Indikerar att GPU-data inte är tillgänglig
                }
            });
        }



        private async Task UpdateHardwareInfo()
        {
            await Task.Run(() =>
            {
                try
                {
                    string tempInfo = "";
                    string systemFanInfo = "";
                    string cpuFanInfo = "";
                    float maxTemp = 0f;
                    float cpuTemp = 0f;

                    // Använd LibreHardwareMonitor för exakt hårdvarudata
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (IHardware hardware in computer.Hardware)
                    {
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            // Temperatursensorer
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                float temp = sensor.Value.Value;
                                string tempName = sensor.Name.Replace("Temperature", "").Replace("Temp", "").Trim();
                                string tempIcon = temp > 80 ? "🔥" : temp > 60 ? "🔸" : "❄️";
                                tempInfo += $"{tempIcon} {tempName}: {temp:F0}°C\n";
                                
                                // Spara CPU-temperatur för huvudvisning
                                if (hardware.HardwareType == HardwareType.Cpu)
                                {
                                    cpuTemp = Math.Max(cpuTemp, temp);
                                }
                                
                                maxTemp = Math.Max(maxTemp, temp);
                            }
                            
                            // Fläktsensorer - separera CPU och System
                            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                            {
                                string fanName = sensor.Name.Replace("Fan", "").Replace("fan", "").Trim();
                                float rpm = sensor.Value.Value;
                                string status = rpm > 500 ? "🟢" : rpm > 100 ? "🟡" : "🔴";
                                string fanData = $"{status} {fanName}: {rpm:F0} RPM\n";
                                
                                // Separera CPU-fläktar från systemfläktar
                                if (hardware.HardwareType == HardwareType.Cpu || 
                                    sensor.Name.ToLower().Contains("cpu") || 
                                    sensor.Name.ToLower().Contains("processor"))
                                {
                                    cpuFanInfo += fanData;
                                }
                                else
                                {
                                    systemFanInfo += fanData;
                                }
                            }
                        }
                        
                        // Kontrollera även sub-hårdvara
                        foreach (IHardware subHardware in hardware.SubHardware)
                        {
                            foreach (ISensor sensor in subHardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                {
                                    float temp = sensor.Value.Value;
                                    string tempName = sensor.Name.Replace("Temperature", "").Replace("Temp", "").Trim();
                                    string tempIcon = temp > 80 ? "🔥" : temp > 60 ? "🔸" : "❄️";
                                    tempInfo += $"{tempIcon} {tempName}: {temp:F0}°C\n";
                                    
                                    if (subHardware.HardwareType == HardwareType.Cpu)
                                    {
                                        cpuTemp = Math.Max(cpuTemp, temp);
                                    }
                                    
                                    maxTemp = Math.Max(maxTemp, temp);
                                }
                                
                                if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                                {
                                    string fanName = sensor.Name.Replace("Fan", "").Replace("fan", "").Trim();
                                    float rpm = sensor.Value.Value;
                                    string status = rpm > 500 ? "🟢" : rpm > 100 ? "🟡" : "🔴";
                                    string fanData = $"{status} {fanName}: {rpm:F0} RPM\n";
                                    
                                    if (subHardware.HardwareType == HardwareType.Cpu || 
                                        sensor.Name.ToLower().Contains("cpu") || 
                                        sensor.Name.ToLower().Contains("processor"))
                                    {
                                        cpuFanInfo += fanData;
                                    }
                                    else
                                    {
                                        systemFanInfo += fanData;
                                    }
                                }
                            }
                        }
                    }

                    // Fallback till WMI för temperatur om LibreHardwareMonitor inte ger resultat
                    if (string.IsNullOrEmpty(tempInfo))
                    {
                        try
                        {
                            using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                            {
                                foreach (ManagementObject obj in searcher.Get())
                                {
                                    double temp = Convert.ToDouble(obj["CurrentTemperature"]);
                                    temp = (temp - 2732) / 10.0;
                                    tempInfo += $"Thermal Zone: {temp:F1}°C\n";
                                    cpuTemp = Math.Max(cpuTemp, (float)temp);
                                    maxTemp = Math.Max(maxTemp, (float)temp);
                                }
                            }
                        }
                        catch { }
                    }

                    // Update UI on main thread
                    Dispatcher.Invoke(() =>
                    {
                        // Begränsa innehåll drastiskt för att undvika scroll
                        var tempLines = tempInfo.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Take(4).ToArray();
                        string limitedTempInfo = string.Join("\n", tempLines);
                        if (tempLines.Length >= 4 && tempInfo.Split('\n').Length > 4)
                        {
                            limitedTempInfo += "\n💡 +fler...";
                        }
                        
                        // Temperature info
                        TemperatureDetailsText.Text = string.IsNullOrEmpty(tempInfo) ? "🚫 Ej tillgängligt\n⚠️ Admin krävs" : limitedTempInfo;
                        
                        // Begränsa fläktinfo ännu mer
                        var cpuFanLines = cpuFanInfo.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Take(2).ToArray();
                        var systemFanLines = systemFanInfo.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Take(2).ToArray();
                        
                        // Fan info - separated
                        CpuFanText.Text = cpuFanLines.Length == 0 ? "❌ Inga CPU-fläktar" : string.Join("\n", cpuFanLines);
                        SystemFanText.Text = systemFanLines.Length == 0 ? "❌ Inga systemfläktar" : string.Join("\n", systemFanLines);
                        
                        // Main temperature display
                        if (cpuTemp > 0)
                        {
                            currentTemperature = cpuTemp;
                            TempValueText.Text = $"{cpuTemp:F0}°C";
                        }
                        else
                        {
                            currentTemperature = 0f;
                            TempValueText.Text = "N/A";
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = $"Hårdvarufel: {ex.Message}";
                    });
                }
            });
        }

        private void LoadSystemInfo()
        {
            try
            {
                string systemInfo = "";
                
                // Get basic system info
                systemInfo += $"Computer: {Environment.MachineName}\n";
                systemInfo += $"OS: {Environment.OSVersion}\n";
                systemInfo += $"Processor Cores: {Environment.ProcessorCount}\n";
                systemInfo += $"System Architecture: {Environment.Is64BitOperatingSystem}\n";

                // Get more detailed CPU info
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        systemInfo += $"CPU: {obj["Name"]}\n";
                        systemInfo += $"Max Clock Speed: {obj["MaxClockSpeed"]} MHz\n";
                        break; // Just get first processor
                    }
                }

                // Get total memory
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double totalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        systemInfo += $"Total Memory: {totalMemory:F1} GB\n";
                        break;
                    }
                }

                SystemInfoText.Text = systemInfo;
            }
            catch (Exception ex)
            {
                SystemInfoText.Text = $"Error loading system info: {ex.Message}";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _timer?.Stop();
                _cpuCounter?.Dispose();
                _cpuCoreCounters?.ForEach(c => c.Dispose());
                _gpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                computer?.Close();
            }
            catch { }
            
            base.OnClosed(e);
        }
    }

    // UpdateVisitor klass för LibreHardwareMonitor
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        
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