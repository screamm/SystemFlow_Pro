# Sprint 3 — Arkitektur & testbarhet

**Mål:** Extrahera hårdvarulagret från `MainWindow`. Introducera minimal MVVM så UI blir testbart. Etablera enhetstestprojekt. Minska `MainWindow.xaml.cs` från 1232 rader till <400.

**Varaktighet:** 2 veckor (~60-80h)
**Branch:** `sprint-03-arkitektur`
**Målversion:** v1.1.0-beta.1 (efter Sprint 4)
**Förutsättningar:** Sprint 1 + 2 klara

**Utgångsläge:** Stabilt och snabbt, men fortfarande en god-class. Noll tester, noll separation av UI/logik/data.

---

## Sprintmål

- [ ] `Services/HardwareService.cs` — all LHM + WMI-logik isolerad
- [ ] `Services/SystemInfoService.cs` — OS-version, username, hårdvaru-namn
- [ ] `Models/SystemSnapshot.cs` — immutable record med all tick-data
- [ ] `ViewModels/MainViewModel.cs` — `INotifyPropertyChanged`, binding-target
- [ ] `MainWindow.xaml` binder mot `MainViewModel` (ingen `x:Name`-manipulering)
- [ ] `MainWindow.xaml.cs` ≤ 400 rader (endast lifecycle + window chrome)
- [ ] `Tests/SystemFlow.Tests.csproj` med minst 15 gröna tester
- [ ] CI-körning av tester förberedd (full workflow kommer i Sprint 5)

---

## Tasks

### T3.1 [P0] Skapa filstruktur
**Var:** Nya mappar i projektroten
**Åtgärd:**
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
    SettingsService.cs    (för polling-intervall mm, används från Sprint 4)
    Logger.cs             (flyttas från Sprint 1)
  ViewModels/
    MainViewModel.cs
    ObservableObject.cs   (bas med INotifyPropertyChanged)
  Views/
    MainWindow.xaml(.cs)   (flyttas eventuellt hit, eller lämna i root)
    SplashWindow.xaml(.cs)
```
**DoD:** Projektet kompilerar efter mappning (uppdatera csproj vid behov — .NET 9 auto-inkluderar `.cs`-filer rekursivt).
**Estimat:** 1h

### T3.2 [P0] Extrahera `HardwareService`
**Var:** Ny fil `Services/HardwareService.cs`, källa: MainWindow.xaml.cs rader 88-120 (Computer init) + alla getters (`GetGpuUsage`, `GetAverageTemperature`, `GetThermalData`, `TryLibreHardwareMonitorFans`, `GetGpuInfo`, `GetTotalMemoryGB`, `GetHardwareInfo`, `GetFriendlyOSName` — ca 600 rader totalt).
**Varför:** Utan separation är logiken otestbar och inte återanvändbar.
**Åtgärd:**
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
    private readonly float _totalMemoryGB; // cachad vid init
    // ... alla private helpers

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
**DoD:** `MainWindow` innehåller inte längre `using LibreHardwareMonitor.Hardware;` eller `using System.Management;`.
**Estimat:** 12h

### T3.3 [P0] Definiera `SystemSnapshot`-modellen
**Var:** `Models/SystemSnapshot.cs`
**Åtgärd:**
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
    string SystemStatus  // OPTIMAL | MEDIUMBELASTNING | HÖG BELASTNING
);

public sealed record CpuCoreInfo(int Index, string Name, float UsagePercent, float? TemperatureC);
public sealed record FanInfo(string Name, float? Rpm, float? PwmPercent, bool IsZeroRpmMode);
public sealed record ThermalReading(string Name, float TemperatureC, string Source);
```
**DoD:** Records kompilerar, fält korrelerar till alla värden UI:n visar.
**Estimat:** 1h

### T3.4 [P0] Implementera `ObservableObject` + `MainViewModel`
**Var:** `ViewModels/ObservableObject.cs` + `ViewModels/MainViewModel.cs`
**Åtgärd:**
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
    // Wrap records i VM-klasser om binding kräver mutable

    public MainViewModel(IHardwareService hardware) { _hardware = hardware; /* timer setup */ }

    private async void OnTick(object? s, EventArgs e)
    {
        var snap = await _hardware.CollectSnapshotAsync();
        ApplySnapshot(snap);
    }

    private void ApplySnapshot(SystemSnapshot s)
    {
        CpuUsage = s.CpuUsagePercent;
        // merge CpuCores into observable collection by index (uppdatera inplace)
    }
}
```
**DoD:** ViewModel kompilerar, kan instansieras med mock-`IHardwareService`.
**Estimat:** 8h

### T3.5 [P0] Binda XAML mot ViewModel
**Var:** `MainWindow.xaml` + `MainWindow.xaml.cs`
**Åtgärd:**
- I constructor: `DataContext = new MainViewModel(App.HardwareService);`
- Ersätt alla `x:Name`-uppdateringar i code-behind med `{Binding PropertyName}` i XAML:
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
- Lägg till value converters där behövs (`PercentFormatConverter`, `TempColorConverter`)
- Behåll code-behind endast för: window chrome (dragmove, min/close-knappar), window state change för timer-paus
**DoD:** `MainWindow.xaml.cs` ≤ 400 rader. Inga `.Text =` tilldelningar (bara binding).
**Estimat:** 12h

### T3.6 [P1] Skapa testprojekt
**Var:** Ny mapp `Tests/SystemFlow.Tests/` med egen `SystemFlow.Tests.csproj`
**Åtgärd:**
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
Lägg till i `SystemFlow_Pro.sln`.
**DoD:** `dotnet test` kör och rapporterar "0 tests" utan fel.
**Estimat:** 1h

### T3.7 [P1] Enhetstester: statuslogik
**Var:** `Tests/SystemFlow.Tests/SystemStatusTests.cs`
**Åtgärd:** Extrahera tröskellogiken (som idag ligger i `UpdateDetailedPanels`) till `SystemStatusEvaluator.Evaluate(snapshot)`. Testa:
```csharp
[Theory]
[InlineData(30, 40, "OPTIMAL")]
[InlineData(65, 70, "MEDIUMBELASTNING")]
[InlineData(85, 90, "HÖG BELASTNING")]
public void Evaluate_ReturnsExpectedStatus(float cpu, float gpu, string expected)
{
    var snap = new SystemSnapshot(... cpu, gpu ...);
    SystemStatusEvaluator.Evaluate(snap).Should().Be(expected);
}
```
Täck också edge cases: null-temps, tomma core-listor, >100% usage (ogiltigt).
**DoD:** Minst 6 gröna tester för statuslogiken.
**Estimat:** 3h

### T3.8 [P1] Enhetstester: `GetFriendlyOSName`
**Var:** `Tests/SystemFlow.Tests/SystemInfoServiceTests.cs`
**Åtgärd:** Om `GetFriendlyOSName` tar OS-version som parameter (inte läser från miljö) kan den testas direkt:
```csharp
[InlineData(10, 0, 22000, "Windows 11")]
[InlineData(10, 0, 19045, "Windows 10 22H2")]
[InlineData(6, 3, 0, "Windows 8.1")]
```
Annars: refaktorera så versionsuppslagning tar `Version` som argument.
**DoD:** Minst 4 gröna tester.
**Estimat:** 2h

### T3.9 [P1] Enhetstester: fläkt-klassificering
**Var:** `Tests/SystemFlow.Tests/FanInfoTests.cs`
**Åtgärd:** Testa logiken som separerar RPM vs procent, identifierar zero-RPM-mode, mappar sensor-typ till FanInfo.
**DoD:** Minst 5 gröna tester inklusive zero-RPM, pumpsensor, vanlig fläkt.
**Estimat:** 3h

### T3.10 [P1] Integrationstest: `HardwareService` lifecycle
**Var:** `Tests/SystemFlow.Tests/HardwareServiceTests.cs`
**Åtgärd:** Verifiera att:
- `CollectSnapshotAsync()` returnerar en snapshot utan att kasta på normal hårdvara
- `Dispose()` anropas säkert och stänger `Computer`
- Två parallella `CollectSnapshotAsync` serialiseras korrekt (inte krasch)
Markera test som `[Trait("Category", "Integration")]` — kräver riktig hårdvara, körs inte alltid i CI.
**DoD:** 3 gröna tester markerade som integration.
**Estimat:** 3h

### T3.11 [P2] Settings-backing via `SettingsService`
**Var:** `Services/SettingsService.cs`
**Varför:** Sprint 4 behöver detta för Settings-UI.
**Åtgärd:**
```csharp
public sealed record AppSettings(
    int PollIntervalMs = 2000,
    string TemperatureUnit = "C",  // "C" | "F"
    bool StartMinimized = false,
    bool PauseWhenMinimized = true
);

public interface ISettingsService { AppSettings Current { get; } Task SaveAsync(AppSettings s); }
public sealed class JsonSettingsService : ISettingsService { /* System.Text.Json till %APPDATA% */ }
```
**DoD:** `SettingsService` kan läsa/skriva `settings.json`, defaults returneras vid första körning.
**Estimat:** 3h

### T3.12 [P2] Dokumentera arkitekturen
**Var:** Ny fil `docs/ARCHITECTURE.md`
**Åtgärd:** Kort beskrivning: lagerdiagram (View → ViewModel → Services → LHM/WMI), varför MVVM-lite, vad som är testbart. Ska räcka för att en ny utvecklare förstår strukturen på 15 min.
**DoD:** Mermaid-diagram + 2-3 stycken prosa.
**Estimat:** 2h

---

## Risk & beroenden

- **T3.2 (HardwareService)** är sprintens hårda nöt. Om `MainWindow` har dolda couplings (statiska fält, events tillbaka till UI) kan det ta 2x estimatet.
- **T3.5 (XAML-binding)** kräver att du behärskar value converters och `ItemsControl` — planera in tid för att lära/repetera om osäker.
- **Tester** kan kännas omöjliga om service-interfacen läcker `Computer`-objekt. Håll interfacen rena — använd records som DTO:er över tråd-gränsen.
- Efter denna sprint är rullning tillbaka dyr. Tagga v1.0.10 före start.

---

## Retro (fyll i efter sprint)
