using SystemMonitorApp.Services;

namespace SystemMonitorApp.ViewModels
{
    /// <summary>
    /// View model for SettingsWindow — reads from and writes to SettingsService.
    /// </summary>
    public sealed class SettingsViewModel : ObservableObject
    {
        private int _pollIntervalMs;
        private bool _isCelsius;
        private bool _pauseWhenMinimized;
        private bool _startMinimized;

        public SettingsViewModel()
        {
            var s = SettingsService.Current;
            _pollIntervalMs = s.PollIntervalMs;
            _isCelsius = s.TemperatureUnit == "C";
            _pauseWhenMinimized = s.PauseWhenMinimized;
            _startMinimized = s.StartMinimized;
        }

        public int PollIntervalMs
        {
            get => _pollIntervalMs;
            set => SetField(ref _pollIntervalMs, value);
        }

        public bool IsCelsius
        {
            get => _isCelsius;
            set => SetField(ref _isCelsius, value);
        }

        public bool PauseWhenMinimized
        {
            get => _pauseWhenMinimized;
            set => SetField(ref _pauseWhenMinimized, value);
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set => SetField(ref _startMinimized, value);
        }

        public void Save()
        {
            SettingsService.Save(new AppSettings
            {
                PollIntervalMs = _pollIntervalMs,
                PauseWhenMinimized = _pauseWhenMinimized,
                StartMinimized = _startMinimized,
                TemperatureUnit = _isCelsius ? "C" : "F"
            });
        }
    }
}
