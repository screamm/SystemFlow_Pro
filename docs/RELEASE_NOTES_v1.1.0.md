# SystemFlow Pro v1.1.0 — Release Notes (template)

**Release date:** TBD (after Sprint 6 QA approval)
**Download:** https://github.com/screamm/SystemFlow_Pro/releases/tag/v1.1.0

First production-grade release since the v1.0.x series. Complete rewrite of
the hardware layer, new UI stack, ~3x better performance, accessible to
screen readers, portable open source distribution.

---

## Download

**Recommended:** `SystemFlow-Pro-v1.1.0-win-x64.zip` (~80 MB)

Extract, run `SystemFlow-Pro.exe`. No .NET installation required.
No admin required.

## Highlights

### Performance

- **~3x faster tick cycle.** The UI thread is decoupled from hardware reading.
  The entire snapshot collection (LibreHardwareMonitor + WMI + Performance Counters)
  now runs on a background thread via a semaphore-gated pipeline.
- **No more flicker.** UI panels are built **once** at startup; snapshots
  only update text/color. Previously the entire tree was rebuilt every second.
- **~3% continuous CPU overhead** (previously 8-15%) on modern machines.
- **Pause on minimize.** When the app is minimized, polling stops entirely. CPU
  drops to ~0%. Resumes on restore.

### Stability

- **No more silent crashes.** A global `DispatcherUnhandledException` handler
  catches all unhandled exceptions, logs the stack trace to
  `%APPDATA%\SystemFlow Pro\logs\`, and shows a friendly error message.
- **Race condition fix.** LibreHardwareMonitor is not thread-safe — all
  access is now serialized via `_computerLock`. Previous versions raced
  `Accept()` calls from multiple threads, which could produce corrupt sensor values.
- **Tick pile-up protected.** If a tick takes longer than the interval, the
  next one is skipped instead of being queued up.
- **Dispose race protected.** Shutdown waits for the running tick (up to 2s)
  before releasing resources.

### UI / UX

- **Aero Snap works again.** The WindowChrome API replaced the previous
  `AllowsTransparency="True"` implementation that broke Snap Layouts,
  maximize double-click, and Windows native drag behavior.
- **Fluent Icons.** All multicolored emoji icons have been replaced with
  Segoe Fluent Icons (with MDL2 Assets fallback for Windows 10). More
  professional, consistent per Windows version, scales correctly at different DPI.
- **Settings dialog.** Configurable polling interval (500ms-5s),
  temperature unit (°C/°F), pause-on-minimize, start-minimized.
  Changes are saved to `%APPDATA%\SystemFlow Pro\settings.json`.
- **About dialog.** Version, build date, link to the GitHub repo and issues,
  third-party attribution.
- **Accessibility.** `AutomationProperties.Name` + `AutomationProperties.HelpText`
  on all interactive controls. Narrator support. TabIndex for logical
  keyboard navigation. Visible focus outline.
- **WCAG contrast raised** from 4.9:1 to 5.5:1 on muted text (passes AA).
- **Smaller window dimensions.** 1400×900 default (MinWidth 1100,
  MinHeight 750) — now fits on 1366×768.

### Architecture (under the hood)

- **Extracted HardwareService** — hardware reading isolated from the View layer.
- **MVVM-lite** with MainViewModel orchestrating the tick loop and exposing
  a snapshot property via `INotifyPropertyChanged`.
- **Immutable records** (`SystemSnapshot`, `CpuCoreInfo`, `FanReading`) for
  thread-boundary-safe data transfer.
- **37 unit tests** (xUnit + FluentAssertions) in `Tests/SystemFlow.Tests/`.

### Infrastructure

- **Self-contained single-file distribution.** No .NET runtime required on
  end-user machines.
- **GitHub Actions CI/CD.** Automatic build + test on every push.
  The release workflow builds and publishes a zip on tag push.
- **Auto update check** against the GitHub Releases API at app startup (non-blocking).
- **LICENSE, PRIVACY, THIRD_PARTY_LICENSES** fully documented.

### Administrator no longer required

Previously, `app.manifest` required `requireAdministrator` even for regular
users — which broke for non-admin accounts. Now the app runs as `asInvoker`
with graceful degradation for sensors that require admin.

## Breaking changes

- **Fan values more accurate.** Previously, `value * 30f` was multiplied for
  all values ≤100, which produced false RPM values for zero-RPM GPUs and
  pump sensors. Now `SensorType.Fan` is shown as RPM and `SensorType.Control`
  as %. If you have saved screenshots for comparison, values may look
  different — the new ones are correct.
- **Icons changed.** Multicolored emoji (fire, gamepad, disk, thermometer) have been replaced with Fluent
  Icons. Status is now indicated via color coding instead of emoji in text.
- **Settings moved.** No previous settings location — now
  `%APPDATA%\SystemFlow Pro\settings.json`. First run creates defaults.
- **Logs.** New logs in `%APPDATA%\SystemFlow Pro\logs\` with 5 MB
  rotation, latest 5 files retained.

## Migration from v1.0.x

- No data migration needed — the app only reads sensors live
- The old version can be uninstalled by deleting the folder
- The new version is portable — extract the .zip wherever you like

## Known limitations

- **°F conversion**: Settings includes the °C/°F choice but conversion in the UI
  will be completed in v1.1.1.
- **Mica backdrop (Windows 11)**: Fluent acrylic background is being implemented in
  v1.2.
- **Auto-update download**: v1.1.0 only shows "new version available"
  with a link. Automatic download via Squirrel/Velopack is coming in v1.2.
- **Languages**: Swedish UI only. English is coming in v1.2.

## Contributors

- David Rydgren — development
- *(Beta testers — fill in after Sprint 6)*

Thanks to everyone who reports bugs and suggests improvements on GitHub.

## Next version

v1.1.1 (bugfix release) or v1.2.0 (features) — see
[Issues](https://github.com/screamm/SystemFlow_Pro/issues) for the backlog.
