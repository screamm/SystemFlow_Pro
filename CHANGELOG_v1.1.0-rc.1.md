# SystemFlow Pro v1.1.0-rc.1 — Produktions-pipeline

**Release date:** TBD
**Branch:** `sprint-05-pipeline`

Release candidate. Funktionellt låst — endast produktions-infrastruktur
tillkommer från denna version. Sprint 6 är rent QA/beta/release.

## Distribution

- **Self-contained single-file publish.** `SystemMonitorApp.csproj`
  konfigurerad med `PublishSingleFile=true`, `SelfContained=true`,
  `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true`,
  `PublishReadyToRun=true`. Resultat: en enda `SystemFlow-Pro.exe` ~70-90 MB
  som körs utan förinstallerad .NET-runtime.
- **README korrigerad.** "EN ENDA FIL — ingen .NET installation krävs" är
  nu sant (tidigare ljög README). Referenser till det obefintliga projektet
  "FannyKnob" och den borttagna `publish-true-single/`-katalogen är
  ersatta med korrekta instruktioner.

## Byggsystem

- **Enad `build.bat`** ersätter `build_release_v1.0.2.bat` → `v1.0.8.bat`.
  Argument: `build.bat [version] [--no-compress]`. Kör clean,
  publish self-contained, kopiera licensfiler och zip-komprimering.
- **Tidigare build_release_*.bat** behålls för backåt-referens men är
  obsoleta. Kan tas bort i framtida cleanup-pass.

## CI/CD

- **`.github/workflows/ci.yml`** — kör vid varje push/PR. Restore →
  build (warn-as-error på CS1998) → test (xUnit, 37 tests) → smoke-publish
  → artefakter laddas upp. Windows-latest runner, `global.json` pinnar
  SDK 9.0.305.
- **`.github/workflows/release.yml`** — triggar på tag `v*`. Bygger
  self-contained single-file, skapar zip, publicerar GitHub Release
  med auto-genererade release notes. Prerelease-flaggan sätts automatiskt
  om tag innehåller `-` (t.ex. `v1.1.0-rc.1`). Distributionen är osignerad
  (öppen källkod, se SmartScreen-hantering i README och FAQ).

## Säkerhet

- **Auto-update-check** mot GitHub Releases API via
  `Services/UpdateChecker.cs`. HTTP GET (User-Agent: "SystemFlow-Pro",
  5 sekunders timeout) 8 sekunder efter appstart. Vid tillgänglig
  uppdatering visas MessageBox med "Öppna release-sida?"-fråga. Alla
  fel är tysta — offline / rate-limited bryter aldrig appen.
- **`THIRD_PARTY_LICENSES.txt`** — komplett attribution för
  LibreHardwareMonitor (MPL 2.0), .NET 9 runtime (MIT), System.Management
  (MIT), Segoe-fonter (Microsoft proprietary, ingår i Windows).
  Kopieras automatiskt till release-mappen av `build.bat` och `release.yml`.
- **`LICENSE`** — MIT-licens på appkoden, copyright David Rydgren
  2025-2026.
- **`PRIVACY.md`** — GDPR-kompatibel policy. Dokumenterar att ingen data
  skickas extern, listar lokalt lagrade filer (`settings.json`,
  loggfiler), beskriver nätverksanropet för update-check och vad
  användaren kan göra för att avstänga det.

## Diagnostik

- `DispatcherUnhandledException` + `TaskScheduler.UnobservedTaskException`
  + `AppDomain.CurrentDomain.UnhandledException` registreras (redan gjort
  i Sprint 1 — verifierat att crash-rapport skrivs till
  `%APPDATA%\SystemFlow Pro\logs\`).

## Hygien

- `.gitignore` uppdaterad: ignorerar `publish/`, `*.pfx`, `*.p12`, `*.snk`,
  `cert.*`, `settings.local.json`, `scripts/` (claude-utvecklarscript hör
  inte hemma i produktionsrepot).

## Distribution — osignerat, öppen källkod

Code-signing är inte planerat. SystemFlow Pro distribueras osignerat;
användare hanterar SmartScreen-varningen via "Mer info" → "Kör ändå".
Motivering: certifikat kostar 200-500 USD/år och projektet är gratis och
öppen källkod — all kod kan granskas på GitHub istället. README och FAQ
dokumenterar detta för slutanvändare.

## Ej genomfört (kräver manuell åtgärd)

- **Git history-rewrite** av `releases/` (50-138 MB per version i
  historiken). Destruktiv operation — kräver användarbekräftelse och bör
  göras utanför denna session. Kommando: `git filter-repo --path releases --invert-paths`
  följt av `git push --force`.
- **Changelogs för v1.0.6/v1.0.7/v1.0.8** — git-historiken täcker dessa
  ändringar; manuell rekonstruktion har inte gjorts.

## Filer påverkade

### Nya filer
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `build.bat` (ersatt)
- `LICENSE`
- `PRIVACY.md`
- `THIRD_PARTY_LICENSES.txt`
- `Services/UpdateChecker.cs`

### Ändrade filer
- `App.xaml.cs` — `CheckForUpdatesInBackground` lagd till
- `README.md` — helt omskriven, faktabaserad
- `.gitignore` — utökad med cert/pfx/dev-paths
- `SystemMonitorApp.csproj` — self-contained publish config, metadata,
  version 1.1.0-rc.1

## Redo för Sprint 6

- [x] Self-contained exe byggbar via `build.bat 1.1.0`
- [x] CI-workflow validerar build + tester vid varje commit
- [x] Release-workflow skapar GitHub Release vid tag-push
- [x] Juridik klar: LICENSE + THIRD_PARTY_LICENSES + PRIVACY
- [x] Auto-update-mekanism lever
- [x] Distribution: osignerat + dokumenterat (ej blockerare)
