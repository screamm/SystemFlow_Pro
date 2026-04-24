# SystemFlow Pro v1.1.0-beta.1 — Architecture + UI/UX modernization

**Release date:** TBD
**Branch:** `sprint-03-arkitektur` → `sprint-04-ui-ux`
**Status:** Beta — functionally complete, remaining: Sprint 5 (pipeline) + Sprint 6 (QA)

Major release combining Sprint 3 and Sprint 4. New architecture (MVVM-lite + testable
service layer) and complete UI modernization (Fluent icons, Aero Snap,
accessibility, Settings/About dialogs).

## Architecture (Sprint 3)

- **New folder structure:** `Models/`, `Services/`, `ViewModels/`, `Tests/`, `docs/`,
  `Views/`.
- **HardwareService** extracted from `MainWindow.xaml.cs`. All access to
  LibreHardwareMonitor and WMI is serialized via `_computerLock` — LHM is not
  thread-safe and the previous version raced `Accept()` from multiple threads.
- **SystemSnapshot / CpuCoreInfo / FanReading** as immutable records in `Models/`.
  Safe to pass across thread boundaries.
- **MainViewModel** owns the tick loop and exposes view-bindable properties.
  `ObservableObject` base for `INotifyPropertyChanged`. The ViewModel is testable
  with a mock `IHardwareService`.
- **MainWindow.xaml.cs** reduced from 1232 → ~365 lines. It is now a pure
  view — only handles rendering of snapshots from the ViewModel.
- **SystemStatusEvaluator** + **OperatingSystemNames** extracted as pure
  functions for testability.
- **37 unit tests** in `Tests/SystemFlow.Tests/` — xUnit + FluentAssertions.
  Tests status logic, OS names, FanReading and SystemSnapshot defaults.
- **docs/ARCHITECTURE.md** documents layers, threading, lifecycle, and decisions.

## UI/UX (Sprint 4)

- **WindowChrome restores Aero Snap.** The previous `WindowStyle="None"` +
  `AllowsTransparency="True"` broke Snap Layouts (Win+Z), maximize double-click
  on the titlebar, and Windows-native drag behavior. We now use
  `System.Windows.Shell.WindowChrome` with `CaptionHeight="48"` and
  `ResizeBorderThickness="6"` — all standard gestures work again.
- **Window dimensions:** 1400×900 (previously 1800×1300), MinWidth=1100,
  MinHeight=750. Now fits on 1366×768 — previously the bottom was clipped on 1080p.
- **Fluent Icons replace multicolored emoji.** `Segoe Fluent Icons, Segoe MDL2 Assets` fallback.
  Headers and chrome use glyph codes (&#xE945; power, &#xE7FC; gpu,
  &#xE977; storage, &#xE9CA; temperature, &#xE713; settings, &#xE946; info,
  &#xE921;/&#xE922;/&#xE8BB; for minimize/maximize/close). Panel rendering
  in code-behind has emoji prefixes removed — status is marked via color coding
  instead, matching Task Manager's professional style.
- **Accessibility — AutomationProperties + TabIndex + FocusVisual.**
  All interactive controls have `AutomationProperties.Name` +
  `AutomationProperties.HelpText`. TabIndex 101-105 on the chrome buttons for
  logical keyboard order. Hero cards have
  `AutomationProperties.LiveSetting="Polite"` so screen readers announce
  updates without interrupting the user. FocusVisualStyle with visible
  outline on focus.
- **WCAG contrast raised.** `TextMutedBrush` from `#94A3B8` (4.9:1) →
  `#A8B2C0` (5.5:1) on dark background — approved AA across the app.
- **Version v1.1.0 visible** in the header (previously v1.0.8 hardcoded).
- **Settings dialog** (`Views/SettingsWindow.xaml`). Configurable
  polling interval (500ms / 1s / 2s / 5s), temperature unit (°C/°F), pause
  when minimized, start minimized. Saved to
  `%APPDATA%\SystemFlow Pro\settings.json`. Polling interval is applied
  live without restart via `MainViewModel.ApplyUpdatedSettings()`.
- **About dialog** (`Views/AboutWindow.xaml`). Version + build date from
  Assembly, link to the GitHub repo and issues, third-party attribution
  (LibreHardwareMonitor MPL 2.0, .NET 9 MIT, Segoe Fluent Icons),
  hyperlinks open in the default browser.
- **Tooltips on hero cards** explain the metric and warning threshold
  (e.g. "CPU total load. Warning at 80%.").

## Files affected

### New files
- `Models/SystemSnapshot.cs`
- `Services/IHardwareService.cs`
- `Services/HardwareService.cs`
- `Services/SystemStatusEvaluator.cs` (+ `OperatingSystemNames`)
- `ViewModels/ObservableObject.cs`
- `ViewModels/MainViewModel.cs`
- `ViewModels/SettingsViewModel.cs`
- `Views/SettingsWindow.xaml(.cs)`
- `Views/AboutWindow.xaml(.cs)`
- `Tests/SystemFlow.Tests/SystemFlow.Tests.csproj`
- `Tests/SystemFlow.Tests/SystemStatusEvaluatorTests.cs`
- `Tests/SystemFlow.Tests/OperatingSystemNamesTests.cs`
- `Tests/SystemFlow.Tests/FanReadingTests.cs`
- `docs/ARCHITECTURE.md`

### Modified files
- `MainWindow.xaml` — completely rewritten, WindowChrome + Fluent Icons + accessibility
- `MainWindow.xaml.cs` — thin view layer, delegates to MainViewModel
- `SystemFlow_Pro.sln` — test project added
- `SystemMonitorApp.csproj` — version 1.1.0-beta.1

## Remaining for Sprint 5

- Self-contained single-file publish (README still lies)
- GitHub Actions CI/CD
- Auto-update via GitHub Releases
- THIRD_PARTY_LICENSES.txt
- Git history cleaned (releases/ committed)

## Remaining for Sprint 6

- Manual QA on multiple hardware configurations
- Beta with external testers
- Fix regressions
- Publish v1.1.0
