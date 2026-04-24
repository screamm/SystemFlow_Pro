# SystemFlow Pro — Architecture Overview

**Version:** Sprint 3 (post-refactor) → v1.1.0-beta.1
**Status:** MVVM transition mode — service layer + ViewModel in place, XAML binding
partial (Sprint 4 completes this).

## Layers (overview)

```
┌─────────────────────────────────────────────────────────────┐
│ View Layer  —  MainWindow.xaml + MainWindow.xaml.cs        │
│   • XAML: markup, visual resources, hero cards             │
│   • Code-behind: imperative panel rendering from snapshots │
│     (Sprint 4 replaces with ItemsControl + DataTemplate)   │
└─────────────────────────┬───────────────────────────────────┘
                          │ DataContext + SnapshotChanged event
┌─────────────────────────▼───────────────────────────────────┐
│ ViewModel Layer  —  ViewModels/MainViewModel.cs            │
│   • ObservableObject (INotifyPropertyChanged base)         │
│   • Owns DispatcherTimer + tick gate (SemaphoreSlim)       │
│   • Exposes Snapshot property + formatted display          │
│     strings (CpuUsageDisplay, GpuUsageDisplay, etc.)       │
│   • Testable with mock IHardwareService                    │
└─────────────────────────┬───────────────────────────────────┘
                          │ IHardwareService
┌─────────────────────────▼───────────────────────────────────┐
│ Service Layer  —  Services/                                │
│   • IHardwareService / HardwareService — LHM + WMI +       │
│     PerformanceCounter, locked via _computerLock           │
│   • SettingsService — JSON settings in %APPDATA%           │
│   • Logger — background-queue file logger                  │
│   • SystemStatusEvaluator — pure function, testable        │
│   • OperatingSystemNames — pure function, testable         │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│ Data Layer  —  Models/                                     │
│   • SystemSnapshot (immutable record)                      │
│   • CpuCoreInfo, FanReading (records)                      │
│   • Crosses thread boundary safely (no shared mutation)    │
└─────────────────────────────────────────────────────────────┘
```

## Tick lifecycle

```
App.OnStartup
  └─ SplashWindow.Show()
  └─ Dispatcher.InvokeAsync(new MainWindow())
  └─ await MainWindow.InitializeAsync()
       └─ new HardwareService()
       └─ new MainViewModel(hardware)
       └─ DataContext = viewModel
       └─ InitializePanels(...)           ◀── builds TextBlocks once
       └─ viewModel.SnapshotChanged += OnSnapshotChanged
       └─ await viewModel.StartAsync()
            └─ await hardware.InitializeAsync()     [Task.Run → background thread]
                 └─ Computer.Open() + Accept(visitor)
                 └─ WMI: Win32_ComputerSystem (RAM)
                 └─ WMI: Win32_Processor (CPU name)
            └─ DispatcherTimer.Start()

  (timer tick every PollIntervalMs)
  DispatcherTimer.Tick (UI thread)
    └─ MainViewModel.OnTick
         └─ SemaphoreSlim.WaitAsync(0)     ◀── skips if previous tick is running
         └─ await hardware.CollectSnapshotAsync()
              └─ Task.Run on bg thread
              └─ lock(_computerLock)
                   └─ computer.Accept(_visitor)    ◀── ONCE per tick
                   └─ read all sensors
                   └─ build SystemSnapshot
         └─ Snapshot = snap                ◀── triggers OnPropertyChanged
         └─ SnapshotChanged?.Invoke(snap)
              └─ MainWindow.OnSnapshotChanged
                   └─ update cached TextBlocks
```

## Thread safety

**LibreHardwareMonitor is NOT thread-safe.** All access to `_computer`
(Accept, Hardware enumeration, Close) is serialized via `_computerLock` in
`HardwareService`.

**PerformanceCounter is thread-safe** per instance — no lock required.

**SystemSnapshot is immutable** (record with `init`-only properties,
`IReadOnlyDictionary`/`IReadOnlyList` for collections). Passed safely between
the bg thread and the UI thread.

**_tickGate (SemaphoreSlim)** protects against pile-up: if a tick takes longer
than the interval, the next one is skipped. Dispose waits up to 2s for the running tick
before releasing resources.

## Testability

| Layer | Testable? | Strategy |
|-------|-----------|----------|
| View (XAML + code-behind) | No (manual QA) | Sprint 6 QA matrix |
| MainViewModel | Yes | Mock `IHardwareService`, call `ApplySnapshotForTest` |
| HardwareService | Partially | Requires real hardware → integration test |
| SystemStatusEvaluator | Yes | Pure function, parametric tests |
| OperatingSystemNames | Yes | Pure function |
| SettingsService | Yes | Temp directory for `%APPDATA%` |
| Logger | Yes | Temp directory, verify file content |

Test project: `Tests/SystemFlow.Tests/SystemFlow.Tests.csproj` (xunit + FluentAssertions).

## Decision and non-decision notes

### Decisions

- **MVVM-lite, not a framework.** `ObservableObject` implements
  `INotifyPropertyChanged` directly. No Prism/MVVMLight/CommunityToolkit.MVVM —
  will not scale past one or two ViewModels, so unnecessary to pull in.
- **Snapshot pattern instead of event-per-property.** The ViewModel exposes
  a `Snapshot` property that is swapped atomically; other display properties
  trigger via `OnPropertyChanged(nameof(CpuUsageDisplay))` etc. Easier to
  keep the UI consistent than binding 20 separate events.
- **XAML binding transition mode.** The hero values are still written imperatively
  (`CpuValueText.Text = ...`) because the XAML markup has not been converted to
  `{Binding}` yet. Sprint 4 does this together with Fluent icons.

### Not decided yet

- Separate `SettingsViewModel` for the Settings dialog — introduced in Sprint 4.
- `ICommand`-based button handlers (instead of Click events) — introduced
  when there is a reason (e.g., Refresh command from Settings).
- Dependency injection container (Microsoft.Extensions.DependencyInjection) —
  not necessary for a single ViewModel. If more ViewModels are added,
  reevaluate.

## File structure

```
SystemFlow_Pro/
  App.xaml(.cs)                    App lifecycle, global exception handlers
  MainWindow.xaml(.cs)             View
  SplashWindow.xaml(.cs)           Loading splash
  app.manifest                     asInvoker, compatibility GUIDs
  SystemMonitorApp.csproj          .NET 9, nullable enable

  Models/
    SystemSnapshot.cs              record SystemSnapshot, CpuCoreInfo, FanReading

  Services/
    IHardwareService.cs            interface
    HardwareService.cs             LHM + WMI + PerfCounter implementation
    SettingsService.cs             JSON settings
    Logger.cs                      Background-queue file logger
    SystemStatusEvaluator.cs       Pure function
    // OperatingSystemNames lives in the same file as SystemStatusEvaluator

  ViewModels/
    ObservableObject.cs            INotifyPropertyChanged base
    MainViewModel.cs               Tick loop, snapshot property

  Tests/SystemFlow.Tests/
    SystemFlow.Tests.csproj
    SystemStatusEvaluatorTests.cs
    OperatingSystemNamesTests.cs
    FanReadingTests.cs
```

## Future work (backlog)

- Sprint 4: convert imperative panel rendering → XAML binding with
  `ItemsControl` + `DataTemplate` + value converters
- ViewModel for the Settings dialog
- `ICommand`-based handlers instead of `Click` events
- Observability: telemetry via Sentry or similar (Sprint 5)
- History mode: preserve the latest N snapshots for charts (v1.2)
