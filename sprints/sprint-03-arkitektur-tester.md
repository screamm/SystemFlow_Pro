# Sprint 3 — Architecture & testability

**Goal:** Extract the hardware layer from `MainWindow`. Introduce minimal MVVM so the UI becomes testable. Establish a unit test project. Reduce `MainWindow.xaml.cs` from 1232 lines to <400.

**Duration:** 2 weeks (~60-80h)
**Branch:** `sprint-03-arkitektur`
**Target version:** v1.1.0-beta.1 (after Sprint 4)
**Prerequisites:** Sprints 1 + 2 complete

**Starting point:** Stable and fast, but still a god class. Zero tests, zero separation of UI/logic/data.

---

## Sprint goal

- [ ] `Services/HardwareService.cs` — all LHM + WMI logic isolated
- [ ] `Services/SystemInfoService.cs` — OS version, username, hardware names
- [ ] `Models/SystemSnapshot.cs` — immutable record with all tick data
- [ ] `ViewModels/MainViewModel.cs` — `INotifyPropertyChanged`, binding target
- [ ] `MainWindow.xaml` binds to `MainViewModel` (no `x:Name` manipulation)
- [ ] `MainWindow.xaml.cs` ≤ 400 lines (only lifecycle + window chrome)
- [ ] `Tests/SystemFlow.Tests.csproj` with at least 15 passing tests
- [ ] CI execution of tests prepared (full workflow comes in Sprint 5)

---

## Tasks

### T3.1 [P0] Create file structure
**Where:** New folders in the project root
**Action:**
```
SystemFlow_Pro/
  Models/
    SystemSnapshot.cs
    CpuCoreInfo.cs
    FanInfo.cs
    ThermalReading.cs
  Services/
    HardwareService.cs
    SystemInfoService.cs
    SettingsService.cs    (for polling interval etc., used from Sprint 4)
    Logger.cs             (moved from Sprint 1)
  ViewModels/
    MainViewModel.cs
    ObservableObject.cs   (base with INotifyPropertyChanged)
  Views/
    MainWindow.xaml(.cs)   (optionally moved here, or left in root)
    SplashWindow.xaml(.cs)
```
**DoD:** The project compiles after the mapping (update csproj if needed — .NET 9 auto-includes `.cs` files recursively).
**Estimate:** 1h

### T3.2 [P0] Extract `HardwareService`
**Where:** New file `Services/HardwareService.cs`, source: MainWindow.xaml.cs lines 88-120 (Computer init) + all getters (`GetGpuUsage`, `GetAverageTemperature`, `GetThermalData`, `TryLibreHardwareMonitorFans`, `GetGpuInfo`, `GetTotalMemoryGB`, `GetHardwareInfo`, `GetFriendlyOSName` — approx. 600 lines total).
**Why:** Without separation the logic is untestable and not reusable.
**Action:**
```csharp
public interface IHardwareService : IDisposable
{
    Task<SystemSnapshot> CollectSnapshotAsync(CancellationToken ct = default);
    bool IsHardwareAvailable { get; }
    bool IsRunningAsAdmin { get; }
}

public sealed class HardwareService : IHardwareService
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private readonly float _totalMemoryGB; // cached at init
    // ... all private helpers

    public async Task<SystemSnapshot> CollectSnapshotAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => {
            _computer.Accept(_visitor);
            return new SystemSnapshot(
                CpuUsage: GetCpuUsage(),
                CpuTemp: GetCpuTemperature(),
                GpuUsage: GetGpuUsage(),
                // ...
            );
        }, ct);
    }
}
```
**DoD:** `MainWindow` no longer contains `using LibreHardwareMonitor.Hardware;` or `using System.Management;`.
**Estimate:** 12h

### T3.3 [P0] Define the `SystemSnapshot` model
**Where:** `Models/SystemSnapshot.cs`
**Action:**
```csharp
public sealed record SystemSnapshot(
    DateTime Timestamp,
    float CpuUsagePercent,
    float? CpuTemperatureC,
    IReadOnlyList<CpuCoreInfo> CpuCores,
    float GpuUsagePercent,
    float? GpuTemperatureC,
    string? GpuName,
    float TotalMemoryGB,
    float UsedMemoryGB,
    IReadOnlyList<FanInfo> Fans,
    IReadOnlyList<ThermalReading> AdditionalThermal,
    string SystemStatus  // OPTIMAL | MEDIUM LOAD | HIGH LOAD
);

public sealed record CpuCoreInfo(int Index, string Name, float UsagePercent, float? TemperatureC);
public sealed record FanInfo(string Name, float? Rpm, float? PwmPercent, bool IsZeroRpmMode);
public sealed record ThermalReading(string Name, float TemperatureC, string Source);
```
**DoD:** Records compile, fields correlate to all values the UI displays.
**Estimate:** 1h

### T3.4 [P0] Implement `ObservableObject` + `MainViewModel`
**Where:** `ViewModels/ObservableObject.cs` + `ViewModels/MainViewModel.cs`
**Action:**
```csharp
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }
}

public sealed class MainViewModel : ObservableObject
{
    private readonly IHardwareService _hardware;
    private readonly DispatcherTimer _timer;

    public float CpuUsage { get => _cpuUsage; set => SetField(ref _cpuUsage, value); }
    // ... 20+ bindable properties

    public ObservableCollection<CpuCoreVm> CpuCores { get; } = new();
    public ObservableCollection<FanVm> Fans { get; } = new();
    // Wrap records in VM classes if binding requires mutable

    public MainViewModel(IHardwareService hardware) { _hardware = hardware; /* timer setup */ }

    private async void OnTick(object? s, EventArgs e)
    {
        var snap = await _hardware.CollectSnapshotAsync();
        ApplySnapshot(snap);
    }

    private void ApplySnapshot(SystemSnapshot s)
    {
        CpuUsage = s.CpuUsagePercent;
        // merge CpuCores into observable collection by index (update in place)
    }
}
```
**DoD:** ViewModel compiles, can be instantiated with a mock `IHardwareService`.
**Estimate:** 8h

### T3.5 [P0] Bind XAML to ViewModel
**Where:** `MainWindow.xaml` + `MainWindow.xaml.cs`
**Action:**
- In constructor: `DataContext = new MainViewModel(App.HardwareService);`
- Replace all `x:Name` updates in code-behind with `{Binding PropertyName}` in XAML:
```xml
<TextBlock Text="{Binding CpuUsageDisplay}" Foreground="{Binding CpuStatusBrush}"/>
<ProgressBar Value="{Binding CpuUsage}"/>
<ItemsControl ItemsSource="{Binding CpuCores}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Grid><TextBlock Text="{Binding Name}"/><ProgressBar Value="{Binding UsagePercent}"/></Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```
- Add value converters where needed (`PercentFormatConverter`, `TempColorConverter`)
- Keep code-behind only for: window chrome (dragmove, min/close buttons), window state change for timer pause
**DoD:** `MainWindow.xaml.cs` ≤ 400 lines. No `.Text =` assignments (only binding).
**Estimate:** 12h

### T3.6 [P1] Create test project
**Where:** New folder `Tests/SystemFlow.Tests/` with its own `SystemFlow.Tests.csproj`
**Action:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.*" />
    <PackageReference Include="FluentAssertions" Version="7.0.*" />
    <PackageReference Include="NSubstitute" Version="5.3.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\SystemMonitorApp.csproj" />
  </ItemGroup>
</Project>
```
Add to `SystemFlow_Pro.sln`.
**DoD:** `dotnet test` runs and reports "0 tests" without errors.
**Estimate:** 1h

### T3.7 [P1] Unit tests: status logic
**Where:** `Tests/SystemFlow.Tests/SystemStatusTests.cs`
**Action:** Extract the threshold logic (currently in `UpdateDetailedPanels`) into `SystemStatusEvaluator.Evaluate(snapshot)`. Test:
```csharp
[Theory]
[InlineData(30, 40, "OPTIMAL")]
[InlineData(65, 70, "MEDIUM LOAD")]
[InlineData(85, 90, "HIGH LOAD")]
public void Evaluate_ReturnsExpectedStatus(float cpu, float gpu, string expected)
{
    var snap = new SystemSnapshot(... cpu, gpu ...);
    SystemStatusEvaluator.Evaluate(snap).Should().Be(expected);
}
```
Also cover edge cases: null temps, empty core lists, >100% usage (invalid).
**DoD:** At least 6 passing tests for the status logic.
**Estimate:** 3h

### T3.8 [P1] Unit tests: `GetFriendlyOSName`
**Where:** `Tests/SystemFlow.Tests/SystemInfoServiceTests.cs`
**Action:** If `GetFriendlyOSName` takes the OS version as a parameter (rather than reading from environment) it can be tested directly:
```csharp
[InlineData(10, 0, 22000, "Windows 11")]
[InlineData(10, 0, 19045, "Windows 10 22H2")]
[InlineData(6, 3, 0, "Windows 8.1")]
```
Otherwise: refactor so version lookup takes `Version` as an argument.
**DoD:** At least 4 passing tests.
**Estimate:** 2h

### T3.9 [P1] Unit tests: fan classification
**Where:** `Tests/SystemFlow.Tests/FanInfoTests.cs`
**Action:** Test the logic that separates RPM vs percent, identifies zero-RPM mode, maps sensor type to FanInfo.
**DoD:** At least 5 passing tests including zero-RPM, pump sensor, normal fan.
**Estimate:** 3h

### T3.10 [P1] Integration test: `HardwareService` lifecycle
**Where:** `Tests/SystemFlow.Tests/HardwareServiceTests.cs`
**Action:** Verify that:
- `CollectSnapshotAsync()` returns a snapshot without throwing on normal hardware
- `Dispose()` is called safely and closes `Computer`
- Two parallel `CollectSnapshotAsync` calls serialize correctly (no crash)
Mark tests with `[Trait("Category", "Integration")]` — requires real hardware, not always run in CI.
**DoD:** 3 passing tests marked as integration.
**Estimate:** 3h

### T3.11 [P2] Settings backing via `SettingsService`
**Where:** `Services/SettingsService.cs`
**Why:** Sprint 4 needs this for the Settings UI.
**Action:**
```csharp
public sealed record AppSettings(
    int PollIntervalMs = 2000,
    string TemperatureUnit = "C",  // "C" | "F"
    bool StartMinimized = false,
    bool PauseWhenMinimized = true
);

public interface ISettingsService { AppSettings Current { get; } Task SaveAsync(AppSettings s); }
public sealed class JsonSettingsService : ISettingsService { /* System.Text.Json to %APPDATA% */ }
```
**DoD:** `SettingsService` can read/write `settings.json`, defaults returned on first run.
**Estimate:** 3h

### T3.12 [P2] Document the architecture
**Where:** New file `docs/ARCHITECTURE.md`
**Action:** Short description: layer diagram (View → ViewModel → Services → LHM/WMI), why MVVM-lite, what is testable. Should be enough for a new developer to understand the structure in 15 minutes.
**DoD:** Mermaid diagram + 2-3 paragraphs of prose.
**Estimate:** 2h

---

## Risk & dependencies

- **T3.2 (HardwareService)** is the sprint's tough nut. If `MainWindow` has hidden couplings (static fields, events back to UI) it may take 2x the estimate.
- **T3.5 (XAML binding)** requires mastery of value converters and `ItemsControl` — plan time to learn/review if uncertain.
- **Tests** may feel impossible if service interfaces leak `Computer` objects. Keep interfaces clean — use records as DTOs across thread boundaries.
- After this sprint, rolling back is expensive. Tag v1.0.10 before starting.

---

## Retrospective (fill in after sprint)
