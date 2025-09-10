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
        
        // Enhanced hardware detection tracking
        private bool _isAdminMode = false;
        private Dictionary<string, string> _hardwareCapabilities = new Dictionary<string, string>();
        private Dictionary<string, DateTime> _lastFanDetection = new Dictionary<string, DateTime>();
        
        // UI caching to prevent flickering
        private Dictionary<string, TextBlock> _fanTextBlocks = new Dictionary<string, TextBlock>();
        private bool _fanPanelsInitialized = false;

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
            // Check if running with admin privileges
            _isAdminMode = IsRunningAsAdministrator();
            
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
                string adminNote = _isAdminMode ? "" : "\n\nTips: Kör som administratör för bättre hårdvaruåtkomst.";
                MessageBox.Show($"Kunde inte initiera systemräknare: {ex.Message}{adminNote}", "Fel", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
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
            try
            {
                var fanData = await GetFanData();
                
                // Only clear panels if we haven't initialized them yet or if fan count changed significantly
                if (!_fanPanelsInitialized || Math.Abs(fanData.Count - _fanTextBlocks.Count) > 2)
                {
                    CpuFansPanel.Children.Clear();
                    SystemFansPanel.Children.Clear();
                    _fanTextBlocks.Clear();
                    _fanPanelsInitialized = true;
                }
                
                foreach (var fan in fanData)
                {
                    // Handle special status message for no fan data
                    if (fan.Key == "Fan Data Status")
                    {
                        if (!_fanTextBlocks.ContainsKey(fan.Key))
                        {
                            var statusText = new TextBlock
                            {
                                Style = (Style)FindResource("DataText"),
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 5, 0, 5),
                                Text = "⚠️ No real fan data available - Try Administrator mode",
                                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)) // Orange
                            };
                            
                            _fanTextBlocks[fan.Key] = statusText;
                            CpuFansPanel.Children.Add(statusText);
                        }
                        continue;
                    }
                    
                    // Clean up fan name for better display
                    string fanName = fan.Key.Length > 20 ? fan.Key.Substring(0, 17) + "..." : fan.Key;
                    
                    // Check if we already have a TextBlock for this fan
                    if (!_fanTextBlocks.ContainsKey(fan.Key))
                    {
                        // Create new TextBlock only if it doesn't exist
                        var fanText = new TextBlock
                        {
                            Style = (Style)FindResource("DataText"),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 1, 0, 1)
                        };
                        
                        _fanTextBlocks[fan.Key] = fanText;
                        
                        // Add to appropriate panel
                        if (fan.Key.ToLower().Contains("gpu") || 
                            fan.Key.ToLower().Contains("nvidia") || 
                            fan.Key.ToLower().Contains("geforce") || 
                            fan.Key.ToLower().Contains("radeon") || 
                            fan.Key.ToLower().Contains("amd"))
                        {
                            SystemFansPanel.Children.Add(fanText);
                        }
                        else
                        {
                            CpuFansPanel.Children.Add(fanText);
                        }
                    }
                    
                    // Update existing TextBlock content
                    var existingFanText = _fanTextBlocks[fan.Key];
                    
                    // Set status based on RPM
                    if (fan.Value > 2000)
                    {
                        existingFanText.Text = $"🟢 {fanName}: {fan.Value:F0} RPM";
                        existingFanText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                    }
                    else if (fan.Value > 800)
                    {
                        existingFanText.Text = $"🟡 {fanName}: {fan.Value:F0} RPM";
                        existingFanText.Foreground = (SolidColorBrush)FindResource("WarnBrush");
                    }
                    else if (fan.Value > 0)
                    {
                        existingFanText.Text = $"🔴 {fanName}: {fan.Value:F0} RPM";
                        existingFanText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                    }
                    else
                    {
                        // Check if this is a GPU fan that might be in zero RPM mode
                        if (fan.Key.ToLower().Contains("gpu") && 
                            (fan.Key.ToLower().Contains("nvidia") || fan.Key.ToLower().Contains("geforce")))
                        {
                            existingFanText.Text = $"❄️ {fanName}: Zero RPM Mode";
                            existingFanText.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                        }
                        else
                        {
                            existingFanText.Text = $"⚫ {fanName}: Inaktiv";
                            existingFanText.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                        }
                    }
                }
                
                // Add informative message if no real fans found
                if (CpuFansPanel.Children.Count == 0)
                {
                    string detectionStatus = GetDetectionStatusMessage();
                    string adminStatus = _isAdminMode ? "✅ Administratörsbehörighet: Ja" : "⚠️ Administratörsbehörighet: Nej (kör som admin för bättre hårdvaruåtkomst)";
                    
                    CpuFansPanel.Children.Add(new TextBlock
                    {
                        Text = $"Inga CPU/system-fläktar detekterade\n\n{adminStatus}\n\n{detectionStatus}\n\nTroliga orsaker:\n• Moderkortet exponerar inte RPM-data\n• Fläktar anslutna direkt till PSU\n• Äldre hårdvara saknar sensor-stöd",
                        Style = (Style)FindResource("DataText"),
                        Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                
                if (SystemFansPanel.Children.Count == 0)
                {
                    string detectionStatus = GetDetectionStatusMessage();
                    string adminStatus = _isAdminMode ? "✅ Administratörsbehörighet: Ja" : "⚠️ Administratörsbehörighet: Nej (kör som admin för bättre hårdvaruåtkomst)";
                    
                    SystemFansPanel.Children.Add(new TextBlock
                    {
                        Text = $"Inga GPU-fläktar detekterade\n\n{adminStatus}\n\n{detectionStatus}\n\nDetta är normalt på äldre system:\n• GPU-fläktar ej exponerade av drivrutin\n• Zero RPM Mode vid låga temperaturer\n• Passiv kylning\n• Rapporterar som procent istället för RPM",
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
            
            // Try multiple detection strategies for better compatibility
            await TryLibreHardwareMonitorFans(fans);
            await TryWindowsManagementFans(fans);
            await TryEstimatedFansFromTemperature(fans);
            
            return fans;
        }
        
        private async Task TryLibreHardwareMonitorFans(Dictionary<string, float> fans)
        {
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
            catch (Exception ex)
            {
                Debug.WriteLine($"LibreHardwareMonitor fan detection failed: {ex.Message}");
                _hardwareCapabilities["LibreHardwareMonitor"] = $"Failed: {ex.Message}";
            }
        }
        
        private async Task TryWindowsManagementFans(Dictionary<string, float> fans)
        {
            try
            {
                await Task.Run(() =>
                {
                    // Try Windows Management Instrumentation as fallback
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Fan"))
                    {
                        foreach (ManagementObject fan in searcher.Get())
                        {
                            try
                            {
                                var name = fan["Name"]?.ToString() ?? "Unknown Fan";
                                var speed = fan["DesiredSpeed"]?.ToString();
                                
                                if (!string.IsNullOrEmpty(speed) && float.TryParse(speed, out float rpm))
                                {
                                    fans[$"WMI {name}"] = rpm;
                                }
                            }
                            catch (Exception fanEx)
                            {
                                Debug.WriteLine($"WMI fan reading error: {fanEx.Message}");
                            }
                        }
                    }
                    
                    // Try thermal zone approach for fan estimation
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_TemperatureProbe"))
                    {
                        foreach (ManagementObject probe in searcher.Get())
                        {
                            try
                            {
                                var name = probe["Name"]?.ToString() ?? "Unknown Probe";
                                var temp = probe["CurrentReading"]?.ToString();
                                
                                if (!string.IsNullOrEmpty(temp) && float.TryParse(temp, out float temperature))
                                {
                                    // Don't show fake estimated values - only show real fan data
                                    Debug.WriteLine($"Temperature data available for {name}: {temperature}°C, but no real fan data");
                                }
                            }
                            catch (Exception probeEx)
                            {
                                Debug.WriteLine($"Temperature probe reading error: {probeEx.Message}");
                            }
                        }
                    }
                });
                
                if (fans.Any(f => f.Key.StartsWith("WMI")))
                {
                    _hardwareCapabilities["WindowsManagement"] = "Supported";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Windows Management fan detection failed: {ex.Message}");
                _hardwareCapabilities["WindowsManagement"] = $"Failed: {ex.Message}";
            }
        }
        
        private async Task TryEstimatedFansFromTemperature(Dictionary<string, float> fans)
        {
            try
            {
                // Only add a message if no real fan data was found
                if (!fans.Any())
                {
                    fans["Fan Data Status"] = 0; // Special key to indicate no real data
                    _hardwareCapabilities["FanData"] = "No real fan data available - check Administrator mode";
                }
                else if (fans.Count < 2) // Very few fans detected
                {
                    _hardwareCapabilities["FanData"] = $"Limited fan data ({fans.Count} fans detected)";
                }
                else
                {
                    _hardwareCapabilities["FanData"] = $"Real fan data available ({fans.Count} fans detected)";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fan data assessment failed: {ex.Message}");
                _hardwareCapabilities["FanData"] = "Fan data assessment failed";
            }
        }
        
        private float EstimateFanSpeedFromTemperature(float temperature, string componentName)
        {
            // Temperature-based fan speed estimation for older hardware
            if (temperature < 40) return 800;   // Low temp = low fan speed
            if (temperature < 50) return 1200;  // Medium temp = medium fan speed  
            if (temperature < 65) return 2000;  // High temp = high fan speed
            if (temperature < 80) return 3000;  // Very high temp = very high fan speed
            return 4000; // Critical temp = maximum fan speed
        }
        
        private async Task<Dictionary<string, float>> GetDetailedTemperatureData()
        {
            var temperatures = new Dictionary<string, float>();
            
            try
            {
                await Task.Run(() =>
                {
                    computer.Accept(new UpdateVisitor());
                    
                    foreach (var hardware in computer.Hardware)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                string name = $"{hardware.Name} {sensor.Name}";
                                temperatures[name] = sensor.Value.Value;
                            }
                        }
                        
                        // Check sub-hardware
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            foreach (var sensor in subHardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                {
                                    string name = $"{subHardware.Name} {sensor.Name}";
                                    temperatures[name] = sensor.Value.Value;
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Temperature data collection failed: {ex.Message}");
            }
            
            return temperatures;
        }
        
        private string GetDetectionStatusMessage()
        {
            var statusParts = new List<string>();
            
            foreach (var capability in _hardwareCapabilities)
            {
                string emoji = capability.Value.StartsWith("Failed") ? "❌" : "✅";
                string simpleName = capability.Key switch
                {
                    "LibreHardwareMonitor" => "LibreHardwareMonitor",
                    "WindowsManagement" => "Windows WMI",
                    "TemperatureEstimation" => "Temperaturbaserad uppskattning",
                    _ => capability.Key
                };
                statusParts.Add($"{emoji} {simpleName}: {capability.Value}");
            }
            
            return statusParts.Count > 0 
                ? $"Detektionsstatus:\n{string.Join("\n", statusParts)}" 
                : "Detektionsstatus: Initialiserar...";
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