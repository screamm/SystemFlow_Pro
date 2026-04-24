# SystemFlow Pro v1.1.0-rc.1 — Production pipeline

**Release date:** TBD
**Branch:** `sprint-05-pipeline`

Release candidate. Functionally locked — only production infrastructure
is added from this version on. Sprint 6 is pure QA/beta/release.

## Distribution

- **Self-contained single-file publish.** `SystemMonitorApp.csproj`
  configured with `PublishSingleFile=true`, `SelfContained=true`,
  `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true`,
  `PublishReadyToRun=true`. Result: a single `SystemFlow-Pro.exe` ~70-90 MB
  that runs without a pre-installed .NET runtime.
- **README corrected.** "A SINGLE FILE — no .NET installation required" is
  now true (previously the README lied). References to the non-existent project
  "FannyKnob" and the removed `publish-true-single/` directory have been
  replaced with correct instructions.

## Build system

- **Unified `build.bat`** replaces `build_release_v1.0.2.bat` → `v1.0.8.bat`.
  Argument: `build.bat [version] [--no-compress]`. Runs clean,
  publishes self-contained, copies license files, and zip compresses.
- **Previous build_release_*.bat** kept for backward reference but are
  obsolete. Can be removed in a future cleanup pass.

## CI/CD

- **`.github/workflows/ci.yml`** — runs on every push/PR. Restore →
  build (warn-as-error on CS1998) → test (xUnit, 37 tests) → smoke publish
  → artifacts uploaded. Windows-latest runner, `global.json` pins
  SDK 9.0.305.
- **`.github/workflows/release.yml`** — triggers on tag `v*`. Builds
  self-contained single-file, creates zip, publishes a GitHub Release
  with auto-generated release notes. The prerelease flag is set automatically
  if the tag contains `-` (e.g. `v1.1.0-rc.1`). The distribution is unsigned
  (open source, see SmartScreen handling in README and FAQ).

## Security

- **Auto-update check** against the GitHub Releases API via
  `Services/UpdateChecker.cs`. HTTP GET (User-Agent: "SystemFlow-Pro",
  5 second timeout) 8 seconds after app start. When an update is available
  a MessageBox is shown with the "Open release page?" question. All
  errors are silent — offline / rate-limited never breaks the app.
- **`THIRD_PARTY_LICENSES.txt`** — complete attribution for
  LibreHardwareMonitor (MPL 2.0), .NET 9 runtime (MIT), System.Management
  (MIT), Segoe fonts (Microsoft proprietary, included in Windows).
  Automatically copied to the release folder by `build.bat` and `release.yml`.
- **`LICENSE`** — MIT license on the app code, copyright David Rydgren
  2025-2026.
- **`PRIVACY.md`** — GDPR-compliant policy. Documents that no data is
  sent externally, lists locally stored files (`settings.json`,
  log files), describes the network call for the update check and what
  the user can do to disable it.

## Diagnostics

- `DispatcherUnhandledException` + `TaskScheduler.UnobservedTaskException`
  + `AppDomain.CurrentDomain.UnhandledException` are registered (already done
  in Sprint 1 — verified that the crash report is written to
  `%APPDATA%\SystemFlow Pro\logs\`).

## Hygiene

- `.gitignore` updated: ignores `publish/`, `*.pfx`, `*.p12`, `*.snk`,
  `cert.*`, `settings.local.json`, `scripts/` (claude dev scripts do not
  belong in the production repo).

## Distribution — unsigned, open source

Code signing is not planned. SystemFlow Pro is distributed unsigned;
users handle the SmartScreen warning via "More info" → "Run anyway".
Rationale: certificates cost 200-500 USD/year and the project is free and
open source — all code can be reviewed on GitHub instead. README and FAQ
document this for end users.

## Not completed (requires manual action)

- **Git history rewrite** of `releases/` (50-138 MB per version in
  the history). Destructive operation — requires user confirmation and should
  be done outside this session. Command: `git filter-repo --path releases --invert-paths`
  followed by `git push --force`.
- **Changelogs for v1.0.6/v1.0.7/v1.0.8** — the git history covers these
  changes; manual reconstruction has not been done.

## Files affected

### New files
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `build.bat` (replaced)
- `LICENSE`
- `PRIVACY.md`
- `THIRD_PARTY_LICENSES.txt`
- `Services/UpdateChecker.cs`

### Modified files
- `App.xaml.cs` — `CheckForUpdatesInBackground` added
- `README.md` — completely rewritten, fact-based
- `.gitignore` — expanded with cert/pfx/dev paths
- `SystemMonitorApp.csproj` — self-contained publish config, metadata,
  version 1.1.0-rc.1

## Ready for Sprint 6

- [x] Self-contained exe buildable via `build.bat 1.1.0`
- [x] CI workflow validates build + tests on every commit
- [x] Release workflow creates a GitHub Release on tag push
- [x] Legal complete: LICENSE + THIRD_PARTY_LICENSES + PRIVACY
- [x] Auto-update mechanism live
- [x] Distribution: unsigned + documented (not a blocker)
