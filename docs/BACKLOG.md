# SystemFlow Pro — Backlog

Items that were not included in v1.1.0 but are worth considering for future
versions. Ordered by rough priority.

## High priority (v1.1.1 — bugfix release)

- [ ] **°F conversion** in rendering. The Settings dialog offers °C/°F but
      conversion of displayed values must be completed.
- [ ] **Git history cleanup** — run `git filter-repo --path releases
      --invert-paths` to remove 800+ MB of old release artifacts from
      the history. Destructive — coordinate with any forks.
- [ ] **Remove obsolete build scripts** (`build_release_v1.0.X.bat`).
      Fully replaced by the unified `build.bat`.

## Not planned

- **Code signing.** Not planned. SystemFlow Pro is distributed unsigned as
  open source — the cost (200-500 USD/year) is not justified for a free
  project. Users handle SmartScreen via "More info" → "Run anyway" and
  can review all code on GitHub. Documented in README and FAQ.

## Medium priority (v1.2.0 — feature release)

- [ ] **Mica backdrop** (Windows 11). DwmSetWindowAttribute interop via
      the Win32 API. Fallback to a flat background on Win10.
- [ ] **Auto-update download.** Today the update check only shows a link.
      Implement Velopack or Squirrel for in-app download +
      auto-patching.
- [ ] **English localization.** Currently only Swedish. `Resources/Strings.resx`
      + `Strings.en.resx`.
- [ ] **Export metrics to CSV.** "Save metrics" button in the header or
      Settings that writes the current snapshot to CSV.
- [ ] **History mode.** Keep the latest N snapshots in memory, display as
      mini-charts per hero card.
- [ ] **Full XAML binding conversion.** MainWindow.xaml.cs still renders
      panels imperatively. Convert to `ItemsControl` +
      `DataTemplate` + value converters for pure MVVM.
- [ ] **Settings: filter sensors.** Let the user hide specific
      sensors that are not of interest.
- [ ] **Widget mode.** Always-on-top mini window with only the hero values.

## Low priority (v1.3+ or backlog)

- [ ] **Cross-platform via MAUI or Avalonia** — Linux/macOS support.
      Requires a different sensor source (`/sys/class/hwmon/` on Linux).
- [ ] **Charts over time** (full history view with a chart per sensor).
- [ ] **Dark/light themes** — currently dark only.
- [ ] **More languages** — German, Finnish, Norwegian.
- [ ] **Age-spread sensor support** — clear indication when a sensor is not
      supported + why (log entry in the UI).
- [ ] **Benchmark integration** — run Cinebench/3DMark and show the results
      alongside live data.
- [ ] **MQTT/REST endpoint** — expose sensors to Home Assistant.

## Technical debt

- [ ] **More unit tests** — ViewModel behavior (tick skip, pause-resume,
      settings apply). Requires mock IHardwareService + Dispatcher stub.
- [ ] **Integration tests** for HardwareService — requires hardware, tag
      `[Trait("Category", "Integration")]`.
- [ ] **Performance benchmarks** — BenchmarkDotNet on `HardwareService.CollectSnapshot`
      to catch regressions.
- [ ] **Remove old build_release_v1.0.X.bat** — obsolete after Sprint 5.
- [ ] **Legacy scripts folder** — `scripts/claude_autonomous_loop.py` is
      dev tooling and does not belong in a production repo.
- [ ] **README: build steps** — document `build.bat [version]` better,
      including expected output size.

## Open questions

- Should we add Sentry or another crash reporter? Opt-in, GDPR compliant.
- Which additional localizations are worth doing — check GitHub Issues for requests.
- Are there users who want a CLI mode (no UI, only logging to CSV)?
