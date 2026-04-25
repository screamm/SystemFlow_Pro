# SystemFlow Pro v1.1.1

**Release date:** 2026-04-25

Quality and polish release after v1.1.0. Faster startup, full English UI,
better fan-detection guidance, and more accurate documentation.

## Highlights

- **Full English translation.** Every string in the UI, every code comment,
  every documentation file is now English. Status labels: "OPTIMAL" /
  "MEDIUM LOAD" / "HIGH LOAD".
- **~30% faster startup** (~11s → ~6-8s). LibreHardwareMonitor enumerates
  fewer hardware types, the diagnostic report runs in the background after
  the window is shown, and the splash minimum-display time is halved.
- **Better fan-detection guidance.** README now contains a vendor-specific
  BIOS table showing exactly which menu and setting unlocks fan RPM reading
  on Gigabyte, ASUS, MSI, and ASRock boards.

## Performance

- LibreHardwareMonitor `IsControllerEnabled`, `IsNetworkEnabled`,
  `IsPsuEnabled`, `IsBatteryEnabled` set to `false` — these were never read
  by any UI panel. Each disabled type saves 200-500ms of LHM enumeration
  time at startup.
- `HardwareDiagnostics.DumpReport` moved out of `HardwareService.InitializeAsync`
  into a background `Task.Run` invoked from `App.xaml.cs` after
  `mainWindow.Show()`. The disk write (~500ms, 179 KB) no longer blocks
  the splash → window transition.
- Diagnostic report is now **conditional**: written only if the motherboard
  sub-hardware tree is empty (real troubleshooting need) or if no recent
  report exists (>24h since last). Eliminates redundant disk writes on
  every start.
- `App.MinSplashMs`: 800 → 400. Splash still visible long enough to
  register but no longer adds 800ms when init is fast.

## Localization

- Full English translation across 35 files: UI, code comments, doc-comments,
  Logger messages, dialog text, README, PRIVACY, FAQ, ARCHITECTURE, BACKLOG,
  QA_CHECKLIST, RELEASE_NOTES, all sprint planning documents, all changelogs,
  SECURITY_ANALYSIS_REPORT, RELEASE_GUIDE, and build-script `REM` comments.
- Status constant rename: `"MEDIUMBELASTNING"` → `"MEDIUM LOAD"`,
  `"HÖG BELASTNING"` → `"HIGH LOAD"`. Tests reference the constants by
  name (`SystemStatusEvaluator.Medium`, `.High`) so all 40 tests still pass.
- Splash version label is now read dynamically from `Assembly.InformationalVersion`
  rather than being hardcoded.

## Hardware reading improvements

- `LibreHardwareMonitorLib` 0.9.4 → **0.9.3**. Avoids the documented
  IT8689E sensor-detection regression in 0.9.4.0
  ([LHM Issue #1569](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/1569)).
- `HardwareService.AddFanSensor` includes a name-based fallback for sensors
  whose `SensorType` is neither `Fan` nor `Control` but whose name contains
  "rpm" or "fan" — recovers fan readings on motherboards that categorize
  them under uncommon sensor types.
- `Win32_Fan` WMI query as a secondary source — rarely populated on desktop
  boards but works on systems with vendor utilities (e.g. Gigabyte Control
  Center) that register `acpimof.dll`.
- "FANS" panels renamed to **"COOLING"** and now show CPU MSR + GPU NVAPI
  data that works on every Windows 11 PC — the panel is no longer dependent
  on motherboard SuperIO access.
- BIOS hint text in CPU COOLING panel when fan RPM is unavailable, with
  one-line vendor instructions (Smart Fan 5 / Q-Fan / Fan Xpert → Manual).

## Security & robustness

- Admin requirement (`requireAdministrator`) restored after community
  feedback. Without admin, most LHM sensors return null on modern systems.
- `UpdateChecker` hardened: 1 MB response cap, URL scheme/host validation
  (only `https://*.github.com`).
- `SplashWindow.xaml.cs` nullability fixed (CS8618/CS8622 warnings gone).
- `AboutWindow.GetBuildDate` uses `AppContext.BaseDirectory` first to
  avoid IL3000 in single-file publish; legacy `Assembly.Location` fallback
  is wrapped in `#pragma warning disable IL3000`.
- App.xaml palette duplication removed — `MainWindow.xaml` is the single
  source of truth; `AccessibleFocusVisual` lives globally in `App.xaml`
  so all dialogs (Settings, About) get visible keyboard focus.

## Documentation

- README accurate: LibreHardwareMonitor version 0.9.3 (was 0.9.4), 40 tests
  (was 37), startup time documented, motherboard-agnostic data sources
  table added, BIOS vendor table for fan unlocking.
- `.gitignore`: ignores `system.md` (visionary-claude plugin profile) and
  `*.stackdump` (Cygwin/Git Bash crash dumps) so the working tree stays
  clean.

## Files changed

40+ files modified across UI, services, tests, and docs. No breaking
behavior changes — settings.json is forward-compatible with v1.1.0.

## Known limitations carried over

- CPU/chassis fan RPM still requires BIOS Smart Fan 5 / Q-Fan / Fan Xpert
  to be set to "Manual" (documented in README + FAQ).
- Distribution remains unsigned; SmartScreen warning expected on first
  launch (documented in README).
- LibreHardwareMonitor `Computer.Open()` takes ~3-5s — unavoidable
  flask-neck shared with HWiNFO64, FanControl, and AIDA64.
