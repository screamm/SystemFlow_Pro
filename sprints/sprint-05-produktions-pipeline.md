# Sprint 5 — Produktions-pipeline

**Mål:** Self-contained single-file build. GitHub Actions CI/CD. Automatisk crash-rapportering. Auto-update-mekanism. Tredjeparts-licenser korrekt dokumenterade. Repo-hygien.

**Varaktighet:** 1-2 veckor (~40-50h)
**Branch:** `sprint-05-pipeline`
**Målversion:** v1.1.0-rc.1
**Förutsättningar:** Sprint 4 klar, all kod funktionellt komplett

**Utgångsläge:** Koden är redo men distributionen är amatörmässig — framework-dependent, osignerad, sex rediga build-scripts, releases/ committat i git.

---

## Sprintmål

- [ ] Ett enda `build.bat [version]` som producerar self-contained single-file exe
- [ ] `.github/workflows/release.yml` triggar på tag push och skapar GitHub Release
- [ ] Crash-reporter skriver till fil + skickar valfritt (opt-in) till insamlingstjänst
- [ ] Auto-update check vid appstart mot GitHub Releases API
- [ ] `THIRD_PARTY_LICENSES.txt` i distribution
- [ ] README uppdaterad, FannyKnob-referenser borttagna, stämmer med faktisk build
- [ ] `releases/` borttagen från git-historik (via `git filter-repo` eller BFG)
- [ ] `scripts/claude_autonomous_loop.py` flyttad eller gitignored
- [ ] SmartScreen-hantering dokumenterad för slutanvändare (osignerad distribution)

---

## Tasks

### T5.1 [P0] Self-contained single-file publish
**Var:** `SystemMonitorApp.csproj` + nytt `build.bat`
**Varför:** Nuvarande release kräver .NET 9 Runtime installerat. README ljuger.
**Åtgärd:**
Lägg till i csproj:
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <PublishReadyToRun>true</PublishReadyToRun>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <DebugType>embedded</DebugType>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```
Publish-kommando:
```
dotnet publish -c Release -r win-x64 --self-contained true -o publish\
```
Förväntat resultat: ~70-90 MB single-file .exe utan externa DLL:er.
**DoD:** Flytta .exe:n till en clean Windows-maskin utan .NET 9 installerat → startar.
**Estimat:** 3h

### T5.2 [P0] Konsolidera build-scripts
**Var:** Ta bort `build.bat`, `build_release_v1.0.2.bat` till `build_release_v1.0.8.bat`. Skapa nytt `build.bat`.
**Varför:** Sex scripts skapar förvirring.
**Åtgärd:** Nytt `build.bat`:
```batch
@echo off
setlocal
if "%~1"=="" (echo Usage: build.bat [version] & exit /b 1)
set VERSION=%~1

echo Building SystemFlow Pro v%VERSION%...
dotnet clean -c Release
dotnet publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained true ^
    -p:Version=%VERSION% -p:AssemblyVersion=%VERSION% -p:FileVersion=%VERSION% ^
    -o publish\v%VERSION%

echo Creating zip...
powershell Compress-Archive -Path publish\v%VERSION%\* -DestinationPath releases\SystemFlow-Pro-v%VERSION%-win-x64.zip -Force

echo Copying license info...
copy LICENSE publish\v%VERSION%\LICENSE.txt
copy THIRD_PARTY_LICENSES.txt publish\v%VERSION%\

echo Done: publish\v%VERSION%\SystemFlow-Pro.exe
```
**DoD:** `build.bat 1.1.0` producerar exe + zip.
**Estimat:** 2h

### T5.3 [AVFÖRT] Code signing
**Status:** Ej planerat. SystemFlow Pro distribueras osignerat som öppen källkod.

**Motivering:** Code signing-certifikat kostar 200-500 USD/år (OV eller EV) och
projektet är gratis. Användare kan granska all kod på GitHub istället.
SmartScreen-varningen hanteras via "Mer info" → "Kör ändå" och minskar
gradvis när fler användare nedladdar och verifierar appen.

**Dokumentation:** README och `docs/FAQ.md` förklarar SmartScreen-beteendet
för slutanvändare. Release-workflow producerar osignerade artefakter.

### T5.4 [P0] `DispatcherUnhandledException` (om inte klar från Sprint 1) + crash-reporter
**Var:** `App.xaml.cs` + ny `Services/CrashReporter.cs`
**Åtgärd:** Bygg vidare på Logger från Sprint 1. Lägg till:
- Vid unhandled exception: skriv detaljerad crash-dump till `%APPDATA%\SystemFlow Pro\crashes\crash-{timestamp}.txt`
- Inkludera: stack trace, OS-version, .NET-version, hårdvarubasinfo, settings-dump
- Visa dialog: "Appen kraschade. Rapporten har sparats. Vill du skicka den till utvecklaren? [Ja/Nej]"
- Vid "Ja": antingen email-link (`mailto:` med filen som bifogad är inte möjligt, använd clipboard-kopia + öppen GitHub Issues-URL) ELLER skicka till en enkel endpoint

Enklaste fullständig lösning: Sentry.io — gratis-tier räcker för soloprojekt:
```xml
<PackageReference Include="Sentry" Version="5.0.*" />
```
```csharp
SentrySdk.Init(o => {
    o.Dsn = "https://...@sentry.io/...";
    o.Release = $"systemflow-pro@{version}";
    o.SendDefaultPii = false; // viktigt för GDPR
});
```
Opt-in via settings: `SendDiagnosticsToDeveloper = false` default.
**DoD:** Testvis kasta en exception → crashdump skrivs. Om Sentry används → syns i dashboard.
**Estimat:** 4h

### T5.5 [P0] Auto-update-check
**Var:** Ny fil `Services/UpdateChecker.cs`
**Varför:** Utan auto-update hamnar användare på gamla versioner. Nya versioner med viktiga fixar når inte fram.
**Åtgärd:** Enkel version: vid appstart, efter 5s fördröjning:
```csharp
public async Task<UpdateInfo?> CheckForUpdatesAsync()
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("SystemFlow-Pro");
    var json = await http.GetStringAsync(
        "https://api.github.com/repos/screamm/SystemFlow_Pro/releases/latest");
    var release = JsonSerializer.Deserialize<GitHubRelease>(json);
    var latestVersion = Version.Parse(release.TagName.TrimStart('v'));
    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;

    if (latestVersion > currentVersion)
        return new UpdateInfo(latestVersion, release.HtmlUrl, release.Body);
    return null;
}
```
Vid tillgänglig uppdatering: icke-blockerande toast/banner i header "Ny version 1.1.1 tillgänglig [Hämta]". Klick öppnar GitHub Release-sidan i browser.

Mer avancerat (valfritt): integrera Squirrel.Windows för helt automatisk nedladdning + patching.
**DoD:** Kan testa med hårdkodad lägre version → banner visas. Verkliga versionsjämförelser fungerar.
**Estimat:** 4h

### T5.6 [P0] GitHub Actions CI/CD
**Var:** Nya filer `.github/workflows/ci.yml` + `.github/workflows/release.yml`
**Åtgärd:**

`ci.yml` (kör vid varje push/PR):
```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 9.0.x }
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build --logger "trx;LogFileName=test-results.trx"
      - uses: actions/upload-artifact@v4
        if: always()
        with: { name: test-results, path: '**/test-results.trx' }
```

`release.yml` (triggar på tag `v*`):
```yaml
name: Release
on:
  push:
    tags: ['v*']
jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 9.0.x }
      - name: Get version from tag
        id: ver
        run: echo "version=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
        shell: bash
      - run: dotnet publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained true
               -p:Version=${{ steps.ver.outputs.version }} -o publish
      - run: Compress-Archive publish\* SystemFlow-Pro-v${{ steps.ver.outputs.version }}.zip
      - uses: softprops/action-gh-release@v2
        with:
          files: SystemFlow-Pro-v*.zip
          generate_release_notes: true
```
**DoD:** Push tag `v1.0.11` (test) → GitHub Actions kör, artefakt + release skapas automatiskt.
**Estimat:** 6h

### T5.7 [P1] THIRD_PARTY_LICENSES.txt
**Var:** Ny fil `THIRD_PARTY_LICENSES.txt`
**Åtgärd:** Inkludera:
- LibreHardwareMonitorLib 0.9.4 — MPL 2.0 (full licenstext + källkodsreferens)
- System.Management — MIT (Microsoft)
- .NET 9 Runtime (om self-contained distributeras) — MIT
- Segoe Fluent Icons — ingår i Windows, ingen särskild notis men nämn
Automatisera via `dotnet-project-licenses` eller manuellt.
**DoD:** Filen inkluderas i build-outputs (se T5.2).
**Estimat:** 2h

### T5.8 [P1] README-uppdatering
**Var:** `README.md`
**Åtgärd:**
- Ta bort "FannyKnob"-referenser
- Ta bort `publish-true-single\`-referens
- Uppdatera installations-instruktion till faktisk path
- Markera självcontained-krav korrekt ("ingen .NET-installation krävs" — nu sant)
- Lägg till screenshot från Sprint 4
- Lägg till sektion "Feedback & buggar" → GitHub Issues-länk
- Versionsnummer i badge: `v1.1.0`
**DoD:** README stämmer med faktisk distribution.
**Estimat:** 2h

### T5.9 [P1] Ta bort `releases/` från git-historik
**Var:** Hela git-historiken
**Varför:** 50-138 MB per version × 8 versioner = ~800 MB i historik. Saktar ner kloning.
**Åtgärd:**
```bash
# Installera git-filter-repo först
pip install git-filter-repo
# Eller använd BFG: https://rtyley.github.io/bfg-repo-cleaner/

git clone --mirror https://github.com/screamm/SystemFlow_Pro systemflow.git
cd systemflow.git
git filter-repo --path releases --invert-paths
git push --force --all
git push --force --tags
```
**VARNING:** Detta skriver om historiken. Koordinera med ev. andra klones/forks.
**DoD:** `git clone` är nu <20 MB. Historiken innehåller ingen `releases/`-mapp.
**Estimat:** 2h

### T5.10 [P1] `.gitignore` uppdatering
**Var:** `.gitignore`
**Åtgärd:**
- Kontrollera att `[Rr]eleases/` fungerar (kan vara att filer committats före pattern-tillägg)
- Lägg till `publish/`, `*.pfx`, `settings.local.json`
- Lägg till `scripts/` (claude_autonomous_loop hör inte hit) eller flytta filerna till `.dev/` och gitignorea det
**DoD:** `git status` efter `dotnet publish` visar inga nya spårade filer.
**Estimat:** 0.5h

### T5.11 [P2] Privacy/datainsamling-policy
**Var:** Ny fil `PRIVACY.md`
**Varför:** Appen samlar hårdvaruinfo + username. Om Sentry eller annan telemetri aktiveras måste det dokumenteras.
**Åtgärd:** Kort dokument:
- Vilken data appen läser lokalt (CPU-namn, GPU-namn, sensorer, OS-version, username)
- Vilken data som lämnar maskinen (ingen per default — om Sentry aktiveras: stack traces + OS-version)
- Om användaren aktiverar crash reports: vad skickas
- GDPR-relevanta punkter
**DoD:** Länkad från About-dialog och README.
**Estimat:** 1h

### T5.12 [P2] Changelog-komplement
**Var:** Skapa `CHANGELOG_v1.0.6.md`, `CHANGELOG_v1.0.7.md`, `CHANGELOG_v1.0.8.md`, `CHANGELOG_v1.1.0.md`
**Åtgärd:** Rekonstruera från git-log vad som ändrats i de saknade versionerna. För v1.1.0: sammanfatta alla Sprint 1-5 ändringar.
**DoD:** Komplett changelog-kedja från v1.0.2 till v1.1.0.
**Estimat:** 2h

### T5.13 [P2] SmartScreen-varning-dokumentation
**Var:** README + FAQ
**Åtgärd:** Förklara att appen är osignerad öppen källkod, att SmartScreen varnar av den anledningen, och att användaren kan klicka "Mer info → Kör ändå" efter att ha granskat koden på GitHub om önskat.
**DoD:** Trouble-shooting-sektion i README.
**Estimat:** 0.5h

### T5.14 [P3] `LICENSE`-fil i root
**Var:** Ny fil `LICENSE`
**Åtgärd:** Bestäm licens (MIT rekommenderat). Kopiera standardtext. Uppdatera README.
**DoD:** Filen existerar, README refererar korrekt licens.
**Estimat:** 0.5h

---

## Risk & beroenden

- **T5.3 (Code signing)** avfört — inga externa beroenden på leveranstid.
- **T5.9 (git history rewrite)** är destruktiv — ta komplett backup av repot innan.
- **T5.6 (CI/CD)** kan avslöja build-problem som bara syntes lokalt (path-skillnader, encoding, etc). Räkna med debugging.
- Sentry.io kräver gratis-konto — setup tar en timme.

---

## Retro (fyll i efter sprint)
