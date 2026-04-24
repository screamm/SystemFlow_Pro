using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SystemMonitorApp.Models;
using SystemMonitorApp.Services;
using SystemMonitorApp.ViewModels;
using SystemMonitorApp.Views;

namespace SystemMonitorApp
{
    /// <summary>
    /// View layer. All hardware access happens in HardwareService; all state lives in
    /// MainViewModel. This code-behind renders panels imperatively from snapshots —
    /// Sprint 4 will replace the imperative panel rendering with XAML binding + ItemsControl.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private bool _panelsInitialized;

        // Cached TextBlocks — built once, updated per snapshot to avoid GC pressure.
        private TextBlock[] _cpuCoreTextBlocks = Array.Empty<TextBlock>();
        private TextBlock? _memoryInfoText;
        private TextBlock? _gpuInfoText;
        private TextBlock? _systemStatusText;
        private TextBlock? _systemUptimeText;
        private TextBlock? _hardwareInfoText;

        private readonly Dictionary<string, TextBlock> _thermalTextBlocks = new();
        private readonly Dictionary<string, TextBlock> _fanTextBlocks = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>Called from App.xaml.cs after splash so heavy init runs off the UI thread.</summary>
        public async Task InitializeAsync()
        {
            var hardware = new HardwareService();
            _viewModel = new MainViewModel(hardware);
            DataContext = _viewModel;

            InitializePanels(Environment.ProcessorCount, hardware.HardwareInfoText);

            _viewModel.SnapshotChanged += OnSnapshotChanged;

            await _viewModel.StartAsync();

            // Hardware info becomes available after StartAsync — refresh the cached panel text.
            if (_hardwareInfoText != null)
                _hardwareInfoText.Text = _viewModel.HardwareInfoText;

            Loaded += OnLoaded;
            StateChanged += OnWindowStateChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Reserved for future Loaded-time work.
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;
            if (!SettingsService.Current.PauseWhenMinimized) return;

            if (WindowState == WindowState.Minimized)
                _viewModel.PauseTimer();
            else
                _viewModel.ResumeTimer();
        }

        private void OnSnapshotChanged(object? sender, SystemSnapshot snapshot)
        {
            if (!_panelsInitialized) return;
            try
            {
                // Hero card values are already bound via DataContext in XAML for simple ones;
                // since XAML still uses x:Name for these, we set directly. Sprint 4 converts to binding.
                CpuValueText.Text = _viewModel?.CpuUsageDisplay ?? "";
                MemoryValueText.Text = _viewModel?.MemoryUsageDisplay ?? "";
                MemoryProgressBar.Value = snapshot.MemoryUsagePercent;
                GpuValueText.Text = _viewModel?.GpuUsageDisplay ?? "";
                TempValueText.Text = _viewModel?.TemperatureDisplay ?? "";

                UpdateCpuCoresPanel(snapshot.CpuCores);
                UpdateMemoryPanel(snapshot);
                UpdateGpuInfoPanel(snapshot.GpuInfoText);
                UpdateThermalPanel(snapshot.Thermals);
                UpdateCpuCoolingPanel(snapshot.CpuCooling);
                UpdateGpuCoolingPanel(snapshot.GpuCooling);
                UpdateSystemPanel(snapshot);
            }
            catch (Exception ex)
            {
                Logger.Warn("MainWindow.OnSnapshotChanged failed", ex);
            }
        }

        private void InitializePanels(int coreCount, string hardwareInfoText)
        {
            int displayCount = Math.Min(coreCount, 16);
            _cpuCoreTextBlocks = new TextBlock[displayCount];

            CpuCoresPanel.Children.Clear();
            for (int i = 0; i < displayCount; i++)
            {
                var tb = new TextBlock
                {
                    Style = (Style)FindResource("DataText"),
                    Margin = new Thickness(0, 2, 0, 0),
                    Text = $"Core {i}: 0%"
                };
                _cpuCoreTextBlocks[i] = tb;
                CpuCoresPanel.Children.Add(tb);
            }

            MemoryPanel.Children.Clear();
            _memoryInfoText = new TextBlock { Style = (Style)FindResource("DataText") };
            MemoryPanel.Children.Add(_memoryInfoText);

            GpuInfoPanel.Children.Clear();
            _gpuInfoText = new TextBlock
            {
                Style = (Style)FindResource("DataText"),
                TextWrapping = TextWrapping.Wrap
            };
            GpuInfoPanel.Children.Add(_gpuInfoText);

            SystemPanel.Children.Clear();
            _systemStatusText = new TextBlock { Style = (Style)FindResource("DataText") };
            _systemUptimeText = new TextBlock
            {
                Style = (Style)FindResource("DataText"),
                Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
            };
            SystemPanel.Children.Add(_systemStatusText);
            SystemPanel.Children.Add(_systemUptimeText);

            HardwarePanel.Children.Clear();
            _hardwareInfoText = new TextBlock
            {
                Style = (Style)FindResource("DataText"),
                TextWrapping = TextWrapping.Wrap,
                Text = hardwareInfoText
            };
            HardwarePanel.Children.Add(_hardwareInfoText);

            _panelsInitialized = true;
        }

        private void UpdateCpuCoresPanel(IReadOnlyList<CpuCoreInfo> cores)
        {
            for (int i = 0; i < _cpuCoreTextBlocks.Length && i < cores.Count; i++)
            {
                var tb = _cpuCoreTextBlocks[i];
                var core = cores[i];
                tb.Text = $"{core.Name}: {core.UsagePercent:F1}%";

                if (core.UsagePercent > 80)
                    tb.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                else if (core.UsagePercent > 60)
                    tb.Foreground = (SolidColorBrush)FindResource("WarnBrush");
                else
                    tb.Foreground = (SolidColorBrush)FindResource("AccentBrush");
            }
        }

        private void UpdateMemoryPanel(SystemSnapshot s)
        {
            if (_memoryInfoText == null) return;
            _memoryInfoText.Text = $"Använt: {s.UsedMemoryGB:F1} GB / {s.TotalMemoryGB:F1} GB\n" +
                                   $"Tillgängligt: {s.AvailableMemoryGB:F1} GB";
        }

        private void UpdateGpuInfoPanel(string text)
        {
            if (_gpuInfoText == null) return;
            _gpuInfoText.Text = text;
        }

        private void UpdateThermalPanel(IReadOnlyDictionary<string, float> thermals)
        {
            foreach (var kvp in thermals)
            {
                if (!_thermalTextBlocks.TryGetValue(kvp.Key, out var tb))
                {
                    tb = new TextBlock
                    {
                        Style = (Style)FindResource("DataText"),
                        Margin = new Thickness(0, 2, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    };
                    _thermalTextBlocks[kvp.Key] = tb;
                    ThermalPanel.Children.Add(tb);
                }

                string tempName = kvp.Key.Length > 25 ? kvp.Key.Substring(0, 22) + "..." : kvp.Key;
                SolidColorBrush brush;

                if (kvp.Value > 80) brush = (SolidColorBrush)FindResource("ErrorBrush");
                else if (kvp.Value > 60) brush = (SolidColorBrush)FindResource("WarnBrush");
                else brush = (SolidColorBrush)FindResource("AccentBrush");

                // Color-coded text — no emoji icons (Sprint 4 UI update).
                tb.Text = $"{tempName}: {kvp.Value:F0}°C";
                tb.Foreground = brush;
            }

            var toRemove = _thermalTextBlocks.Keys.Where(k => !thermals.ContainsKey(k)).ToList();
            foreach (var key in toRemove)
            {
                ThermalPanel.Children.Remove(_thermalTextBlocks[key]);
                _thermalTextBlocks.Remove(key);
            }

            if (_thermalTextBlocks.Count == 0 && ThermalPanel.Children.Count == 0)
            {
                ThermalPanel.Children.Add(new TextBlock
                {
                    Text = "Temperaturdata ej tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                });
            }
        }

        // Render the CPU cooling panel. Uses CoolingReadout entries which work on
        // every Windows 11 system because they come from CPU MSR + NVIDIA driver API,
        // not motherboard SuperIO (which may be blocked by Windows security or other apps).
        private void UpdateCpuCoolingPanel(IReadOnlyList<CoolingReadout> readouts)
        {
            CpuFansPanel.Children.Clear();
            if (readouts.Count == 0)
            {
                CpuFansPanel.Children.Add(new TextBlock
                {
                    Text = "Väntar på CPU-sensorer...",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                });
                return;
            }
            foreach (var r in readouts)
                CpuFansPanel.Children.Add(BuildCoolingRow(r));

            // If no fan readouts are present among CPU cooling data, show a concise
            // "why no RPM" note. SuperIO-based RPM reading requires BIOS to hand
            // over PWM control to the OS (Smart Fan 5 → Full Speed on Gigabyte).
            bool hasFanRow = false;
            foreach (var r in readouts)
            {
                if (r.DisplayValue.Contains("RPM") || r.DisplayValue.EndsWith("%"))
                { hasFanRow = true; break; }
            }
            if (!hasFanRow)
            {
                var hint = new TextBlock
                {
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 12, 0, 0),
                    Text = "Fläkt-RPM ej tillgänglig.\n" +
                           "Aktivera i BIOS: Smart Fan 5 / Q-Fan / Fan Xpert → \"Manual\" eller \"Full Speed\"."
                };
                CpuFansPanel.Children.Add(hint);
            }
        }

        private void UpdateGpuCoolingPanel(IReadOnlyList<CoolingReadout> readouts)
        {
            SystemFansPanel.Children.Clear();
            if (readouts.Count == 0)
            {
                SystemFansPanel.Children.Add(new TextBlock
                {
                    Text = "Ingen GPU-sensor tillgänglig",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush")
                });
                return;
            }
            foreach (var r in readouts)
                SystemFansPanel.Children.Add(BuildCoolingRow(r));
        }

        private TextBlock BuildCoolingRow(CoolingReadout readout)
        {
            SolidColorBrush brush = readout.Severity switch
            {
                CoolingSeverity.Critical => (SolidColorBrush)FindResource("ErrorBrush"),
                CoolingSeverity.Warning => (SolidColorBrush)FindResource("WarnBrush"),
                CoolingSeverity.Healthy => (SolidColorBrush)FindResource("AccentBrush"),
                CoolingSeverity.Idle => (SolidColorBrush)FindResource("TextMutedBrush"),
                _ => (SolidColorBrush)FindResource("TextSecondaryBrush")
            };

            string label = readout.Label.Length > 26
                ? readout.Label.Substring(0, 23) + "..."
                : readout.Label;

            return new TextBlock
            {
                Text = $"{label}: {readout.DisplayValue}",
                Style = (Style)FindResource("DataText"),
                Foreground = brush,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.NoWrap
            };
        }

        private void UpdateFanPanels(IReadOnlyDictionary<string, FanReading> fans)
        {
            foreach (var kvp in fans)
            {
                string key = kvp.Key;
                FanReading reading = kvp.Value;
                string displayName = key.Length > 20 ? key.Substring(0, 17) + "..." : key;

                if (!_fanTextBlocks.TryGetValue(key, out var tb))
                {
                    tb = new TextBlock
                    {
                        Style = (Style)FindResource("DataText"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 1, 0, 1)
                    };
                    _fanTextBlocks[key] = tb;

                    bool routeToGpuPanel = reading.IsGpu
                        || key.Contains("gpu", StringComparison.OrdinalIgnoreCase)
                        || key.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
                        || key.Contains("geforce", StringComparison.OrdinalIgnoreCase)
                        || key.Contains("radeon", StringComparison.OrdinalIgnoreCase);

                    if (routeToGpuPanel) SystemFansPanel.Children.Add(tb);
                    else CpuFansPanel.Children.Add(tb);
                }

                FormatFanText(tb, displayName, reading);
            }

            var toRemove = _fanTextBlocks.Keys.Where(k => !fans.ContainsKey(k)).ToList();
            foreach (var key in toRemove)
            {
                var tb = _fanTextBlocks[key];
                CpuFansPanel.Children.Remove(tb);
                SystemFansPanel.Children.Remove(tb);
                _fanTextBlocks.Remove(key);
            }

            EnsureFanEmptyPlaceholders();
        }

        private void FormatFanText(TextBlock tb, string displayName, FanReading reading)
        {
            // Color-coded text — no emoji icons (Sprint 4 UI update).
            if (reading.IsPercent)
            {
                tb.Text = $"{displayName}: {reading.RawValue:F0}%";
                tb.Foreground = reading.RawValue > 70
                    ? (SolidColorBrush)FindResource("WarnBrush")
                    : (SolidColorBrush)FindResource("AccentBrush");
                return;
            }

            if (reading.RawValue > 2000)
            {
                tb.Text = $"{displayName}: {reading.RawValue:F0} RPM";
                tb.Foreground = (SolidColorBrush)FindResource("AccentBrush");
            }
            else if (reading.RawValue > 800)
            {
                tb.Text = $"{displayName}: {reading.RawValue:F0} RPM";
                tb.Foreground = (SolidColorBrush)FindResource("WarnBrush");
            }
            else if (reading.RawValue > 0)
            {
                tb.Text = $"{displayName}: {reading.RawValue:F0} RPM";
                tb.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
            else
            {
                if (reading.IsGpu)
                {
                    tb.Text = $"{displayName}: Zero RPM Mode";
                    tb.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                }
                else
                {
                    tb.Text = $"{displayName}: Inaktiv";
                    tb.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                }
            }
        }

        private void EnsureFanEmptyPlaceholders()
        {
            string reportHint = HardwareDiagnostics.LastReportPath != null
                ? $"\n\nDiagnostikrapport: {HardwareDiagnostics.LastReportPath}"
                : "";

            if (CpuFansPanel.Children.Count == 0)
            {
                CpuFansPanel.Children.Add(new TextBlock
                {
                    Text = "Inga CPU/system-fläktar detekterade\n\n" +
                           "Moderkortets SuperIO-chip (ITE / Nuvoton) läses normalt för\n" +
                           "fläktdata, men kan blockeras av:\n" +
                           "• Windows 11 Insider / 25H2 begränsar port-I/O till LPC-bussen\n" +
                           "• Moderkortet finns i LHM:s databas men SuperIO-chipet är inte\n" +
                           "  mappat för just din variant/revision\n" +
                           "• Annat program (AORUS Engine, ASUS AI Suite, Armoury Crate)\n" +
                           "  har exklusiv åtkomst till SuperIO\n" +
                           "• Fläktar är direkt anslutna till PSU utan RPM-pinne\n\n" +
                           "Felsökning:\n" +
                           "1. Kör HWiNFO64 — om inte heller det hittar fläktarna är det\n" +
                           "   en Windows- eller BIOS-begränsning\n" +
                           "2. Stäng andra hårdvaruappar och starta om\n" +
                           "3. Uppdatera BIOS" +
                           reportHint,
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            if (SystemFansPanel.Children.Count == 0)
            {
                SystemFansPanel.Children.Add(new TextBlock
                {
                    Text = "Inga GPU-fläktar detekterade\n\n" +
                           "Vanligt på moderna GPU:er:\n" +
                           "• Zero RPM Mode — fläktarna stoppas under ~55°C\n" +
                           "• Hybrid-grafik där dGPU är avstängd\n" +
                           "• Drivrutinen exponerar inte load-sensor",
                    Style = (Style)FindResource("DataText"),
                    Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void UpdateSystemPanel(SystemSnapshot s)
        {
            if (_systemStatusText == null || _systemUptimeText == null) return;

            SolidColorBrush color = s.SystemStatus switch
            {
                SystemStatusEvaluator.High => (SolidColorBrush)FindResource("ErrorBrush"),
                SystemStatusEvaluator.Medium => (SolidColorBrush)FindResource("WarnBrush"),
                _ => (SolidColorBrush)FindResource("AccentBrush")
            };

            _systemStatusText.Text = $"Status: {s.SystemStatus}";
            _systemStatusText.Foreground = color;
            _systemUptimeText.Text = $"Uppdaterad: {s.Timestamp:HH:mm:ss}";
        }

        // ===== UI chrome handlers =====
        // Note: WindowChrome handles drag-to-move and double-click-to-maximize natively —
        // no explicit DragMove handler needed.

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
            => this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SettingsWindow { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    // Settings changed — propagate poll-interval change to the running timer.
                    _viewModel?.ApplyUpdatedSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Open SettingsWindow failed", ex);
                MessageBox.Show("Kunde inte öppna inställningar. Se loggen för detaljer.",
                    "SystemFlow Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new AboutWindow { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Warn("Open AboutWindow failed", ex);
                MessageBox.Show("Kunde inte öppna Om-dialogen. Se loggen för detaljer.",
                    "SystemFlow Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                StateChanged -= OnWindowStateChanged;
                Loaded -= OnLoaded;

                if (_viewModel != null)
                {
                    _viewModel.SnapshotChanged -= OnSnapshotChanged;
                    _viewModel.Dispose();
                    _viewModel = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("MainWindow.OnClosed cleanup failed", ex);
            }

            base.OnClosed(e);
        }
    }
}
