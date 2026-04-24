# SystemFlow Pro v1.1.0 — Release Notes (template)

**Release date:** TBD (efter Sprint 6 godkänd QA)
**Download:** https://github.com/screamm/SystemFlow_Pro/releases/tag/v1.1.0

Första produktionsmognad sedan v1.0.x-serien. Komplett omskrivning av
hårdvarulagret, ny UI-stack, ~3x bättre prestanda, tillgänglig för
skärmläsare, portabel öppen-källkod-distribution.

---

## Ladda ner

**Rekommenderat:** `SystemFlow-Pro-v1.1.0-win-x64.zip` (~80 MB)

Packa upp, kör `SystemFlow-Pro.exe`. Ingen .NET installation behövs.
Ingen admin krävs.

## Höjdpunkter

### Prestanda

- **~3x snabbare tick-cykel.** UI-tråden frikopplad från hårdvaruläsning.
  Hela snapshot-hämtningen (LibreHardwareMonitor + WMI + Performance Counters)
  körs nu på en bakgrundstråd via en semaphore-gated pipeline.
- **Inget mer flimmer.** UI-paneler byggs **en gång** vid start; snapshots
  uppdaterar bara text/färg. Tidigare revs hela träd varje sekund.
- **~3% kontinuerlig CPU-overhead** (tidigare 8-15%) på moderna maskiner.
- **Paus vid minimerad.** När appen minimeras stoppas polling helt. CPU
  faller till ~0%. Återaktivering vid restore.

### Stabilitet

- **Inga fler tysta krascher.** Global `DispatcherUnhandledException`-handler
  fångar alla oupptagna exceptions, loggar stack trace till
  `%APPDATA%\SystemFlow Pro\logs\` och visar vänligt felmeddelande.
- **Race-condition-fix.** LibreHardwareMonitor är inte trådsäker — all
  åtkomst serialiseras nu via `_computerLock`. Tidigare versioner racade
  `Accept()`-anrop från flera trådar, vilket kunde ge korrupta sensor-värden.
- **Tick-pile-up skyddad.** Om en tick tar längre tid än intervall skippas
  nästa istället för att köa upp.
- **Dispose-race skyddad.** Stängning väntar på pågående tick (upp till 2s)
  innan resurser släpps.

### UI / UX

- **Aero Snap fungerar igen.** WindowChrome-API ersatte den tidigare
  `AllowsTransparency="True"`-implementation som bröt Snap Layouts,
  maximize-dubbelklick och Windows-native dragbeteende.
- **Fluent Icons.** Alla multicolored emoji-ikoner ersatta med
  Segoe Fluent Icons (med MDL2 Assets-fallback för Windows 10). Mer
  professionellt, konsekvent per Windows-version, skalar korrekt i DPI.
- **Settings-dialog.** Konfigurerbar pollingintervall (500ms-5s),
  temperaturenhet (°C/°F), pausa-vid-minimering, starta-minimerad.
  Ändringar sparas i `%APPDATA%\SystemFlow Pro\settings.json`.
- **About-dialog.** Version, byggdatum, länk till GitHub-repo och issues,
  tredjepart-attribution.
- **Accessibility.** `AutomationProperties.Name` + `AutomationProperties.HelpText`
  på alla interaktiva kontroller. Narrator-stöd. TabIndex för logisk
  tangentbordsnavigation. Synlig fokus-outline.
- **WCAG-kontrast höjd** från 4.9:1 till 5.5:1 på muted-text (godkänd AA).
- **Mindre fönsterdimensioner.** 1400×900 default (MinWidth 1100,
  MinHeight 750) — ryms nu på 1366×768.

### Arkitektur (under huven)

- **Extraherad HardwareService** — hårdvaruläsning isolerad från View-lager.
- **MVVM-lite** med MainViewModel som orkestrerar tick-loop och exponerar
  snapshot-property via `INotifyPropertyChanged`.
- **Immutable records** (`SystemSnapshot`, `CpuCoreInfo`, `FanReading`) för
  trådgräns-säker data-överföring.
- **37 enhetstester** (xUnit + FluentAssertions) i `Tests/SystemFlow.Tests/`.

### Infrastruktur

- **Self-contained single-file distribution.** Ingen .NET-runtime behövs
  hos slutanvändare.
- **GitHub Actions CI/CD.** Automatisk build + test vid varje push.
  Release-workflow bygger och publicerar zip vid tag-push.
- **Auto-update-check** mot GitHub Releases API vid appstart (icke-blockerande).
- **LICENSE, PRIVACY, THIRD_PARTY_LICENSES** komplett dokumenterade.

### Administratör inte längre nödvändig

Tidigare krävde `app.manifest` `requireAdministrator` även för vanliga
användare — bröt för icke-admin-konton. Nu körs appen som `asInvoker`
med graceful degradation för sensorer som kräver admin.

## Ändringar som kan påverka dig

- **Fläktvärden mer korrekta.** Tidigare multiplicerade `value * 30f` för
  alla värden ≤100, vilket gav falska RPM-värden för zero-RPM GPU:er och
  pumpsensorer. Nu visas `SensorType.Fan` som RPM och `SensorType.Control`
  som %. Om du har sparat skärmdumpar för jämförelse kan värden se
  annorlunda ut — de nya är korrekta.
- **Ikoner ändrade.** Multicolored emoji (🔥 🎮 💾 🌡️) ersatta med Fluent
  Icons. Status markeras nu via färgkodning istället för emoji i text.
- **Settings flyttat.** Ingen tidigare settings-plats — nu
  `%APPDATA%\SystemFlow Pro\settings.json`. Första körning skapar defaults.
- **Loggar.** Nya loggar i `%APPDATA%\SystemFlow Pro\logs\` med 5 MB
  rotation, senaste 5 filer.

## Migrering från v1.0.x

- Ingen migrering av data behövs — appen läser bara sensorer live
- Gamla version kan avinstalleras genom att radera mappen
- Nya versionen är portabel — packa upp .zip vart du vill

## Kända begränsningar

- **°F-konvertering**: Settings har °C/°F-valet men konvertering i UI
  slutförs i v1.1.1.
- **Mica-backdrop (Windows 11)**: Fluent-acrylic bakgrund implementeras i
  v1.2.
- **Auto-update nedladdning**: v1.1.0 visar bara "ny version tillgänglig"
  med länk. Automatisk nedladdning via Squirrel/Velopack kommer i v1.2.
- **Språk**: Endast svenska UI. Engelska kommer i v1.2.

## Bidragande

- David Rydgren — utveckling
- *(Beta-testare — fyll i efter Sprint 6)*

Tack till alla som rapporterar buggar och föreslår förbättringar på GitHub.

## Nästa version

v1.1.1 (bugfix release) eller v1.2.0 (features) — se
[Issues](https://github.com/screamm/SystemFlow_Pro/issues) för backlog.
