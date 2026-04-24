# SystemFlow Pro v1.1.0-beta.1 — Arkitektur + UI/UX modernisering

**Release date:** TBD
**Branch:** `sprint-03-arkitektur` → `sprint-04-ui-ux`
**Status:** Beta — funktionellt komplett, kvar: Sprint 5 (pipeline) + Sprint 6 (QA)

Stor release som samlar Sprint 3 och Sprint 4. Ny arkitektur (MVVM-lite + testbart
servicelager) och komplett UI-modernisering (Fluent-ikoner, Aero Snap,
accessibility, Settings/About-dialoger).

## Arkitektur (Sprint 3)

- **Ny mappstruktur:** `Models/`, `Services/`, `ViewModels/`, `Tests/`, `docs/`,
  `Views/`.
- **HardwareService** extraherad ur `MainWindow.xaml.cs`. All åtkomst till
  LibreHardwareMonitor och WMI serialiseras via `_computerLock` — LHM är inte
  thread-safe och tidigare version racade `Accept()` från flera trådar.
- **SystemSnapshot / CpuCoreInfo / FanReading** som immutable records i `Models/`.
  Säkra att passera mellan tråd-gränser.
- **MainViewModel** äger tick-loopen och exponerar view-bindable properties.
  `ObservableObject`-bas för `INotifyPropertyChanged`. ViewModel är testbar
  med mock `IHardwareService`.
- **MainWindow.xaml.cs** reducerad från 1232 → ~365 rader. Den är nu en ren
  view — hanterar bara rendering av snapshots från ViewModel.
- **SystemStatusEvaluator** + **OperatingSystemNames** extraherade som rena
  funktioner för testbarhet.
- **37 enhetstester** i `Tests/SystemFlow.Tests/` — xUnit + FluentAssertions.
  Testar status-logik, OS-namn, FanReading och SystemSnapshot-defaults.
- **docs/ARCHITECTURE.md** dokumenterar lager, trådning, livscykel och beslut.

## UI/UX (Sprint 4)

- **WindowChrome återställer Aero Snap.** Tidigare `WindowStyle="None"` +
  `AllowsTransparency="True"` bröt Snap Layouts (Win+Z), maximize-dubbelklick
  på titelbar, och Windows-native dragbeteende. Nu använder vi
  `System.Windows.Shell.WindowChrome` med `CaptionHeight="48"` och
  `ResizeBorderThickness="6"` — alla standardgester fungerar igen.
- **Fönsterdimensioner:** 1400×900 (tidigare 1800×1300), MinWidth=1100,
  MinHeight=750. Ryms nu på 1366×768 — tidigare klipptes nederdelen på 1080p.
- **Fluent Icons ersätter multicolored emoji.** `Segoe Fluent Icons, Segoe MDL2 Assets`-fallback.
  Headers och chrome använder glyph-koder (&#xE945; power, &#xE7FC; gpu,
  &#xE977; storage, &#xE9CA; temperature, &#xE713; settings, &#xE946; info,
  &#xE921;/&#xE922;/&#xE8BB; för minimize/maximize/close). Panel-rendering
  i code-behind har emoji-prefix borttagna — status markeras via färgkodning
  i stället, matchar Task Managers professionella stil.
- **Accessibility — AutomationProperties + TabIndex + FocusVisual.**
  Alla interaktiva kontroller har `AutomationProperties.Name` +
  `AutomationProperties.HelpText`. TabIndex 101-105 på chrome-knapparna för
  logisk tangentbordsordning. Hero-kort har
  `AutomationProperties.LiveSetting="Polite"` så skärmläsare meddelar
  uppdateringar utan att avbryta användaren. FocusVisualStyle med synlig
  outline på fokus.
- **WCAG-kontrast höjd.** `TextMutedBrush` från `#94A3B8` (4.9:1) →
  `#A8B2C0` (5.5:1) på dark bakgrund — godkänd AA över hela appen.
- **Version v1.1.0 synlig** i header (tidigare v1.0.8 hårdkodat).
- **Settings-dialog** (`Views/SettingsWindow.xaml`). Konfigurerbar
  pollingintervall (500ms / 1s / 2s / 5s), temperaturenhet (°C/°F), pausa
  vid minimering, starta minimerad. Sparas till
  `%APPDATA%\SystemFlow Pro\settings.json`. Polling-intervall appliceras
  live utan omstart via `MainViewModel.ApplyUpdatedSettings()`.
- **About-dialog** (`Views/AboutWindow.xaml`). Version + byggdatum från
  Assembly, länk till GitHub-repo och issues, tredjepart-attribution
  (LibreHardwareMonitor MPL 2.0, .NET 9 MIT, Segoe Fluent Icons),
  hyperlinkar öppnas i standardbrowsern.
- **Tooltips på hero-kort** förklarar metriken och varningsgräns
  (t.ex. "CPU total belastning. Varning vid 80%.").

## Filer påverkade

### Nya filer
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

### Ändrade filer
- `MainWindow.xaml` — helt rewritten, WindowChrome + Fluent Icons + accessibility
- `MainWindow.xaml.cs` — tunnt view-lager, delegerar till MainViewModel
- `SystemFlow_Pro.sln` — testprojekt tillagt
- `SystemMonitorApp.csproj` — version 1.1.0-beta.1

## Kvarstår till Sprint 5

- Self-contained single-file publish (README ljuger fortfarande)
- GitHub Actions CI/CD
- Auto-update via GitHub Releases
- THIRD_PARTY_LICENSES.txt
- Git-historik städad (releases/ committat)

## Kvarstår till Sprint 6

- Manuell QA på flera hårdvarukonfigurationer
- Beta med externa testare
- Fixa regressioner
- Publicera v1.1.0
