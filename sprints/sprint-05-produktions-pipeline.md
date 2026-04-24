# Sprint 5 — Production pipeline

**Goal:** Self-contained single-file build. GitHub Actions CI/CD. Automatic crash reporting. Auto-update mechanism. Third-party licenses correctly documented. Repo hygiene.

**Duration:** 1-2 weeks (~40-50h)
**Branch:** `sprint-05-pipeline`
**Target version:** v1.1.0-rc.1
**Prerequisites:** Sprint 4 complete, all code functionally complete

**Starting point:** The code is ready but distribution is amateurish — framework-dependent, unsigned, six messy build scripts, releases/ committed in git.

---

## Sprint goal

- [ ] A single `build.bat [version]` that produces a self-contained single-file exe
- [ ] `.github/workflows/release.yml` triggers on tag push and creates a GitHub Release
- [ ] Crash reporter writes to file + optionally (opt-in) sends to a collection service
- [ ] Auto-update check at app start against the GitHub Releases API
- [ ] `THIRD_PARTY_LICENSES.txt` in the distribution
- [ ] README updated, FannyKnob references removed, matches the actual build
- [ ] `releases/` removed from git history (via `git filter-repo` or BFG)
- [ ] `scripts/claude_autonomous_loop.py` moved or gitignored
- [ ] SmartScreen handling documented for end users (unsigned distribution)

---

## Tasks

### T5.1 [P0] Self-contained single-file publish
**Where:** `SystemMonitorApp.csproj` + new `build.bat`
**Why:** The current release requires .NET 9 Runtime to be installed. README lies.
**Action:**
Add to csproj:
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
Publish command:
```
dotnet publish -c Release -r win-x64 --self-contained true -o publish\
```
Expected result: ~70-90 MB single-file .exe with no external DLLs.
**DoD:** Move the .exe to a clean Windows machine without .NET 9 installed → starts.
**Estimate:** 3h

### T5.2 [P0] Consolidate build scripts
**Where:** Remove `build.bat`, `build_release_v1.0.2.bat` through `build_release_v1.0.8.bat`. Create a new `build.bat`.
**Why:** Six scripts create confusion.
**Action:** New `build.bat`:
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
**DoD:** `build.bat 1.1.0` produces exe + zip.
**Estimate:** 2h

### T5.3 [DECLINED] Code signing
**Status:** Not planned. SystemFlow Pro is distributed unsigned as open source.

**Rationale:** Code signing certificates cost 200-500 USD/year (OV or EV) and
the project is free. Users can review all code on GitHub instead.
The SmartScreen warning is handled via "More info" → "Run anyway" and decreases
gradually as more users download and verify the app.

**Documentation:** README and `docs/FAQ.md` explain the SmartScreen behavior
for end users. The release workflow produces unsigned artifacts.

### T5.4 [P0] `DispatcherUnhandledException` (if not complete from Sprint 1) + crash reporter
**Where:** `App.xaml.cs` + new `Services/CrashReporter.cs`
**Action:** Build on the Logger from Sprint 1. Add:
- On unhandled exception: write a detailed crash dump to `%APPDATA%\SystemFlow Pro\crashes\crash-{timestamp}.txt`
- Include: stack trace, OS version, .NET version, hardware basic info, settings dump
- Show dialog: "The app crashed. The report has been saved. Do you want to send it to the developer? [Yes/No]"
- On "Yes": either email link (`mailto:` with the file attached is not possible, use clipboard copy + open GitHub Issues URL) OR send to a simple endpoint

Easiest complete solution: Sentry.io — the free tier is sufficient for a solo project:
```xml
<PackageReference Include="Sentry" Version="5.0.*" />
```
```csharp
SentrySdk.Init(o => {
    o.Dsn = "https://...@sentry.io/...";
    o.Release = $"systemflow-pro@{version}";
    o.SendDefaultPii = false; // important for GDPR
});
```
Opt-in via settings: `SendDiagnosticsToDeveloper = false` default.
**DoD:** Throw a test exception → crash dump is written. If Sentry is used → appears in the dashboard.
**Estimate:** 4h

### T5.5 [P0] Auto-update check
**Where:** New file `Services/UpdateChecker.cs`
**Why:** Without auto-update, users end up on old versions. New versions with important fixes do not reach them.
**Action:** Simple version: at app start, after 5s delay:
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
When an update is available: non-blocking toast/banner in the header "New version 1.1.1 available [Download]". Click opens the GitHub Release page in the browser.

More advanced (optional): integrate Squirrel.Windows for fully automatic download + patching.
**DoD:** Can be tested with a hardcoded lower version → banner is shown. Actual version comparisons work.
**Estimate:** 4h

### T5.6 [P0] GitHub Actions CI/CD
**Where:** New files `.github/workflows/ci.yml` + `.github/workflows/release.yml`
**Action:**

`ci.yml` (runs on every push/PR):
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

`release.yml` (triggers on tag `v*`):
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
**DoD:** Push tag `v1.0.11` (test) → GitHub Actions runs, artifact + release created automatically.
**Estimate:** 6h

### T5.7 [P1] THIRD_PARTY_LICENSES.txt
**Where:** New file `THIRD_PARTY_LICENSES.txt`
**Action:** Include:
- LibreHardwareMonitorLib 0.9.4 — MPL 2.0 (full license text + source code reference)
- System.Management — MIT (Microsoft)
- .NET 9 Runtime (if self-contained is distributed) — MIT
- Segoe Fluent Icons — included in Windows, no separate notice required but mention
Automate via `dotnet-project-licenses` or manually.
**DoD:** The file is included in build outputs (see T5.2).
**Estimate:** 2h

### T5.8 [P1] README update
**Where:** `README.md`
**Action:**
- Remove "FannyKnob" references
- Remove `publish-true-single\` reference
- Update installation instructions to actual path
- Mark self-contained requirement correctly ("no .NET installation required" — now true)
- Add screenshot from Sprint 4
- Add section "Feedback & bugs" → GitHub Issues link
- Version number in badge: `v1.1.0`
**DoD:** README matches the actual distribution.
**Estimate:** 2h

### T5.9 [P1] Remove `releases/` from git history
**Where:** The entire git history
**Why:** 50-138 MB per version × 8 versions = ~800 MB in history. Slows down cloning.
**Action:**
```bash
# Install git-filter-repo first
pip install git-filter-repo
# Or use BFG: https://rtyley.github.io/bfg-repo-cleaner/

git clone --mirror https://github.com/screamm/SystemFlow_Pro systemflow.git
cd systemflow.git
git filter-repo --path releases --invert-paths
git push --force --all
git push --force --tags
```
**WARNING:** This rewrites history. Coordinate with any other clones/forks.
**DoD:** `git clone` is now <20 MB. History contains no `releases/` folder.
**Estimate:** 2h

### T5.10 [P1] `.gitignore` update
**Where:** `.gitignore`
**Action:**
- Verify that `[Rr]eleases/` works (files may have been committed before the pattern was added)
- Add `publish/`, `*.pfx`, `settings.local.json`
- Add `scripts/` (claude_autonomous_loop does not belong here) or move the files to `.dev/` and gitignore it
**DoD:** `git status` after `dotnet publish` shows no new tracked files.
**Estimate:** 0.5h

### T5.11 [P2] Privacy/data collection policy
**Where:** New file `PRIVACY.md`
**Why:** The app collects hardware info + username. If Sentry or other telemetry is enabled, it must be documented.
**Action:** Short document:
- What data the app reads locally (CPU name, GPU name, sensors, OS version, username)
- What data leaves the machine (none by default — if Sentry is enabled: stack traces + OS version)
- If the user enables crash reports: what is sent
- GDPR-relevant points
**DoD:** Linked from the About dialog and README.
**Estimate:** 1h

### T5.12 [P2] Changelog supplement
**Where:** Create `CHANGELOG_v1.0.6.md`, `CHANGELOG_v1.0.7.md`, `CHANGELOG_v1.0.8.md`, `CHANGELOG_v1.1.0.md`
**Action:** Reconstruct from git log what was changed in the missing versions. For v1.1.0: summarize all Sprint 1-5 changes.
**DoD:** Complete changelog chain from v1.0.2 to v1.1.0.
**Estimate:** 2h

### T5.13 [P2] SmartScreen warning documentation
**Where:** README + FAQ
**Action:** Explain that the app is unsigned open source, that SmartScreen warns for that reason, and that the user can click "More info → Run anyway" after reviewing the code on GitHub if desired.
**DoD:** Troubleshooting section in README.
**Estimate:** 0.5h

### T5.14 [P3] `LICENSE` file in root
**Where:** New file `LICENSE`
**Action:** Decide on license (MIT recommended). Copy standard text. Update README.
**DoD:** The file exists, README references the license correctly.
**Estimate:** 0.5h

---

## Risk & dependencies

- **T5.3 (Code signing)** declined — no external dependencies on delivery time.
- **T5.9 (git history rewrite)** is destructive — take a complete backup of the repo before.
- **T5.6 (CI/CD)** may reveal build issues that only appeared locally (path differences, encoding, etc.). Expect debugging.
- Sentry.io requires a free account — setup takes an hour.

---

## Retrospective (fill in after sprint)
