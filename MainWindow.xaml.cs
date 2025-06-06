using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();
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
                CpuUsageText.Text = $"CPU Total: {cpuUsage:F1}%";

                // Update CPU cores
                string coreInfo = "";
                for (int i = 0; i < _cpuCoreCounters.Count; i++)
                {
                    float coreUsage = _cpuCoreCounters[i].NextValue();
                    coreInfo += $"Core {i}: {coreUsage:F1}%\n";
                }
                CpuCoresText.Text = coreInfo;

                // Update Memory
                float availableMemory = _memoryCounter.NextValue();
                MemoryText.Text = $"Available Memory: {availableMemory:F0} MB";

                // Update temperatures and fan speeds
                await UpdateHardwareInfo();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private async Task UpdateHardwareInfo()
        {
            await Task.Run(() =>
            {
                try
                {
                    string tempInfo = "";
                    string fanInfo = "";

                    // Använd LibreHardwareMonitor för exakt hårdvarudata
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (IHardware hardware in computer.Hardware)
                    {
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            // Temperatursensorer
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                string tempName = $"{hardware.Name} - {sensor.Name}";
                                tempInfo += $"{tempName}: {sensor.Value.Value:F1}°C\n";
                            }
                            
                            // Fläktsensorer
                            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                            {
                                string fanName = $"{hardware.Name} - {sensor.Name}";
                                fanInfo += $"{fanName}: {sensor.Value.Value:F0} RPM\n";
                            }
                        }
                        
                        // Kontrollera även sub-hårdvara
                        foreach (IHardware subHardware in hardware.SubHardware)
                        {
                            foreach (ISensor sensor in subHardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                {
                                    string tempName = $"{subHardware.Name} - {sensor.Name}";
                                    tempInfo += $"{tempName}: {sensor.Value.Value:F1}°C\n";
                                }
                                
                                if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                                {
                                    string fanName = $"{subHardware.Name} - {sensor.Name}";
                                    fanInfo += $"{fanName}: {sensor.Value.Value:F0} RPM\n";
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
                                }
                            }
                        }
                        catch { }
                    }

                    // Update UI on main thread
                    Dispatcher.Invoke(() =>
                    {
                        TemperatureText.Text = string.IsNullOrEmpty(tempInfo) ? "Temperaturdata ej tillgänglig" : tempInfo;
                        FanText.Text = string.IsNullOrEmpty(fanInfo) ? "Fläktdata ej tillgänglig" : fanInfo;
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSystemInfo();
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