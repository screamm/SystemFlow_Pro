# Sprint 2 — Performance & resource management

**Goal:** Decouple the UI thread from WMI/LHM. Eliminate GC pressure from UI panels. Stop memory leaks. Implement intelligent polling.

**Duration:** 1 week (~30-40h)
**Branch:** `sprint-02-prestanda`
**Target version:** v1.0.10
**Prerequisites:** Sprint 1 complete (Accept cached, .Result gone, logger present)

**Starting point after Sprint 1:** The app is stable but still CPU-hungry (8-15% continuously), UI flickers during updates, panels are torn down and rebuilt every second.

---

## Sprint goal

- [ ] Timer_Tick runs on a background thread, UI updates marshalled to the dispatcher
- [ ] No `Panel.Children.Clear()` + `Add()` in the hot path (done once at init)
- [ ] Timer pauses when the window is minimized
- [ ] Event handlers unsubscribed on close
- [ ] Splash init on background thread — MainWindow constructor returns quickly
- [ ] Continuous CPU overhead <3% on reference machine
- [ ] Tick cost <50ms (down from ~300ms)

---

## Tasks

### T2.1 [P0] Move Timer_Tick work to a background thread
**Where:** `MainWindow.xaml.cs:Timer_Tick` + `UpdateSystemData` + all getters
**Why:** 1 Hz WMI/LHM calls on the UI thread = ~200-500ms blocking/second.
**Action:**
```csharp
private readonly SemaphoreSlim _tickGate = new(1, 1);

private async void Timer_Tick(object? sender, EventArgs e)
{
    if (!await _tickGate.WaitAsync(0)) return; // skip if previous tick still running
    try
    {
        var snapshot = await Task.Run(() => CollectSystemSnapshot());
        ApplySnapshotToUI(snapshot); // already runs on UI thread via DispatcherTimer
    }
    catch (Exception ex) { Logger.Error("Tick failed", ex); }
    finally { _tickGate.Release(); }
}
```
- Create a new `SystemSnapshot` record/class with all values needed for the UI
- `CollectSystemSnapshot()` does Accept + all reads, returns values
- `ApplySnapshotToUI(snapshot)` updates `TextBlock.Text` etc — runs on UI thread
**DoD:** UI freeze during Tick <10ms according to Visual Studio Diagnostics Tools. Alt-tab during polling feels responsive.
**Estimate:** 6h

### T2.2 [P0] Cache UI panels (no Clear+Add every tick)
**Where:** `MainWindow.xaml.cs` — `UpdateCpuCoresPanel` (224), `UpdateMemoryPanel` (265), `UpdateGpuInfoPanel` (296), `UpdateThermalPanel` (325), `UpdateSystemPanel` (524), `UpdateHardwarePanel` (573)
**Why:** 30-100 UIElements/sec created and discarded → GC Gen0 every 2-3s, layout pass over the entire tree.
**Action:** Two alternatives, choose per panel:

**Alt A — `ItemsControl` + `ObservableCollection`** (for dynamic lists such as CPU cores):
```xml
<ItemsControl ItemsSource="{Binding CpuCores}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Grid>
                <Grid.ColumnDefinitions>...</Grid.ColumnDefinitions>
                <TextBlock Text="{Binding Name}"/>
                <ProgressBar Value="{Binding UsagePercent}"/>
                <TextBlock Text="{Binding UsageDisplay}"/>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Alt B — Panel built once with named TextBlocks** (for fixed panels such as Hardware):
- Build in XAML with `x:Name` assignments
- In code-behind: only update `.Text` values

**DoD:** `dotMemory` snapshot during run shows <10 UIElement allocations/second (down from 100+).
**Estimate:** 8h

### T2.3 [P0] Pause timer when the window is minimized
**Where:** `MainWindow.xaml.cs` — add `StateChanged` event
**Why:** No reason to poll sensors when the user is not looking. Saves battery on laptops.
**Action:**
```csharp
StateChanged += (s, e) => {
    if (WindowState == WindowState.Minimized)
    {
        _timer.Stop();
        Logger.Info("Timer paused (minimized)");
    }
    else if (_timer?.IsEnabled == false)
    {
        _timer.Start();
        Logger.Info("Timer resumed");
    }
};
```
**DoD:** Minimize + Task Manager shows CPU usage drops to ~0%. Restore window → updates continue.
**Estimate:** 0.5h

### T2.4 [P1] Event unsubscribe in OnClosed
**Where:** `MainWindow.xaml.cs:OnClosed`
**Why:** `_timer.Tick += Timer_Tick` without matching `-=` keeps `MainWindow` alive after Close → classic WPF leak.
**Action:**
```csharp
protected override void OnClosed(EventArgs e)
{
    _timer.Stop();
    _timer.Tick -= Timer_Tick;
    StateChanged -= OnStateChanged; // if the above is moved to a named method
    _computer?.Close();
    _computer = null;
    _tickGate?.Dispose();
    base.OnClosed(e);
}
```
**DoD:** `dotMemory` snapshot after Close shows 0 instances of MainWindow (previously 1+).
**Estimate:** 1h

### T2.5 [P1] Decouple Splash/MainWindow init
**Where:** `App.xaml.cs` + `SplashWindow.xaml.cs:49-73` + `MainWindow.xaml.cs:InitializeCounters`
**Why:** `new MainWindow()` runs `computer.Open()` + `Accept()` on the UI thread (1-3s). Splash freezes.
**Action:**
1. Make the `MainWindow` constructor minimal: only `InitializeComponent()` + field init
2. Create `public async Task InitializeAsync()` that runs LHM init on a background thread:
```csharp
public async Task InitializeAsync()
{
    await Task.Run(() => {
        _computer = new Computer { IsCpuEnabled = true, ... };
        _computer.Open();
    });
    StartTimer();
}
```
3. `App.xaml.cs`:
```csharp
_splashWindow.Show();
var main = new MainWindow();
await main.InitializeAsync();
main.Show();
_splashWindow.Close();
```
4. Remove the nested `Task.Run → InvokeAsync → Task.Run` pyramid in `SplashWindow.CloseSplash`.

**DoD:** Splash appears immediately (<200ms), closes as soon as init is complete. No hardcoded `Task.Delay(2000)`.
**Estimate:** 4h

### T2.6 [P1] Fix %→RPM heuristic
**Where:** `MainWindow.xaml.cs:781, 812, 818, 847`
**Why:** The current `if (fanSpeed <= 100) fanSpeed *= 30f;` assumes all values ≤100 are percentages. Wrong for zero-RPM-mode GPUs and pump sensors.
**Action:** Use `sensor.SensorType` correctly:
```csharp
if (sensor.SensorType == SensorType.Fan)      // RPM, unchanged
    displayValue = $"{value:0} RPM";
else if (sensor.SensorType == SensorType.Control) // Percent of max PWM
    displayValue = $"{value:0}%";
```
Separate RPM and percent sensors in the UI (two rows per fan if both exist).
**DoD:** GPU in zero-RPM idle shows "0 RPM" (not "0%" converted to "0 RPM"). Pump sensor shows correct RPM.
**Estimate:** 2h

### T2.7 [P2] Remove duplicate CPU core sampling
**Where:** `MainWindow.xaml.cs:229-231` + `_cpuCoreCounters` field
**Why:** Both `PerformanceCounter.NextValue()` AND LHM read per-core usage. Double cost, potentially inconsistent values.
**Action:** Choose one source. Recommendation: keep LHM (consistent with the rest of the app). Remove `_cpuCoreCounters` field, loop in `InitializeCounters`, and all usage.
**DoD:** `_cpuCoreCounters` no longer exists. CPU panel displays correct per-core values from LHM.
**Estimate:** 1h

### T2.8 [P2] Reduce string allocations in the hot path
**Where:** `MainWindow.xaml.cs:340, 414` + value formatting
**Why:** `Substring` + `$"{}"` in inner loops → many small string allocations = GC pressure.
**Action:**
- Skip `Substring` when `Length ≤ 25` (no truncation needed)
- Use `string.Create` or a pre-allocated `StringBuilder` where the format is repeated
- Cache format strings as const
**DoD:** dotMemory snapshot shows <200 string allocations/second during polling (down from 500+).
**Estimate:** 2h

### T2.9 [P2] Configurable polling interval
**Where:** `MainWindow.xaml.cs:126` + new settings backing
**Why:** Hardcoded 1s is too aggressive in the background, but users with large cooling systems want 500ms.
**Action:** Add field `_pollIntervalMs` with default 2000ms. Settings file `%APPDATA%\SystemFlow Pro\settings.json` (JSON serialization via System.Text.Json). Settings UI comes in Sprint 4.
**DoD:** Change in settings.json → after restart the new interval is used.
**Estimate:** 2h

### T2.10 [P3] Benchmark before/after
**Where:** New file `docs/PERFORMANCE_NOTES.md`
**Why:** Without measurements we do not know that optimizations actually help.
**Action:** Measure:
- CPU usage in Task Manager, average over 5 min (minimized + focused)
- Tick cost via `Stopwatch` around `CollectSystemSnapshot`
- GC Gen0/min via PerfView or Visual Studio Diagnostics
Compare against Sprint 0 figures (if available) or pre-Sprint 2 baseline.
**DoD:** Document with before/after table committed.
**Estimate:** 2h

---

## Risk & dependencies

- **T2.1** is the sprint's largest task and a dependency for T2.2. If the `CollectSystemSnapshot` + `ApplySnapshotToUI` architecture takes longer, defer T2.8-T2.10 to backlog.
- **T2.2** requires XAML changes + likely DataContext — if the MVVM foundation is missing (Sprint 3), Alt B (named TextBlocks) can be used as an intermediate step.
- LibreHardwareMonitor is not thread-safe — `_computer.Accept()` must still be **single-caller**. The semaphore in T2.1 guarantees this.

---

## Retrospective (fill in after sprint)
