# SystemFlow Pro v1.0.9 — Stabilization & security

**Release date:** TBD
**Branch:** `sprint-01-stabilisering`

First of three security releases on the path to v1.1.0. Focus: eliminate active
bugs, silent catch blocks, unnecessary admin requirements, and async anti-patterns.

## Security improvements

- **Admin rights no longer required.** `app.manifest` changed from
  `requireAdministrator` → `asInvoker`. The app works for regular users;
  sensors that require admin (certain MSR reads) degrade to "N/A".
  Fallback UI displays information about admin status.
- **Global error handling.** `App.xaml.cs` registers
  `DispatcherUnhandledException`, `TaskScheduler.UnobservedTaskException`, and
  `AppDomain.CurrentDomain.UnhandledException`. Crashes are logged and a
  user-friendly error message is shown instead of a silent crash.
- **Structured logging.** New `Logger` class writes to
  `%APPDATA%\SystemFlow Pro\logs\app-{date}.log` with rotation at 5 MB
  (the 5 most recent files are kept). 13 previously empty `catch {}` blocks
  replaced with `Logger.Warn(...)` calls that preserve safe default values.
- **WMI timeouts.** All `ManagementObjectSearcher` calls now have a 2-second
  timeout via `EnumerationOptions`. Prevents the app from hanging on broken
  WMI providers.

## Stability improvements

- **Deadlock fix.** `GetTotalMemoryGB().Result` which was called synchronously
  on the UI thread in `UpdateMemoryPanel` has been removed. Total physical
  memory is now read **once** in the constructor and cached in `_totalMemoryGB` —
  the value never changes during runtime.
- **One hardware update per tick.** `computer.Accept(new UpdateVisitor())`
  was previously called 5-7 times per tick (GpuUsage, AvgTemp, ThermalData,
  FanData, GpuInfo). Now Accept is called **once** first in `UpdateSystemData`
  and a shared `UpdateVisitor` instance is reused. Reduces race conditions
  in LibreHardwareMonitor and eliminates per-tick allocation.
- **Typo fixed.** "HORG LOAD" → "HIGH LOAD" in the system status.

## Code quality

- **Modern C# enabled.** `<Nullable>enable</Nullable>`,
  `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`.
  `CS1998` (async without await) is now treated as an error.
- **async cleanup.** Hardware readers (`GetGpuUsage`, `GetAverageTemperature`,
  `GetThermalData`, `GetGpuInfo`) are now synchronous — they had `async Task<T>`
  without `await`, which caused a CS1998 warning and still ran on the UI thread.
  (Sprint 2 moves the entire tick work to a background thread.)
- **Dead code removed:** `_gpuCounter` field (never assigned),
  `EstimateFanSpeedFromTemperature` method (never called).
- **Event unsubscribe.** `_timer.Tick -= Timer_Tick` in `OnClosed` to
  release the WPF object when the window is closed.

## Minor UI fixes

- `Icon="app.ico"` added to MainWindow and SplashWindow — the correct icon
  is now shown in the taskbar and Alt+Tab.

## Files affected

- `App.xaml.cs` — rewritten with global exception handling
- `MainWindow.xaml.cs` — rewritten, ~25% less logic remaining after cleanup
- `MainWindow.xaml` — Icon added
- `SplashWindow.xaml` — Icon added
- `app.manifest` — `asInvoker`
- `SystemMonitorApp.csproj` — nullable + language settings
- `Services/Logger.cs` — new file

## Remaining for Sprint 2

- The UI thread is still blocked by WMI calls (~20-200ms per tick)
- UI panels are torn down and rebuilt every second (GC pressure)
- Timer does not pause when the window is minimized
- %→RPM heuristic is still a guess (sensor.Name-based)
- Splash init runs on the UI thread (1-3s freeze)
