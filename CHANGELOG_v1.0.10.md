# SystemFlow Pro v1.0.10 — Performance & resource management

**Release date:** TBD
**Branch:** `sprint-02-prestanda`

Second security release on the path to v1.1.0. Focus: decouple the UI thread from
hardware reading, eliminate flickering, stop memory leaks, and introduce
user-configurable polling frequency.

## Performance

- **New thread pipeline.** `Timer_Tick` is now `async void` with a `SemaphoreSlim` gate.
  Hardware reading (`CollectSnapshot`) runs on a background thread via `Task.Run`;
  the UI thread receives a finished `SystemSnapshot` and updates cached
  TextBlocks (`ApplySnapshotToUI`). If a tick overlaps the previous one it is skipped
  rather than queued — no pile-up on slow systems.
- **Panel caching.** All panels build TextBlocks **once** in
  `InitializePanels` and afterwards only update `.Text` and `.Foreground`.
  The previous `Panel.Children.Clear() + Add()` was the dominant GC source.
  Dynamic panels (thermal, fans) use a dictionary cache and add or
  remove entries only when the sensor set changes.
- **Timer pauses when the window is minimized.** `StateChanged` handler stops
  polling when `WindowState == Minimized` (configurable via
  `settings.json → PauseWhenMinimized`). CPU usage drops to ~0% in
  the minimized state.
- **Configurable polling interval.** Stored in `%APPDATA%\SystemFlow Pro\settings.json`
  (`PollIntervalMs`, default 2000). Clamped to 500-60000ms.
  Settings UI comes in Sprint 4.

## Stability

- **Splash init decoupled.** The `MainWindow` constructor is now minimal —
  hardware init (LibreHardwareMonitor open + Accept + WMI memory read) runs
  in `InitializeAsync()` on a background thread. The splash no longer freezes 1-3s
  while the hardware enumerates. `Task.Delay(2000)` hack replaced with an adaptive
  minimum display time (800ms).
- **Fan heuristic fixed.** `sensor.SensorType == Fan` → RPM with no conversion.
  `sensor.SensorType == Control` → percent (PWM). The previous arbitrary
  `value * 30f` multiplication that made GPUs in zero-RPM mode show
  incorrect values has been removed. Percent fans are now shown as "%", RPM fans as
  "RPM" — not false conversions.
- **Event unsubscribes.** `OnClosed` deregisters `Timer.Tick`, `StateChanged`,
  `Loaded`. Fully releases the `MainWindow` reference so GC can collect the object.
  `_tickGate.Dispose()` added.

## Code structure (preparation for Sprint 3)

- Internal `SystemSnapshot` class + `FanReading` record struct are used to
  pass data from the background thread to the UI. Sprint 3 moves these to
  `Models/` as `public sealed record`.
- `ReadGpuUsage`, `ReadAverageTemperature`, `ReadThermalData`, `ReadFanData`
  are now synchronous — they run from `Task.Run` in `CollectSnapshot`.
- New `Services/SettingsService.cs` with JSON serialization and clamping.

## Expected result

- Tick cost: ~300ms → ~30-50ms (10x faster)
- UI freeze per tick: eliminated
- GC Gen0/min during polling: ~20 → ~2
- Continuous CPU overhead: 8-15% → <3% on reference machine
- Minimized: ~0% CPU (timer stopped)

## Remaining for Sprint 3

- Still a god class, no MVVM or binding
- No unit tests
- Hardware logic not extracted to a service

## Files affected

- `MainWindow.xaml.cs` — major rewrite, snapshot pattern
- `App.xaml.cs` — calls `InitializeAsync`, adaptive splash timing
- `SystemMonitorApp.csproj` — version bump
- `Services/SettingsService.cs` — new file
