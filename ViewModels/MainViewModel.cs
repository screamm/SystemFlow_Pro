using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SystemMonitorApp.Models;
using SystemMonitorApp.Services;

namespace SystemMonitorApp.ViewModels
{
    /// <summary>
    /// Orchestrates the tick loop, owns the current snapshot, and exposes view-bindable
    /// properties. The view subscribes to <see cref="SnapshotChanged"/> to refresh UI.
    ///
    /// Testing: construct with an <see cref="IHardwareService"/> stub and drive Tick manually
    /// via <see cref="ApplySnapshotForTest"/>.
    /// </summary>
    public sealed class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IHardwareService _hardware;
        private readonly SemaphoreSlim _tickGate = new(1, 1);
        private DispatcherTimer? _timer;
        private volatile bool _disposed;

        private SystemSnapshot _snapshot = new();

        public MainViewModel(IHardwareService hardware)
        {
            _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        }

        /// <summary>Raised on UI thread after a new snapshot has been applied.</summary>
        public event EventHandler<SystemSnapshot>? SnapshotChanged;

        public SystemSnapshot Snapshot
        {
            get => _snapshot;
            private set
            {
                _snapshot = value;
                OnPropertyChanged(nameof(Snapshot));
                OnPropertyChanged(nameof(CpuUsageDisplay));
                OnPropertyChanged(nameof(MemoryUsageDisplay));
                OnPropertyChanged(nameof(GpuUsageDisplay));
                OnPropertyChanged(nameof(TemperatureDisplay));
                OnPropertyChanged(nameof(SystemStatusDisplay));
                OnPropertyChanged(nameof(LastUpdatedDisplay));
                OnPropertyChanged(nameof(MemoryUsagePercent));
                OnPropertyChanged(nameof(IsAdminMode));
            }
        }

        public string CpuUsageDisplay => $"{_snapshot.CpuUsagePercent:F0}%";
        public string MemoryUsageDisplay => $"{_snapshot.MemoryUsagePercent:F0}%";
        public double MemoryUsagePercent => _snapshot.MemoryUsagePercent;
        public string GpuUsageDisplay => _snapshot.GpuUsagePercent >= 0 ? $"{_snapshot.GpuUsagePercent:F0}%" : "N/A";
        public string TemperatureDisplay => _snapshot.AverageTemperatureC > 0 ? $"{_snapshot.AverageTemperatureC:F0}°C" : "N/A";
        public string SystemStatusDisplay => _snapshot.SystemStatus;
        public string LastUpdatedDisplay => $"Uppdaterad: {_snapshot.Timestamp:HH:mm:ss}";
        public bool IsAdminMode => _hardware.IsRunningAsAdmin;
        public string HardwareInfoText => _hardware.HardwareInfoText;

        public async Task StartAsync()
        {
            await _hardware.InitializeAsync();
            int intervalMs = SettingsService.Current.PollIntervalMs;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
            _timer.Tick += OnTick;
            _timer.Start();

            // Immediate first sample so UI doesn't wait PollIntervalMs to show data.
            _ = TickOnceAsync();
        }

        /// <summary>Re-reads settings and applies changes to the running timer.</summary>
        public void ApplyUpdatedSettings()
        {
            if (_timer == null) return;
            int newInterval = SettingsService.Current.PollIntervalMs;
            if ((int)_timer.Interval.TotalMilliseconds != newInterval)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(newInterval);
                Logger.Info($"Poll interval updated to {newInterval}ms");
            }
        }

        public void PauseTimer()
        {
            if (_timer?.IsEnabled == true)
            {
                _timer.Stop();
                Logger.Info("ViewModel timer paused");
            }
        }

        public void ResumeTimer()
        {
            if (_timer != null && !_timer.IsEnabled)
            {
                _timer.Start();
                Logger.Info("ViewModel timer resumed");
            }
        }

        private async void OnTick(object? sender, EventArgs e) => await TickOnceAsync();

        private async Task TickOnceAsync()
        {
            if (_disposed) return;
            if (!await _tickGate.WaitAsync(0))
            {
                Logger.Info("ViewModel tick skipped — previous tick still running");
                return;
            }

            try
            {
                if (_disposed) return;
                var snap = await _hardware.CollectSnapshotAsync();
                if (_disposed) return;

                // Already on UI thread because DispatcherTimer marshals; but guard explicitly
                // in case someone calls TickOnceAsync from a different thread during testing.
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                    await Application.Current.Dispatcher.InvokeAsync(() => Snapshot = snap);
                else
                    Snapshot = snap;

                SnapshotChanged?.Invoke(this, snap);
            }
            catch (Exception ex)
            {
                Logger.Warn("ViewModel tick failed", ex);
            }
            finally
            {
                try { _tickGate.Release(); }
                catch (ObjectDisposedException) { }
                catch (Exception ex) { Logger.Warn("Tick gate release failed", ex); }
            }
        }

        /// <summary>Test hook — applies a snapshot synchronously without touching hardware.</summary>
        internal void ApplySnapshotForTest(SystemSnapshot snap) => Snapshot = snap;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
            }

            // Drain in-flight tick.
            try
            {
                if (_tickGate.Wait(TimeSpan.FromSeconds(2)))
                    _tickGate.Release();
            }
            catch (Exception ex) { Logger.Warn("Tick drain on dispose failed", ex); }

            try { _tickGate.Dispose(); } catch { }
            _hardware.Dispose();
            Logger.Info("MainViewModel disposed");
        }
    }
}
