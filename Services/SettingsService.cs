using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// User settings persisted as JSON to %APPDATA%\SystemFlow Pro\settings.json.
    /// Mutations do not push live — components should re-read on relevant events or use events.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>Polling interval in milliseconds. Default 2000 (2 sec).</summary>
        public int PollIntervalMs { get; set; } = 2000;

        /// <summary>Pause timer when window is minimized. Default true.</summary>
        public bool PauseWhenMinimized { get; set; } = true;

        /// <summary>Start app minimized to taskbar. Default false.</summary>
        public bool StartMinimized { get; set; } = false;

        /// <summary>Temperature display unit. "C" (Celsius) or "F" (Fahrenheit).</summary>
        public string TemperatureUnit { get; set; } = "C";
    }

    public static class SettingsService
    {
        private static readonly string _settingsPath;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        private static AppSettings _current = new();

        static SettingsService()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SystemFlow Pro",
                "settings.json");

            _current = Load();
        }

        public static AppSettings Current => _current;

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _current = new AppSettings();
                    Save(_current);
                    return _current;
                }

                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                _current = settings ?? new AppSettings();

                // Clamp values to safe ranges
                if (_current.PollIntervalMs < 500) _current.PollIntervalMs = 500;
                if (_current.PollIntervalMs > 60_000) _current.PollIntervalMs = 60_000;
                if (_current.TemperatureUnit != "C" && _current.TemperatureUnit != "F")
                    _current.TemperatureUnit = "C";

                return _current;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load settings, using defaults", ex);
                _current = new AppSettings();
                return _current;
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
                _current = settings;
                Logger.Info($"Settings saved. Poll={settings.PollIntervalMs}ms, PauseMin={settings.PauseWhenMinimized}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
            }
        }
    }
}
