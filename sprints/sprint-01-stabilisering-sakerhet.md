# Sprint 1 — Stabilization & security

**Goal:** Eliminate all active bugs that can crash the app or mask production errors. Remove unnecessary admin requirements. Lay the groundwork for diagnostics.

**Duration:** 1 week (~30-40h solo part-time)
**Branch:** `sprint-01-stabilisering`
**Target version:** v1.0.9

**Starting point:** v1.0.8.1 — the app works but silently swallows exceptions, has deadlock risk, requires unnecessary admin, and lacks crash logging.

---

## Sprint goal (Definition of Success)

- [ ] App starts without admin rights (asInvoker) with graceful degradation
- [ ] `.Result` eliminated from the UI thread
- [ ] `DispatcherUnhandledException` catches and logs crashes to file
- [ ] All 13 empty `catch {}` replaced with structured logging
- [ ] Code compiles without CS1998 warnings (async without await)
- [ ] `<Nullable>enable</Nullable>` enabled, new warnings addressed
- [ ] Dead code removed (`_gpuCounter`, `EstimateFanSpeedFromTemperature`)

---

## Tasks

### T1.1 [P0] Introduce a simple file logger
**Where:** New file `Services/Logger.cs`
**Why:** All following tasks assume a logger exists. Without one, empty catch blocks are replaced with `Debug.WriteLine` which does not help users in the field.
**Action:**
- Create a static `Logger` class that writes to `%APPDATA%\SystemFlow Pro\logs\app-{yyyy-MM-dd}.log`
- Methods: `Info(string)`, `Warn(string, Exception?)`, `Error(string, Exception?)`
- Automatic rotation at >5 MB, keep the most recent 5 files
- Thread-safe via `lock` or `ConcurrentQueue` + background flush
**DoD:** Can call `Logger.Error("test", ex)` from MainWindow and verify that the log file is created.
**Estimate:** 3h

### T1.2 [P0] Unhandled exception handler
**Where:** `App.xaml.cs:OnStartup`
**Why:** The app crashes silently today. Without this you will not know it crashes for users.
**Action:**
```csharp
DispatcherUnhandledException += (s, e) => {
    Logger.Error("Unhandled UI exception", e.Exception);
    MessageBox.Show($"An error occurred. The log has been saved to %APPDATA%\\SystemFlow Pro\\logs.\n\n{e.Exception.Message}",
        "SystemFlow Pro", MessageBoxButton.OK, MessageBoxImage.Error);
    e.Handled = true;
};
TaskScheduler.UnobservedTaskException += (s, e) => {
    Logger.Error("Unobserved task exception", e.Exception);
    e.SetObserved();
};
AppDomain.CurrentDomain.UnhandledException += (s, e) => {
    Logger.Error("AppDomain unhandled", e.ExceptionObject as Exception);
};
```
**DoD:** Throw a test exception in a button handler → window appears, log is written, app stays alive.
**Estimate:** 1h

### T1.3 [P0] Fix `.Result` on the UI thread
**Where:** `MainWindow.xaml.cs:271` (`UpdateMemoryPanel`)
**Why:** `GetTotalMemoryGB().Result` is a deadlock risk and blocks the UI for 20-200ms.
**Action:** Total RAM never changes during runtime. Read it **once** in `InitializeCounters()` and cache as field `private float _totalMemoryGB`. Remove the `.Result` call.
**DoD:** `Grep` for `\.Result` in the project returns 0 hits. UI rendering of the memory panel takes <5ms.
**Estimate:** 1h

### T1.4 [P0] Consolidate `computer.Accept()` to once per tick
**Where:** `MainWindow.xaml.cs:626, 660, 694, 756, 1005, 1072`
**Why:** The hardware tree is traversed 4-7 times/sec. Race conditions in LibreHardwareMonitor. A new `UpdateVisitor` is allocated for each call.
**Action:**
- Declare `private static readonly UpdateVisitor _visitor = new();`
- Add `_computer.Accept(_visitor);` **once** first in `UpdateSystemData`
- Remove all other `computer.Accept(new UpdateVisitor())` calls in getters
- Also remove redundant `hardware.Update()` / `subHardware.Update()` in inner loops
**DoD:** Search for `Accept(` and `.Update()` in MainWindow.xaml.cs shows at most 1 Accept call + no inner Update loops.
**Estimate:** 2h

### T1.5 [P0] Replace all empty `catch {}` with structured logging
**Where:** `MainWindow.xaml.cs` lines 185, 216, 251, 282, 311, 363, 509, 559, 588, 1106, 1138, 1210 (13 total)
**Why:** Production errors disappear without trace.
**Action:** For each:
```csharp
catch (Exception ex)
{
    Logger.Warn($"Failed in {nameof(MethodName)}: {ex.Message}", ex);
    // return safe default value
}
```
- Still swallows the exception but leaves a trace
- Some should escalate to the user (critical sensors not found) — mark with TODO + user-friendly fallback UI
**DoD:** `Grep` for `catch\s*\{\s*\}` in .cs files returns 0 hits. `catch (Exception)` without `ex` — 0 hits.
**Estimate:** 3h

### T1.6 [P0] `requireAdministrator` → `asInvoker`
**Where:** `app.manifest:19`
**Why:** No code path requires admin. Blocks non-admin users and increases attack surface if compromised.
**Action:**
```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```
Test: start as a regular user. Sensors that require admin (certain MSR reads in LHM) will return null — confirm that the UI degrades to "N/A" without crashing.
Add a visible indicator in the header: "Run as admin for more sensors" if `!IsRunningAsAdmin()`.
**DoD:** The app starts and runs without a UAC prompt. The header badge shows admin status correctly.
**Estimate:** 2h

### T1.7 [P1] Enable modern C# and nullable
**Where:** `SystemMonitorApp.csproj`
**Why:** .NET 9 without `<Nullable>` is wasteful. Catches null bugs at compile time.
**Action:**
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
<WarningsAsErrors>CS8600;CS8602;CS8603;CS8625;CS1998</WarningsAsErrors>
```
Address new warnings one at a time. Count may be 30-80 depending on codebase.
**DoD:** Build without warnings. All null references clearly marked (`?` or `!`).
**Estimate:** 6-8h

### T1.8 [P1] Fix `async` without `await`
**Where:** `MainWindow.xaml.cs:600, 620, 654, 686, 1063`
**Why:** CS1998. The methods are not asynchronous — they block the UI thread.
**Action:**
- For methods that read WMI/LHM: `await Task.Run(() => ...)` around CPU-intensive work
- For methods that do not need to be async: remove `async` and return the value directly
- See Sprint 2 for full threading — this is a "minimum viable" fix
**DoD:** No CS1998 warnings. All `async` methods have at least one `await`.
**Estimate:** 3h

### T1.9 [P1] WMI timeouts
**Where:** `MainWindow.xaml.cs:604, 904, 926, 1119` (all `ManagementObjectSearcher`)
**Why:** WMI hangs indefinitely on broken systems.
**Action:**
```csharp
var options = new EnumerationOptions
{
    Timeout = TimeSpan.FromSeconds(2),
    ReturnImmediately = false
};
using var searcher = new ManagementObjectSearcher(null, query, options);
```
**DoD:** All `ManagementObjectSearcher` calls have timeout <= 2s.
**Estimate:** 1h

### T1.10 [P2] Remove dead code
**Where:**
- `_gpuCounter` field (line 21) + dispose (line 1208) — never assigned
- `_cpuCoreCounters` — used in parallel with LHM, duplicate work. Choose one (recommendation: keep LHM, remove PerformanceCounter)
- `EstimateFanSpeedFromTemperature` (line 987) — never called
**DoD:** Compiler warnings about unused field/method = 0.
**Estimate:** 1h

### T1.11 [P2] Typos and small UI fixes
**Where:**
- `MainWindow.xaml.cs:534` "HORG LOAD" → "HIGH LOAD"
- `MainWindow.xaml:4` + `SplashWindow.xaml` — add `Icon="app.ico"` (or pack URI)
**DoD:** Taskbar/Alt+Tab shows the correct icon. No typos in visual strings.
**Estimate:** 0.5h

### T1.12 [P2] CHANGELOG for v1.0.9
**Where:** New file `CHANGELOG_v1.0.9.md`
**Action:** Summarize all changes from Sprint 1. Follow the existing format from v1.0.3-v1.0.5.
**DoD:** The file exists and mentions all P0 tasks.
**Estimate:** 0.5h

---

## Risk & dependencies

- **T1.6** (asInvoker) may expose sensors that truly require admin → must gracefully degrade to "N/A". If many sensors disappear for non-admin users: document in README + show tooltip.
- **T1.7** (nullable) may take longer than estimated if the codebase uses null returns as control flow.
- **T1.4** assumes that `UpdateSystemData` runs before all getters — verify the call graph.

---

## Retrospective (fill in after sprint)

- What went well:
- What took longer than expected:
- What was moved to Sprint 2:
