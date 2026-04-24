# SystemFlow Pro v1.0.10 — Prestanda & resurshantering

**Release date:** TBD
**Branch:** `sprint-02-prestanda`

Andra säkerhetsreleasen på vägen mot v1.1.0. Fokus: frikoppla UI-tråden från
hårdvaruläsning, eliminera flimmer, stoppa minnesläckor, och introducera
användarkonfigurerbar pollingfrekvens.

## Prestanda

- **Ny tråd-pipeline.** `Timer_Tick` är nu `async void` med `SemaphoreSlim`-gate.
  Hårdvaruläsning (`CollectSnapshot`) körs på bakgrundstråd via `Task.Run`;
  UI-tråden tar emot en färdig `SystemSnapshot` och uppdaterar cachade
  TextBlocks (`ApplySnapshotToUI`). Om en tick överlappar föregående skippas
  den istället för att köa — ingen pile-up på långsamma system.
- **Panel-cachning.** Alla paneler bygger TextBlocks **en gång** i
  `InitializePanels` och uppdaterar därefter bara `.Text` och `.Foreground`.
  Tidigare `Panel.Children.Clear() + Add()` var dominerande GC-källa.
  Dynamiska paneler (thermal, fans) använder dictionary-cache och lägger till
  eller tar bort endast när sensoruppsättningen ändras.
- **Timer pausar vid minimerat fönster.** `StateChanged`-handler stoppar
  pollingen när `WindowState == Minimized` (konfigurerbart via
  `settings.json → PauseWhenMinimized`). CPU-användning faller till ~0% i
  minimerad läget.
- **Konfigurerbar pollingintervall.** Läggs i `%APPDATA%\SystemFlow Pro\settings.json`
  (`PollIntervalMs`, default 2000). Begränsas till 500-60000ms.
  Settings-UI kommer i Sprint 4.

## Stabilitet

- **Splash-init decoupled.** `MainWindow`-konstruktorn är nu minimal —
  hårdvaru-init (LibreHardwareMonitor open + Accept + WMI memory read) körs
  i `InitializeAsync()` på bakgrundstråd. Splashen fryser inte längre 1-3s
  medan hårdvaran enumererar. `Task.Delay(2000)`-hack ersatt av adaptiv
  minimum-visningstid (800ms).
- **Fläktheuristik fixad.** `sensor.SensorType == Fan` → RPM utan konvertering.
  `sensor.SensorType == Control` → procent (PWM). Den tidigare godtyckliga
  `value * 30f`-multiplikationen som gjorde att GPU:er i zero-RPM-läge visade
  fel värden är borttagen. Procent-fläktar visas nu som "%", RPM-fläktar som
  "RPM" — inte falska omräkningar.
- **Event unsubscribes.** `OnClosed` avregistrerar `Timer.Tick`, `StateChanged`,
  `Loaded`. Frigör `MainWindow`-referensen fullt så GC kan samla objektet.
  `_tickGate.Dispose()` läggs till.

## Kodstruktur (förberedelse för Sprint 3)

- Internal `SystemSnapshot`-klass + `FanReading`-record-struct används för att
  passera data från bakgrundstråden till UI. Sprint 3 flyttar dessa till
  `Models/` som `public sealed record`.
- `ReadGpuUsage`, `ReadAverageTemperature`, `ReadThermalData`, `ReadFanData`
  är nu synkrona — de körs från `Task.Run` i `CollectSnapshot`.
- Ny `Services/SettingsService.cs` med JSON-serialisering och clamping.

## Förväntat resultat

- Tick-kostnad: ~300ms → ~30-50ms (10x snabbare)
- UI-frysning per tick: eliminerad
- GC Gen0/min under polling: ~20 → ~2
- Kontinuerlig CPU-overhead: 8-15% → <3% på referensmaskin
- Minimerad: ~0% CPU (timer stoppad)

## Kvarstår till Sprint 3

- Fortfarande god-class, ingen MVVM eller binding
- Inga enhetstester
- Hårdvarulogik inte extraherad till service

## Filer påverkade

- `MainWindow.xaml.cs` — större rewrite, snapshot-mönster
- `App.xaml.cs` — anropar `InitializeAsync`, adaptiv splash-timing
- `SystemMonitorApp.csproj` — version bump
- `Services/SettingsService.cs` — ny fil
