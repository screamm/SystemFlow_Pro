# Sprint 2 — Prestanda & resurshantering

**Mål:** Frikoppla UI-tråden från WMI/LHM. Eliminera GC-trycket från UI-paneler. Stoppa minnesläckor. Implementera intelligent polling.

**Varaktighet:** 1 vecka (~30-40h)
**Branch:** `sprint-02-prestanda`
**Målversion:** v1.0.10
**Förutsättningar:** Sprint 1 klar (Accept cachad, .Result borta, logger finns)

**Utgångsläge efter Sprint 1:** Appen är stabil men fortfarande CPU-hungrig (8-15% kontinuerligt), UI flimrar vid uppdatering, paneler rivs och byggs om varje sekund.

---

## Sprintmål

- [ ] Timer_Tick kör på bakgrundstråd, UI-uppdatering marshallas till dispatcher
- [ ] Inga `Panel.Children.Clear()` + `Add()` i hot path (en gång vid init)
- [ ] Timer pausas vid minimerat fönster
- [ ] Event-handlers unsubscribas vid stängning
- [ ] Splash-init på bakgrundstråd — MainWindow-konstruktor returnerar snabbt
- [ ] Kontinuerlig CPU-overhead <3% på referensmaskin
- [ ] Tick-kostnad <50ms (från ~300ms)

---

## Tasks

### T2.1 [P0] Flytta Timer_Tick-arbete till bakgrundstråd
**Var:** `MainWindow.xaml.cs:Timer_Tick` + `UpdateSystemData` + alla getters
**Varför:** 1 Hz WMI/LHM-anrop på UI-tråd = ~200-500ms blockering/sekund.
**Åtgärd:**
```csharp
private readonly SemaphoreSlim _tickGate = new(1, 1);

private async void Timer_Tick(object? sender, EventArgs e)
{
    if (!await _tickGate.WaitAsync(0)) return; // skip if previous tick still running
    try
    {
        var snapshot = await Task.Run(() => CollectSystemSnapshot());
        ApplySnapshotToUI(snapshot); // körs redan på UI-tråd via DispatcherTimer
    }
    catch (Exception ex) { Logger.Error("Tick failed", ex); }
    finally { _tickGate.Release(); }
}
```
- Skapa en ny `SystemSnapshot` record/class med alla värden som behövs för UI
- `CollectSystemSnapshot()` gör Accept + alla läsningar, returnerar värden
- `ApplySnapshotToUI(snapshot)` uppdaterar `TextBlock.Text` etc — kör på UI-tråd
**DoD:** UI frys under Tick <10ms enligt Visual Studio Diagnostics Tools. Alt-tab under polling känns rapp.
**Estimat:** 6h

### T2.2 [P0] Cacha UI-paneler (ingen Clear+Add varje tick)
**Var:** `MainWindow.xaml.cs` — `UpdateCpuCoresPanel` (224), `UpdateMemoryPanel` (265), `UpdateGpuInfoPanel` (296), `UpdateThermalPanel` (325), `UpdateSystemPanel` (524), `UpdateHardwarePanel` (573)
**Varför:** 30-100 UIElement/sek skapas och kastas → GC Gen0 var 2-3s, layout-pass på hela trädet.
**Åtgärd:** Två alternativ, välj per panel:

**Alt A — `ItemsControl` + `ObservableCollection`** (för dynamiska listor som CPU-cores):
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

**Alt B — En gång-byggd panel med namngivna TextBlocks** (för fasta paneler som Hardware):
- Bygg i XAML med `x:Name`-tilldelningar
- I code-behind: uppdatera bara `.Text`-värden

**DoD:** `dotMemory`-snapshot under körning visar <10 UIElement-allokeringar/sekund (ner från 100+).
**Estimat:** 8h

### T2.3 [P0] Pausa timer vid minimerat fönster
**Var:** `MainWindow.xaml.cs` — lägg till `StateChanged` event
**Varför:** Ingen anledning att polla sensorer när användaren inte tittar. Sparar batteri på laptops.
**Åtgärd:**
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
**DoD:** Minimera + Task Manager visar CPU-användning faller till ~0%. Återställ fönster → uppdatering fortsätter.
**Estimat:** 0.5h

### T2.4 [P1] Event-unsubscribe i OnClosed
**Var:** `MainWindow.xaml.cs:OnClosed`
**Varför:** `_timer.Tick += Timer_Tick` utan matchande `-=` håller `MainWindow` levande efter Close → klassisk WPF-läcka.
**Åtgärd:**
```csharp
protected override void OnClosed(EventArgs e)
{
    _timer.Stop();
    _timer.Tick -= Timer_Tick;
    StateChanged -= OnStateChanged; // om ovanstående flyttas till named method
    _computer?.Close();
    _computer = null;
    _tickGate?.Dispose();
    base.OnClosed(e);
}
```
**DoD:** `dotMemory` snapshot efter Close visar 0 instanser av MainWindow (tidigare 1+).
**Estimat:** 1h

### T2.5 [P1] Frikoppla Splash/MainWindow-init
**Var:** `App.xaml.cs` + `SplashWindow.xaml.cs:49-73` + `MainWindow.xaml.cs:InitializeCounters`
**Varför:** `new MainWindow()` kör `computer.Open()` + `Accept()` på UI-tråden (1-3s). Splash fryser.
**Åtgärd:**
1. Gör `MainWindow`-konstruktorn minimal: bara `InitializeComponent()` + field-init
2. Skapa `public async Task InitializeAsync()` som kör LHM-init på bakgrundstråd:
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
4. Ta bort den nestlade `Task.Run → InvokeAsync → Task.Run`-pyramiden i `SplashWindow.CloseSplash`.

**DoD:** Splash visas direkt (<200ms), stänger så snart init är klar. Ingen hårdkodad `Task.Delay(2000)`.
**Estimat:** 4h

### T2.6 [P1] Fixa %→RPM-heuristik
**Var:** `MainWindow.xaml.cs:781, 812, 818, 847`
**Varför:** Nuvarande `if (fanSpeed <= 100) fanSpeed *= 30f;` antar alla värden ≤100 är procent. Fel för zero-RPM-mode GPU:er och pumpsensorer.
**Åtgärd:** Använd `sensor.SensorType` korrekt:
```csharp
if (sensor.SensorType == SensorType.Fan)      // RPM, oförändrat
    displayValue = $"{value:0} RPM";
else if (sensor.SensorType == SensorType.Control) // Procent av max PWM
    displayValue = $"{value:0}%";
```
Separera RPM- och procent-sensorer i UI (två rader per fläkt om båda finns).
**DoD:** GPU i zero-RPM-idle visar "0 RPM" (inte "0%" omräknat till "0 RPM"). Pumpsensor visar korrekt RPM.
**Estimat:** 2h

### T2.7 [P2] Ta bort dubbel CPU-core-sampling
**Var:** `MainWindow.xaml.cs:229-231` + `_cpuCoreCounters`-fältet
**Varför:** Båda `PerformanceCounter.NextValue()` OCH LHM läser per-core-användning. Dubbel kostnad, potentiellt inkonsekventa värden.
**Åtgärd:** Välj en källa. Rekommendation: behåll LHM (enhetligt med resten av appen). Ta bort `_cpuCoreCounters`-field, loop i `InitializeCounters`, och allt användande.
**DoD:** `_cpuCoreCounters` finns inte längre. CPU-panel visar korrekta per-core-värden från LHM.
**Estimat:** 1h

### T2.8 [P2] Minska string-allokeringar i hot path
**Var:** `MainWindow.xaml.cs:340, 414` + värdeformattering
**Varför:** `Substring` + `$"{}"` i inner loops → många små string-allokeringar = GC-tryck.
**Åtgärd:**
- Skippa `Substring` när `Length ≤ 25` (ingen trunkering behövs)
- Använd `string.Create` eller pre-allokerad `StringBuilder` där format upprepas
- Cacha format-strängar som const
**DoD:** dotMemory-snapshot visar <200 string-allokeringar/sekund under polling (ner från 500+).
**Estimat:** 2h

### T2.9 [P2] Konfigurerbart polling-intervall
**Var:** `MainWindow.xaml.cs:126` + ny settings-backing
**Varför:** Hårdkodad 1s är för aggressivt i bakgrund, men användare med stora kylsystem vill ha 500ms.
**Åtgärd:** Lägg till field `_pollIntervalMs` med default 2000ms. Inställningsfil `%APPDATA%\SystemFlow Pro\settings.json` (JSON-serialisering via System.Text.Json). Settings-UI kommer i Sprint 4.
**DoD:** Ändring i settings.json → efter omstart används nytt intervall.
**Estimat:** 2h

### T2.10 [P3] Benchmarka före/efter
**Var:** Ny fil `docs/PERFORMANCE_NOTES.md`
**Varför:** Utan mätpunkter vet vi inte att optimeringar faktiskt hjälper.
**Åtgärd:** Mät:
- CPU-användning i Task Manager, medelvärde över 5 min (minimerad + fokuserad)
- Tick-kostnad via `Stopwatch` runt `CollectSystemSnapshot`
- GC Gen0/min via PerfView eller Visual Studio Diagnostics
Jämför mot Sprint 0-siffror (om tillgängligt) eller pre-Sprint-2 baseline.
**DoD:** Dokument med före/efter-tabell committad.
**Estimat:** 2h

---

## Risk & beroenden

- **T2.1** är sprintens största uppgift och beroende för T2.2. Om `CollectSystemSnapshot` + `ApplySnapshotToUI` arkitektur tar längre tid, skjut T2.8-T2.10 till backlog.
- **T2.2** kräver XAML-ändringar + troligen DataContext — om MVVM-grunden saknas (Sprint 3) kan Alt B (namngivna TextBlocks) användas som mellansteg.
- LibreHardwareMonitor är inte thread-safe — `_computer.Accept()` måste fortfarande vara **single-caller**. Semaphore i T2.1 garanterar det.

---

## Retro (fyll i efter sprint)
