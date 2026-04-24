# SystemFlow Pro v1.0.9 — Stabilisering & säkerhet

**Release date:** TBD
**Branch:** `sprint-01-stabilisering`

Första av tre säkerhetsreleaser på väg mot v1.1.0. Fokus: eliminera aktiva
buggar, tysta catch-block, onödiga admin-krav och async-antimönster.

## Säkerhetsförbättringar

- **Admin-rättigheter krävs inte längre.** `app.manifest` ändrat från
  `requireAdministrator` → `asInvoker`. Appen fungerar för vanliga användare;
  sensorer som kräver admin (vissa MSR-läsningar) degraderar till "N/A".
  Fallback-UI visar info om admin-status.
- **Global felhantering.** `App.xaml.cs` registrerar
  `DispatcherUnhandledException`, `TaskScheduler.UnobservedTaskException` och
  `AppDomain.CurrentDomain.UnhandledException`. Kraschen loggas och ett
  användarvänligt felmeddelande visas istället för tyst crash.
- **Strukturerad loggning.** Ny `Logger`-klass skriver till
  `%APPDATA%\SystemFlow Pro\logs\app-{datum}.log` med rotation vid 5 MB
  (senaste 5 filer sparas). 13 tidigare tomma `catch {}`-block ersatta med
  `Logger.Warn(...)`-anrop som behåller felsäkra defaultvärden.
- **WMI-timeouts.** Alla `ManagementObjectSearcher`-anrop har nu 2-sekunders
  timeout via `EnumerationOptions`. Förhindrar att appen hänger på trasiga
  WMI-providers.

## Stabilitetsförbättringar

- **Deadlock-fix.** `GetTotalMemoryGB().Result` som anropades synkront på
  UI-tråden i `UpdateMemoryPanel` har tagits bort. Totalt fysiskt minne
  läses nu **en gång** i konstruktorn och cachas i `_totalMemoryGB` —
  värdet ändras aldrig under körning.
- **En hårdvaru-uppdatering per tick.** `computer.Accept(new UpdateVisitor())`
  anropades tidigare 5-7 gånger per tick (GpuUsage, AvgTemp, ThermalData,
  FanData, GpuInfo). Nu anropas Accept **en gång** först i `UpdateSystemData`
  och en delad `UpdateVisitor`-instans återanvänds. Minskar race conditions
  i LibreHardwareMonitor och elimineras per-tick allokering.
- **Stavfel fixat.** "HÖRG BELASTNING" → "HÖG BELASTNING" i systemstatus.

## Kodkvalitet

- **Modern C# aktiverad.** `<Nullable>enable</Nullable>`,
  `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`.
  `CS1998` (async utan await) behandlas nu som error.
- **async-cleanup.** Hårdvaruläsare (`GetGpuUsage`, `GetAverageTemperature`,
  `GetThermalData`, `GetGpuInfo`) är nu synkrona — de hade `async Task<T>`
  utan `await`, vilket gav CS1998-varning och körde ändå på UI-tråden.
  (Sprint 2 flyttar hela tick-arbetet till bakgrundstråd.)
- **Död kod borttagen:** `_gpuCounter`-field (aldrig tilldelad),
  `EstimateFanSpeedFromTemperature`-metod (aldrig anropad).
- **Event unsubscribe.** `_timer.Tick -= Timer_Tick` i `OnClosed` för att
  frigöra WPF-objektet vid fönster-stängning.

## UI-småfixar

- `Icon="app.ico"` lagt till på MainWindow och SplashWindow — korrekt ikon
  visas nu i taskbar och Alt+Tab.

## Filer påverkade

- `App.xaml.cs` — rewritten med global exception handling
- `MainWindow.xaml.cs` — rewritten, ~25% mindre logik kvar efter bortplock
- `MainWindow.xaml` — Icon tillagd
- `SplashWindow.xaml` — Icon tillagd
- `app.manifest` — `asInvoker`
- `SystemMonitorApp.csproj` — nullable + language-settings
- `Services/Logger.cs` — ny fil

## Kvarstår till Sprint 2

- UI-tråden blockeras fortfarande av WMI-anrop (~20-200ms per tick)
- UI-paneler rivs och byggs om varje sekund (GC-tryck)
- Timer pausar inte vid minimerat fönster
- %→RPM-heuristik är fortfarande gissning (sensor.Name-baserad)
- Splash-init sker på UI-tråden (1-3s frys)
