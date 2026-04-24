# SystemFlow Pro — Arkitekturöversikt

**Version:** Sprint 3 (post-refactor) → v1.1.0-beta.1
**Status:** Övergångsläge MVVM — servicelager + ViewModel på plats, XAML-binding
partiell (Sprint 4 slutför).

## Lager (översikt)

```
┌─────────────────────────────────────────────────────────────┐
│ View Layer  —  MainWindow.xaml + MainWindow.xaml.cs        │
│   • XAML: markup, visuella resurser, hero-kort             │
│   • Code-behind: imperativ panel-rendering från snapshots  │
│     (Sprint 4 ersätter med ItemsControl + DataTemplate)    │
└─────────────────────────┬───────────────────────────────────┘
                          │ DataContext + SnapshotChanged event
┌─────────────────────────▼───────────────────────────────────┐
│ ViewModel Layer  —  ViewModels/MainViewModel.cs            │
│   • ObservableObject (INotifyPropertyChanged-bas)          │
│   • Äger DispatcherTimer + tick-gate (SemaphoreSlim)       │
│   • Exponerar Snapshot-property + formaterade display-     │
│     strings (CpuUsageDisplay, GpuUsageDisplay, etc.)       │
│   • Testbar med mock IHardwareService                      │
└─────────────────────────┬───────────────────────────────────┘
                          │ IHardwareService
┌─────────────────────────▼───────────────────────────────────┐
│ Service Layer  —  Services/                                │
│   • IHardwareService / HardwareService — LHM + WMI +       │
│     PerformanceCounter, låst via _computerLock             │
│   • SettingsService — JSON-settings i %APPDATA%            │
│   • Logger — bakgrunds-queue file-logger                   │
│   • SystemStatusEvaluator — ren funktion, testbar          │
│   • OperatingSystemNames — ren funktion, testbar           │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│ Data Layer  —  Models/                                     │
│   • SystemSnapshot (immutable record)                      │
│   • CpuCoreInfo, FanReading (records)                      │
│   • Korsar tråd-gräns säkert (ingen delad mutation)        │
└─────────────────────────────────────────────────────────────┘
```

## Tick-livscykel

```
App.OnStartup
  └─ SplashWindow.Show()
  └─ Dispatcher.InvokeAsync(new MainWindow())
  └─ await MainWindow.InitializeAsync()
       └─ new HardwareService()
       └─ new MainViewModel(hardware)
       └─ DataContext = viewModel
       └─ InitializePanels(...)           ◀── bygger TextBlocks en gång
       └─ viewModel.SnapshotChanged += OnSnapshotChanged
       └─ await viewModel.StartAsync()
            └─ await hardware.InitializeAsync()     [Task.Run → bakgrundstråd]
                 └─ Computer.Open() + Accept(visitor)
                 └─ WMI: Win32_ComputerSystem (RAM)
                 └─ WMI: Win32_Processor (CPU-namn)
            └─ DispatcherTimer.Start()

  (timer tick var PollIntervalMs)
  DispatcherTimer.Tick (UI-tråd)
    └─ MainViewModel.OnTick
         └─ SemaphoreSlim.WaitAsync(0)     ◀── skippar om förra ticket kör
         └─ await hardware.CollectSnapshotAsync()
              └─ Task.Run på bg-tråd
              └─ lock(_computerLock)
                   └─ computer.Accept(_visitor)    ◀── EN gång per tick
                   └─ läs alla sensorer
                   └─ bygg SystemSnapshot
         └─ Snapshot = snap                ◀── triggar OnPropertyChanged
         └─ SnapshotChanged?.Invoke(snap)
              └─ MainWindow.OnSnapshotChanged
                   └─ uppdatera cachade TextBlocks
```

## Trådsäkerhet

**LibreHardwareMonitor är INTE thread-safe.** All åtkomst till `_computer`
(Accept, Hardware-enumeration, Close) serialiseras via `_computerLock` i
`HardwareService`.

**PerformanceCounter är thread-safe** per instans — inget lås behövs.

**SystemSnapshot är immutable** (record med `init`-only properties,
`IReadOnlyDictionary`/`IReadOnlyList` för collections). Skickas säkert mellan
bg-tråd och UI-tråd.

**_tickGate (SemaphoreSlim)** skyddar mot pile-up: om en tick tar längre tid
än intervall skippas nästa. Dispose väntar upp till 2s på pågående tick
innan resurser släpps.

## Testbarhet

| Lager | Testbar? | Strategi |
|-------|---------|----------|
| View (XAML + code-behind) | Nej (manual QA) | Sprint 6 QA-matris |
| MainViewModel | Ja | Mock `IHardwareService`, kör `ApplySnapshotForTest` |
| HardwareService | Delvis | Kräver riktig hårdvara → integrationstest |
| SystemStatusEvaluator | Ja | Ren funktion, parametriska tester |
| OperatingSystemNames | Ja | Ren funktion |
| SettingsService | Ja | Temp-katalog för `%APPDATA%` |
| Logger | Ja | Temp-katalog, verifiera filinnehåll |

Testprojekt: `Tests/SystemFlow.Tests/SystemFlow.Tests.csproj` (xunit + FluentAssertions).

## Besluts- och icke-besluts-noteringar

### Beslut

- **MVVM-lite, inte ett ramverk.** `ObservableObject` implementerar
  `INotifyPropertyChanged` direkt. Ingen Prism/MVVMLight/CommunityToolkit.MVVM —
  kommer inte skalas förbi en eller två ViewModels, så onödigt att dra in.
- **Snapshot-mönster istället för event-per-property.** ViewModel exponerar
  en `Snapshot`-property som byts ut atomiskt; övriga display-properties
  triggas via `OnPropertyChanged(nameof(CpuUsageDisplay))` etc. Enklare att
  hålla UI konsekvent än att binda 20 separata event.
- **Övergångsläge XAML-binding.** Hero-värdena skrivs fortfarande imperativt
  (`CpuValueText.Text = ...`) eftersom XAML-markupen inte konverterats till
  `{Binding}` än. Sprint 4 gör det tillsammans med Fluent-ikoner.

### Inte beslut ännu

- Separat `SettingsViewModel` för Settings-dialog — införs i Sprint 4.
- `ICommand`-baserade knapp-handlers (istället för Click-events) — införs
  när det finns skäl (t.ex. Refresh-command från Settings).
- Dependency injection-container (Microsoft.Extensions.DependencyInjection) —
  inte nödvändigt för en ViewModel. Om fler ViewModels tillkommer,
  omvärdera.

## Filstruktur

```
SystemFlow_Pro/
  App.xaml(.cs)                    App-lifecycle, global exception handlers
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
    // OperatingSystemNames finns i samma fil som SystemStatusEvaluator

  ViewModels/
    ObservableObject.cs            INotifyPropertyChanged-bas
    MainViewModel.cs               Tick-loop, snapshot property

  Tests/SystemFlow.Tests/
    SystemFlow.Tests.csproj
    SystemStatusEvaluatorTests.cs
    OperatingSystemNamesTests.cs
    FanReadingTests.cs
```

## Framtida arbete (backlog)

- Sprint 4: konvertera imperativ panel-rendering → XAML-binding med
  `ItemsControl` + `DataTemplate` + value converters
- ViewModel för Settings-dialog
- `ICommand`-baserade handlers istället för `Click`-event
- Observability: telemetri via Sentry eller liknande (Sprint 5)
- Historik-läge: bevara senaste N snapshots för diagram (v1.2)
