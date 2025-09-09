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
            
            InitializeCounters();
            InitializeTimer();
            LoadSystemInfo();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
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

                // Initiera LibreHardwareMonitor - aktivera alla möjliga hårdvarutyper
                computer = new Computer
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

                // Update Memory
                float availableMemory = _memoryCounter.NextValue();
                float availableGB = availableMemory / 1024f;
                
                // Get total memory for percentage calculation
                float totalMemoryGB = await GetTotalMemoryGB();
                float usedMemoryGB = totalMemoryGB - availableGB;
                float memoryUsagePercent = (usedMemoryGB / totalMemoryGB) * 100f;
                
                currentMemoryUsage = memoryUsagePercent;
                MemoryValueText.Text = $"{memoryUsagePercent:F0}%";
                MemoryProgressBar.Value = memoryUsagePercent;

                // Update GPU
                float gpuUsage = await GetGpuUsage();
                currentGpuUsage = gpuUsage;
                if (gpuUsage >= 0)
                {
                    GpuValueText.Text = $"{gpuUsage:F0}%";
                }
                else
                {
                    GpuValueText.Text = "N/A";
                }

                // Update Temperature
                float avgTemp = await GetAverageTemperature();
                if (avgTemp > 0)
                {
                    currentTemperature = avgTemp;
                    TempValueText.Text = $"{avgTemp:F0}°C";
                }
                else
                {
                    TempValueText.Text = "N/A";
                }

                // Update detailed panels
                await UpdateDetailedPanels();
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }

        private async Task UpdateDetailedPanels()
        {
            try
            {
                // Update CPU Cores Panel
                UpdateCpuCoresPanel();

                // Update Memory Panel
                UpdateMemoryPanel();

                // Update GPU Info Panel
                await UpdateGpuInfoPanel();

                // Update Thermal Panel
                await UpdateThermalPanel();

                // Update Fan Panels
                await UpdateFanPanels();

                // Update System Panel
                UpdateSystemPanel();

                // Update Hardware Panel
                UpdateHardwarePanel();
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }

        private void UpdateCpuCoresPanel()
        {
            CpuCoresPanel.Children.Clear();
            
            try
            {
                int coreCount = _cpuCoreCounters.Count;
                for (int i = 0; i < Math.Min(coreCount, 16); i++)
                {
                    float coreUsage = _cpuCoreCounters[i].NextValue();
                    
                    var coreText = new TextBlock
                    {
                        Text = $"Core {i}: {coreUsage:F1}%",
                        Style = (Style)FindResource("DataText"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    
                    // Color based on usage
                    if (coreUsage > 80)
                        coreText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                    else if (coreUsage > 60)
                        coreText.Foreground = (SolidColorBrush)FindResource("WarnBrush");
                    else
                        coreText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                    
                    CpuCoresPanel.Children.Add(coreText);
                }
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Data ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                CpuCoresPanel.Children.Add(errorText);
            }
        }

        private void UpdateMemoryPanel()
        {
            MemoryPanel.Children.Clear();
            
            try
            {
                float availableMemory = _memoryCounter.NextValue();
                float availableGB = availableMemory / 1024f;
                float totalMemoryGB = GetTotalMemoryGB().Result;
                float usedMemoryGB = totalMemoryGB - availableGB;
                
                var memoryInfo = new TextBlock
                {
                    Text = $"Använt: {usedMemoryGB:F1} GB / {totalMemoryGB:F1} GB\nTillgängligt: {availableGB:F1} GB",
                    Style = (Style)FindResource("DataText")
                };
                
                MemoryPanel.Children.Add(memoryInfo);
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Minnesdata ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                MemoryPanel.Children.Add(errorText);
            }
        }

        private async Task UpdateGpuInfoPanel()
        {
            GpuInfoPanel.Children.Clear();
            
            try
            {
                var gpuInfo = await GetGpuInfo();
                
                var gpuText = new TextBlock
                {
                    Text = gpuInfo,
                    Style = (Style)FindResource("DataText"),
                    TextWrapping = TextWrapping.Wrap
                };
                
                GpuInfoPanel.Children.Add(gpuText);
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "GPU-data ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                GpuInfoPanel.Children.Add(errorText);
            }
        }

        private async Task UpdateThermalPanel()
        {
            ThermalPanel.Children.Clear();
            
            try
            {
                var thermalData = await GetThermalData();
                
                foreach (var temp in thermalData)
                {
                    var tempText = new TextBlock
                    {
                        Style = (Style)FindResource("DataText"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };

                    // Set color based on temperature with better formatting for text wrapping
                    string tempName = temp.Key.Length > 25 ? temp.Key.Substring(0, 22) + "..." : temp.Key;
                    
                    if (temp.Value > 80)
                    {
                        tempText.Text = $"🔥 {tempName}: {temp.Value:F0}°C";
                        tempText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                    }
                    else if (temp.Value > 60)
                    {
                        tempText.Text = $"🔸 {tempName}: {temp.Value:F0}°C";
                        tempText.Foreground = (SolidColorBrush)FindResource("WarnBrush");
                    }
                    else
                    {
                        tempText.Text = $"❄️ {tempName}: {temp.Value:F0}°C";
                        tempText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                    }
                    
                    // Enable text wrapping for better display on smaller screens
                    tempText.TextWrapping = TextWrapping.Wrap;
                    
                    ThermalPanel.Children.Add(tempText);
                }
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Temperaturdata ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                ThermalPanel.Children.Add(errorText);
            }
        }

        private async Task UpdateFanPanels()
        {
            CpuFansPanel.Children.Clear();
            SystemFansPanel.Children.Clear();
            
            try
            {
                var fanData = await GetFanData();
                
                foreach (var fan in fanData)
                {
                    var fanText = new TextBlock
                    {
                        Style = (Style)FindResource("DataText"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };

                    // Clean up fan name for better display
                    string fanName = fan.Key.Length > 20 ? fan.Key.Substring(0, 17) + "..." : fan.Key;
                    
                    // Set status based on RPM
                    if (fan.Value > 2000)
                    {
                        fanText.Text = $"🟢 {fanName}: {fan.Value:F0} RPM";
                        fanText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                    }
                    else if (fan.Value > 800)
                    {
                        fanText.Text = $"🟡 {fanName}: {fan.Value:F0} RPM";
                        fanText.Foreground = (SolidColorBrush)FindResource("WarnBrush");
                    }
                    else if (fan.Value > 0)
                    {
                        fanText.Text = $"🔴 {fanName}: {fan.Value:F0} RPM";
                        fanText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                    }
                    else
                    {
                        // Check if this is a GPU fan that might be in zero RPM mode
                        if (fan.Key.ToLower().Contains("gpu") && 
                            (fan.Key.ToLower().Contains("nvidia") || fan.Key.ToLower().Contains("geforce")))
                        {
                            fanText.Text = $"❄️ {fanName}: Zero RPM Mode";
                            fanText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                        }
                        else
                        {
                            fanText.Text = $"⚫ {fanName}: Inaktiv";
                            fanText.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                        }
                    }
                    
                    // Enable text wrapping and set margin
                    fanText.TextWrapping = TextWrapping.Wrap;
                    fanText.Margin = new Thickness(0, 1, 0, 1);
                    
                    // Categorize fans
                    if (fan.Key.ToLower().Contains("gpu") || 
                        fan.Key.ToLower().Contains("nvidia") ||
                        fan.Key.ToLower().Contains("geforce") ||
                        fan.Key.ToLower().Contains("radeon"))
                    {
                        // GPU fans go to SystemFansPanel (now labeled as GPU-FLÄKTAR)
                        SystemFansPanel.Children.Add(fanText);
                    }
                    else
                    {
                        // All other fans (CPU + System fans like IT8689E) go to CPU section
                        CpuFansPanel.Children.Add(fanText);
                    }
                }
                
                // Add informative message if no real fans found
                if (CpuFansPanel.Children.Count == 0)
                {
                    CpuFansPanel.Children.Add(new TextBlock
                    {
                        Text = "Inga CPU/system-fläktar detekterade\n\nTroliga orsaker:\n• Moderkortet exponerar inte RPM-data\n• Fläktar anslutna direkt till PSU\n• Behöver administratörsbehörighet",
                        Style = (Style)FindResource("DataText"),
                        Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                
                if (SystemFansPanel.Children.Count == 0)
                {
                    SystemFansPanel.Children.Add(new TextBlock
                    {
                        Text = "Inga GPU-fläktar detekterade\n\nDetta är normalt om:\n• GPU-fläktar ej exponerade av drivrutin\n• LibreHardwareMonitor ej stöder ditt grafikkort\n• Moderna GPU:er har Zero RPM Mode\n• Passiv kylning eller låg temperatur\n• Äldre system rapporterar som procent/kontroll",
                        Style = (Style)FindResource("DataText"),
                        Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Fläktdata ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                CpuFansPanel.Children.Add(errorText);
                SystemFansPanel.Children.Add(errorText);
            }
        }

        private void UpdateSystemPanel()
        {
            SystemPanel.Children.Clear();
            
            try
            {
                string systemStatus = "OPTIMAL";
                SolidColorBrush statusColor = (SolidColorBrush)FindResource("AccentBrush");
                
                if (currentCpuUsage > 80 || currentMemoryUsage > 85 || currentTemperature > 75)
                {
                    systemStatus = "HÖRG BELASTNING";
                    statusColor = (SolidColorBrush)FindResource("ErrorBrush");
                }
                else if (currentCpuUsage > 60 || currentMemoryUsage > 70 || currentTemperature > 60)
                {
                    systemStatus = "MEDIUMBELASTNING";
                    statusColor = (SolidColorBrush)FindResource("WarnBrush");
                }
                
                var statusText = new TextBlock
                {
                    Text = $"Status: {systemStatus}",
                    Style = (Style)FindResource("DataText"),
                    Foreground = statusColor
                };
                
                var uptimeText = new TextBlock
                {
                    Text = $"Uppdaterad: {DateTime.Now:HH:mm:ss}",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                
                SystemPanel.Children.Add(statusText);
                SystemPanel.Children.Add(uptimeText);
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Systemstatus ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                SystemPanel.Children.Add(errorText);
            }
        }

        private void UpdateHardwarePanel()
        {
            HardwarePanel.Children.Clear();
            
            try
            {
                var hardwareInfo = GetHardwareInfo();
                
                var hardwareText = new TextBlock
                {
                    Text = hardwareInfo,
                    Style = (Style)FindResource("DataText"),
                    TextWrapping = TextWrapping.Wrap
                };
                
                HardwarePanel.Children.Add(hardwareText);
            }
            catch
            {
                var errorText = new TextBlock
                {
                    Text = "Hårdvaruinformation ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                };
                HardwarePanel.Children.Add(errorText);
            }
        }

        private async Task<float> GetTotalMemoryGB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong totalBytes = (ulong)obj["TotalPhysicalMemory"];
                        return totalBytes / (1024f * 1024f * 1024f); // Convert to GB
                    }
                }
            }
            catch
            {
                return 0f; // Return 0 if unable to detect memory
            }
            return 0f;
        }

        private async Task<float> GetGpuUsage()
        {
            try
            {
                if (computer != null)
                {
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia || 
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                                {
                                    return sensor.Value ?? 0f;
                                }
                            }
                        }
                    }
                }
                
                // No simulated data - return 0 if no real GPU data found
                return 0f;
            }
            catch
            {
                return -1f; // Indicates error
            }
        }

        private async Task<float> GetAverageTemperature()
        {
            try
            {
                if (computer != null)
                {
                    computer.Accept(new UpdateVisitor());
                    
                    List<float> temperatures = new List<float>();
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                temperatures.Add(sensor.Value.Value);
                            }
                        }
                    }
                    
                    return temperatures.Count > 0 ? temperatures.Average() : 0f;
                }
                
                return 0f;
            }
            catch
            {
                return 0f;
            }
        }

        private async Task<Dictionary<string, float>> GetThermalData()
        {
            var temperatures = new Dictionary<string, float>();
            
            try
            {
                if (computer != null)
                {
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                float tempValue = sensor.Value.Value;
                                
                                // Validate temperature reading (filter out obviously incorrect readings)
                                if (tempValue > 0 && tempValue < 150) // Reasonable temp range 1-149°C
                                {
                                    string name = $"{hardware.Name} {sensor.Name}";
                                    // Clean up sensor name for better display
                                    name = name.Replace("Temperature", "Temp").Replace("Package", "Pkg");
                                    temperatures[name] = tempValue;
                                }
                            }
                        }
                    }
                }
                
                // No simulated data - return empty if no real temperatures found
            }
            catch
            {
                // Return empty on error - no fake data
            }
            
            return temperatures;
        }

        private async Task<Dictionary<string, float>> GetFanData()
        {
            var fans = new Dictionary<string, float>();
            
            try
            {
                if (computer != null)
                {
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        // Force hardware update for GPU specifically
                        if (hardware.HardwareType == HardwareType.GpuNvidia || 
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            hardware.Update();
                        }
                        
                        foreach (var sensor in hardware.Sensors)
                        {
                            // Check for Fan sensors
                            if (sensor.SensorType == SensorType.Fan)
                            {
                                string name = $"{hardware.Name} {sensor.Name}";
                                float fanSpeed = sensor.Value ?? 0f;
                                
                                // Handle systems that report fan speed as percentage (0-100) instead of RPM
                                // If value is suspiciously low for RPM (like 30-39 for GPU), likely percentage
                                if (fanSpeed > 0 && fanSpeed < 200 && 
                                    (hardware.HardwareType == HardwareType.GpuNvidia ||
                                     hardware.HardwareType == HardwareType.GpuAmd ||
                                     hardware.HardwareType == HardwareType.GpuIntel ||
                                     name.ToLower().Contains("gpu")))
                                {
                                    // Convert percentage to estimated RPM (multiply by ~50)
                                    // This is an approximation: 0% = 0 RPM, 100% = ~5000 RPM
                                    fanSpeed = fanSpeed * 50f;
                                }
                                
                                fans[name] = fanSpeed;
                            }
                            // Check for Control sensors (fans often reported as Control)
                            else if (sensor.SensorType == SensorType.Control && 
                                    (sensor.Name.ToLower().Contains("fan") || 
                                     sensor.Name.ToLower().Contains("pump")))
                            {
                                string name = $"{hardware.Name} {sensor.Name}";
                                float fanSpeed = sensor.Value ?? 0f;
                                
                                // Handle percentage to RPM conversion for Control sensors too
                                if (fanSpeed > 0 && fanSpeed < 200 && 
                                    (hardware.HardwareType == HardwareType.GpuNvidia ||
                                     hardware.HardwareType == HardwareType.GpuAmd ||
                                     hardware.HardwareType == HardwareType.GpuIntel ||
                                     name.ToLower().Contains("gpu")))
                                {
                                    fanSpeed = fanSpeed * 50f;
                                }
                                
                                fans[name] = fanSpeed;
                            }
                            // Check for RPM sensors (some hardware reports RPM directly)
                            else if (sensor.Name.ToLower().Contains("rpm") || 
                                    sensor.Name.ToLower().Contains("fan"))
                            {
                                string name = $"{hardware.Name} {sensor.Name}";
                                float fanSpeed = sensor.Value ?? 0f;
                                fans[name] = fanSpeed;
                            }
                            // For GPU specifically, also check all Control sensors
                            else if ((hardware.HardwareType == HardwareType.GpuNvidia ||
                                     hardware.HardwareType == HardwareType.GpuAmd ||
                                     hardware.HardwareType == HardwareType.GpuIntel) &&
                                    sensor.SensorType == SensorType.Control)
                            {
                                string name = $"{hardware.Name} {sensor.Name}";
                                float fanSpeed = sensor.Value ?? 0f;
                                fans[name] = fanSpeed;
                            }
                        }
                        
                        // Check sub-hardware (like CPU packages, GPU sub-components, etc.)
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            subHardware.Update(); // Force update
                            
                            foreach (var sensor in subHardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Fan)
                                {
                                    string name = $"{subHardware.Name} {sensor.Name}";
                                    float fanSpeed = sensor.Value ?? 0f;
                                    fans[name] = fanSpeed;
                                }
                                else if (sensor.SensorType == SensorType.Control && 
                                        (sensor.Name.ToLower().Contains("fan") || 
                                         sensor.Name.ToLower().Contains("pump")))
                                {
                                    string name = $"{subHardware.Name} {sensor.Name}";
                                    float fanSpeed = sensor.Value ?? 0f;
                                    fans[name] = fanSpeed;
                                }
                                // Check for RPM sensors
                                else if (sensor.Name.ToLower().Contains("rpm") || 
                                        sensor.Name.ToLower().Contains("fan"))
                                {
                                    string name = $"{subHardware.Name} {sensor.Name}";
                                    float fanSpeed = sensor.Value ?? 0f;
                                    fans[name] = fanSpeed;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return empty on error
            }
            
            return fans;
        }

        private async Task<string> GetGpuInfo()
        {
            try
            {
                string gpuInfo = "GPU Information:\n";
                bool foundGpu = false;
                
                if (computer != null)
                {
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia || 
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foundGpu = true;
                            gpuInfo += $"{hardware.Name}\n";
                            
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.Value.HasValue)
                                {
                                    if (sensor.SensorType == SensorType.Load)
                                        gpuInfo += $"Load: {sensor.Value:F1}%\n";
                                    else if (sensor.SensorType == SensorType.Temperature)
                                        gpuInfo += $"Temp: {sensor.Value:F0}°C\n";
                                    else if (sensor.SensorType == SensorType.SmallData)
                                        gpuInfo += $"Memory: {sensor.Value:F1} MB\n";
                                }
                            }
                        }
                    }
                }
                
                if (!foundGpu)
                {
                    gpuInfo = "Ingen GPU detekterad\n\nMöjliga orsaker:\n• LibreHardwareMonitor stöder ej GPU\n• Administratörsbehörighet krävs\n• GPU-drivrutiner saknas";
                }
                
                return gpuInfo;
            }
            catch
            {
                return "GPU-information ej tillgänglig\nKräver administratörsbehörighet";
            }
        }

        private string GetHardwareInfo()
        {
            try
            {
                string info = "";
                
                // Processor info
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info += $"CPU: {obj["Name"]}\n";
                        break;
                    }
                }
                
                // Memory info
                info += $"Cores: {Environment.ProcessorCount}\n";
                // Få mer användarvänlig OS-info
            var os = Environment.OSVersion;
            var friendlyName = GetFriendlyOSName(os);
            info += $"OS: {friendlyName} (Build {os.Version.Build})\n";
                info += $"User: {Environment.UserName}\n";
                
                return info;
            }
            catch
            {
                return "Hårdvaruinformation ej tillgänglig";
            }
        }

        private void LoadSystemInfo()
        {
            // Initial load - data will be updated by timer
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private string GetFriendlyOSName(OperatingSystem os)
        {
            if (os.Platform == PlatformID.Win32NT)
            {
                var version = os.Version;
                
                if (version.Major == 10)
                {
                    // Windows 11 börjar med build 22000
                    if (version.Build >= 22000)
                    {
                        return "Windows 11";
                    }
                    else
                    {
                        return "Windows 10";
                    }
                }
                else if (version.Major == 6)
                {
                    if (version.Minor == 3) return "Windows 8.1";
                    if (version.Minor == 2) return "Windows 8";
                    if (version.Minor == 1) return "Windows 7";
                }
                
                return $"Windows NT {version.Major}.{version.Minor}";
            }
            
            return os.ToString();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _timer?.Stop();
                computer?.Close();
                
                foreach (var counter in _cpuCoreCounters ?? new List<PerformanceCounter>())
                {
                    counter?.Dispose();
                }
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _gpuCounter?.Dispose();
            }
            catch { }
            
            base.OnClosed(e);
        }
    }

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