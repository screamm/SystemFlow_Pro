# Sprint 1 — Stabilisering & säkerhet

**Mål:** Eliminera alla aktiva buggar som kan krascha appen eller maskera produktionsfel. Ta bort onödiga admin-krav. Lägg grund för diagnostik.

**Varaktighet:** 1 vecka (~30-40h solo deltid)
**Branch:** `sprint-01-stabilisering`
**Målversion:** v1.0.9

**Utgångsläge:** v1.0.8.1 — appen fungerar men sväljer exceptions tyst, har deadlock-risk, kräver onödig admin, och saknar crash-logging.

---

## Sprintmål (Definition of Success)

- [ ] Appen startar utan admin-rättigheter (asInvoker) med graceful degradation
- [ ] `.Result` eliminerat från UI-tråden
- [ ] `DispatcherUnhandledException` fångar och loggar crashes till fil
- [ ] Alla 13 tomma `catch {}` ersatta med strukturerad logging
- [ ] Kod kompilerar utan CS1998-varningar (async utan await)
- [ ] `<Nullable>enable</Nullable>` aktiverat, nya varningar åtgärdade
- [ ] Död kod borttagen (`_gpuCounter`, `EstimateFanSpeedFromTemperature`)

---

## Tasks

### T1.1 [P0] Introducera enkel fil-logger
**Var:** Ny fil `Services/Logger.cs`
**Varför:** Alla följande tasks förutsätter en logger. Utan den byts tomma catch-block mot `Debug.WriteLine` som inte hjälper användare i fält.
**Åtgärd:**
- Skapa statisk `Logger`-klass som skriver till `%APPDATA%\SystemFlow Pro\logs\app-{yyyy-MM-dd}.log`
- Metoder: `Info(string)`, `Warn(string, Exception?)`, `Error(string, Exception?)`
- Automatisk rotation vid >5 MB, behåll senaste 5 filer
- Thread-safe via `lock` eller `ConcurrentQueue` + background flush
**DoD:** Kan kalla `Logger.Error("test", ex)` från MainWindow och verifiera att loggfil skapas.
**Estimat:** 3h

### T1.2 [P0] Unhandled exception handler
**Var:** `App.xaml.cs:OnStartup`
**Varför:** Appen kraschar tyst idag. Utan detta vet du inte att den kraschar hos användare.
**Åtgärd:**
```csharp
DispatcherUnhandledException += (s, e) => {
    Logger.Error("Unhandled UI exception", e.Exception);
    MessageBox.Show($"Ett fel uppstod. Loggen har sparats i %APPDATA%\\SystemFlow Pro\\logs.\n\n{e.Exception.Message}",
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
**DoD:** Kasta testvis en exception i en knapp-handler → fönster visas, logg skrivs, appen lever.
**Estimat:** 1h

### T1.3 [P0] Fixa `.Result` på UI-tråden
**Var:** `MainWindow.xaml.cs:271` (`UpdateMemoryPanel`)
**Varför:** `GetTotalMemoryGB().Result` är deadlock-risk och blockerar UI 20-200ms.
**Åtgärd:** Totalt RAM ändras aldrig under körning. Läs in **en gång** i `InitializeCounters()` och cacha som field `private float _totalMemoryGB`. Ta bort `.Result`-anropet.
**DoD:** `Grep` på `\.Result` i projektet returnerar 0 träffar. UI-rendering av minnespanel tar <5ms.
**Estimat:** 1h

### T1.4 [P0] Konsolidera `computer.Accept()` till en gång per tick
**Var:** `MainWindow.xaml.cs:626, 660, 694, 756, 1005, 1072`
**Varför:** Hårdvaruträdet traverseras 4-7 ggr/sek. Race conditions i LibreHardwareMonitor. Ny `UpdateVisitor` allokeras varje anrop.
**Åtgärd:**
- Deklarera `private static readonly UpdateVisitor _visitor = new();`
- Lägg till `_computer.Accept(_visitor);` **en gång** först i `UpdateSystemData`
- Ta bort alla andra `computer.Accept(new UpdateVisitor())`-anrop i getters
- Ta även bort redundanta `hardware.Update()` / `subHardware.Update()` i inre loopar
**DoD:** Sökning på `Accept(` och `.Update()` i MainWindow.xaml.cs visar max 1 Accept-anrop + inga inre Update-loopar.
**Estimat:** 2h

### T1.5 [P0] Ersätt alla tomma `catch {}` med strukturerad logging
**Var:** `MainWindow.xaml.cs` rader 185, 216, 251, 282, 311, 363, 509, 559, 588, 1106, 1138, 1210 (13 st totalt)
**Varför:** Produktionsfel försvinner spårlöst.
**Åtgärd:** För varje:
```csharp
catch (Exception ex)
{
    Logger.Warn($"Failed in {nameof(MethodName)}: {ex.Message}", ex);
    // returnera säkert default-värde
}
```
- Sväljer fortfarande exception men lämnar spår
- Vissa bör eskalera till användare (kritiska sensorer som inte hittas) — markera med TODO + user-friendly fallback-UI
**DoD:** `Grep` på `catch\s*\{\s*\}` i .cs-filer returnerar 0 träffar. `catch (Exception)` utan `ex` — 0 träffar.
**Estimat:** 3h

### T1.6 [P0] `requireAdministrator` → `asInvoker`
**Var:** `app.manifest:19`
**Varför:** Ingen kodväg kräver admin. Blockerar icke-admin-användare och ökar angreppsyta vid kompromettering.
**Åtgärd:**
```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```
Testa: starta som vanlig användare. Sensorer som kräver admin (vissa MSR-läsningar i LHM) kommer returnera null — bekräfta att UI degraderar till "N/A" utan crash.
Lägg till en synlig indikator i headern: "Kör som admin för fler sensorer" om `!IsRunningAsAdmin()`.
**DoD:** Appen startar och kör utan UAC-prompt. Headerbadge visar admin-status korrekt.
**Estimat:** 2h

### T1.7 [P1] Aktivera modern C# och nullable
**Var:** `SystemMonitorApp.csproj`
**Varför:** .NET 9 utan `<Nullable>` är slöseri. Fångar null-bugs vid kompilering.
**Åtgärd:**
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
<WarningsAsErrors>CS8600;CS8602;CS8603;CS8625;CS1998</WarningsAsErrors>
```
Åtgärda nya warnings en i taget. Antal kan vara 30-80 beroende på kodbas.
**DoD:** Build utan warnings. Alla null-referenses tydligt markerade (`?` eller `!`).
**Estimat:** 6-8h

### T1.8 [P1] Fixa `async` utan `await`
**Var:** `MainWindow.xaml.cs:600, 620, 654, 686, 1063`
**Varför:** CS1998. Metoderna är inte asynkrona — de blockerar UI-tråden.
**Åtgärd:**
- För metoder som läser WMI/LHM: `await Task.Run(() => ...)` runt CPU-tungt arbete
- För metoder som inte behöver vara async: ta bort `async` och returnera värdet direkt
- Se Sprint 2 för full trådning — detta är "minimum viable" fix
**DoD:** Inga CS1998 varningar. Alla `async` metoder har minst ett `await`.
**Estimat:** 3h

### T1.9 [P1] WMI-timeouts
**Var:** `MainWindow.xaml.cs:604, 904, 926, 1119` (alla `ManagementObjectSearcher`)
**Varför:** WMI hänger oändligt på trasiga system.
**Åtgärd:**
```csharp
var options = new EnumerationOptions
{
    Timeout = TimeSpan.FromSeconds(2),
    ReturnImmediately = false
};
using var searcher = new ManagementObjectSearcher(null, query, options);
```
**DoD:** Alla `ManagementObjectSearcher`-anrop har timeout <= 2s.
**Estimat:** 1h

### T1.10 [P2] Ta bort död kod
**Var:**
- `_gpuCounter` field (rad 21) + dispose (rad 1208) — aldrig tilldelad
- `_cpuCoreCounters` — används parallellt med LHM, dubbeljobb. Välj en (rekommendation: behåll LHM, ta bort PerformanceCounter)
- `EstimateFanSpeedFromTemperature` (rad 987) — aldrig anropad
**DoD:** Kompilator-varning om oanvänt field/method = 0.
**Estimat:** 1h

### T1.11 [P2] Stavfel och små UI-fixar
**Var:**
- `MainWindow.xaml.cs:534` "HÖRG BELASTNING" → "HÖG BELASTNING"
- `MainWindow.xaml:4` + `SplashWindow.xaml` — lägg till `Icon="app.ico"` (eller pack-URI)
**DoD:** Taskbar/Alt+Tab visar rätt ikon. Inga stavfel i visuella strängar.
**Estimat:** 0.5h

### T1.12 [P2] CHANGELOG för v1.0.9
**Var:** Ny fil `CHANGELOG_v1.0.9.md`
**Åtgärd:** Sammanfatta alla ändringar från Sprint 1. Följ befintligt format från v1.0.3-v1.0.5.
**DoD:** Filen existerar och nämner alla P0-tasks.
**Estimat:** 0.5h

---

## Risk & beroenden

- **T1.6** (asInvoker) kan avslöja sensorer som verkligen kräver admin → måste graceful-degrada till "N/A". Om många sensorer försvinner för icke-admin-användare: dokumentera i README + visa tooltip.
- **T1.7** (nullable) kan ta längre tid än estimerat om kodbasen använder null-retur som kontrollflöde.
- **T1.4** förutsätter att `UpdateSystemData` kör före alla getters — verifiera call graph.

---

## Retro (fyll i efter sprint)

- Vad gick bra:
- Vad tog längre tid än tänkt:
- Vad flyttades till Sprint 2:
