# Sprint 4 — UI/UX modernisering

**Mål:** Ersätt multicolored emoji med Fluent-ikoner. Fixa trasig window chrome. Gör appen tillgänglig (WCAG-nivå för desktop). Konsolidera färgpalett. Lägg till Settings-UI och About-dialog. Höj det visuella intrycket från "2022 hobby-dashboard" till "2026 Windows-native".

**Varaktighet:** 2 veckor (~50-70h)
**Branch:** `sprint-04-ui-ux`
**Målversion:** v1.1.0-beta.1
**Förutsättningar:** Sprint 3 klar (MVVM på plats, binding fungerar)

---

## Sprintmål

- [ ] Noll multicolored emoji i XAML-filer
- [ ] Aero Snap, Snap Layouts (Win+Z), maximize-double-click fungerar
- [ ] MinWidth ≤ 1100, MinHeight ≤ 800 — ryms på 1366×768
- [ ] Alla interaktiva kontroller har `AutomationProperties.Name` + ToolTip
- [ ] TabIndex på alla knappar, FocusVisualStyle synlig
- [ ] Färgpalett definierad på ett ställe (App.xaml), MainWindow använder StaticResource
- [ ] Settings-dialog: polling-intervall, temperatur-enhet, start-minimerad
- [ ] About-dialog: version, licens, GitHub-länk, tredjepart-attribution
- [ ] Empty state när sensorer saknas (inte tom panel)
- [ ] Tooltip på alla hero-kort ("CPU Package (Tctl) — tröskel 95°C")

---

## Tasks

### T4.1 [P0] Byt multicolored emoji mot Segoe Fluent Icons
**Var:** `MainWindow.xaml` rader 194, 230, 264, 298, 334, 339, 349, 354, 364, 377, 387, 390 + eventuella andra
**Varför:** Bryter mot globala UI-regler i `~/.claude/RULES.md`. Renderas olika per Windows-version, skalar dåligt i DPI 150%+.
**Åtgärd:**
```xml
<Style TargetType="TextBlock" x:Key="FluentIcon">
    <Setter Property="FontFamily" Value="Segoe Fluent Icons, Segoe MDL2 Assets"/>
    <Setter Property="FontSize" Value="18"/>
    <Setter Property="Foreground" Value="{StaticResource AccentBrush}"/>
</Style>
```
Ersätt varje emoji med glyph-kod (visas i "Character Map" eller via fluenticons.co):
- `⚡` → `&#xE945;` (Lightning)
- `🎮` → `&#xE7FC;` (Game controller)
- `💾` → `&#xE105;` (Save, tolkas som RAM-ikon)
- `🌡️` → `&#xE9CA;` (Temperature / thermometer saknas, använd `&#xF152;`)
- `🔥` → `&#xE945;` eller custom path
- `❄️` → `&#xE9CA;` (Snow)
- `🎯` → `&#xF272;` (Target)
- `⚙️` → `&#xE713;` (Settings)
- `🔧` → `&#xE90F;` (Repair)
**DoD:** `Grep` på unicode-range `[\x{1F300}-\x{1FAFF}]` i .xaml-filer returnerar 0 träffar.
**Estimat:** 4h

### T4.2 [P0] Fix window chrome (Aero Snap + custom titlebar)
**Var:** `MainWindow.xaml:1-50`
**Varför:** `WindowStyle="None"` + `AllowsTransparency="True"` bryter Aero Snap, Snap Layouts, maximize-double-click.
**Åtgärd:** Byt till `WindowChrome`-API som bevarar Win32-funktioner:
```xml
<Window ...>
    <WindowChrome.WindowChrome>
        <WindowChrome
            CaptionHeight="48"
            GlassFrameThickness="0"
            ResizeBorderThickness="6"
            UseAeroCaptionButtons="False"
            CornerRadius="8"/>
    </WindowChrome.WindowChrome>
    <Border Background="{StaticResource BackgroundBrush}" CornerRadius="8">
        <Grid>
            <!-- Header: sätt WindowChrome.IsHitTestVisibleInChrome="True" på knappar -->
            <Grid Height="48" VerticalAlignment="Top">
                <TextBlock Text="SystemFlow Pro" Margin="16,0,0,0"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
                            WindowChrome.IsHitTestVisibleInChrome="True">
                    <Button Content="&#xE921;" Click="Minimize_Click"/> <!-- Chrome min -->
                    <Button Content="&#xE922;" Click="Maximize_Click"/> <!-- Chrome max -->
                    <Button Content="&#xE8BB;" Click="Close_Click"/>    <!-- Chrome close -->
                </StackPanel>
            </Grid>
            <!-- content -->
        </Grid>
    </Border>
</Window>
```
Ta bort `WindowStyle="None"` och `AllowsTransparency="True"`. Behåll DropShadow via `WindowChrome.CornerRadius`.
**DoD:** Win+Up maximerar, Win+Down minimerar, drag till skärmkant snappar, Win+Z visar Snap Layouts, dubbelklick på titelbaren maximerar/återställer.
**Estimat:** 6h

### T4.3 [P0] Minska fönsterdimensioner + DPI-säker layout
**Var:** `MainWindow.xaml:4, 7-9`
**Varför:** 1800×1300 + MinWidth 1400 klipps på 1080p. Hero-kort `Height="240"` klipps på 200% DPI.
**Åtgärd:**
- `Width="1400" Height="900" MinWidth="1100" MinHeight="750"`
- Alla `Height="X"` i kort → byt till `MinHeight="X"` eller ta bort helt
- Gör hero-grid `UniformGrid` med auto-sizing istället för fast höjd
- Testa på 125%, 150%, 200% DPI via Windows-inställningar
**DoD:** Appen ryms på 1366×768. Vid 200% DPI klipps ingen text.
**Estimat:** 3h

### T4.4 [P0] Konsolidera färgpalett
**Var:** `App.xaml:6-17` och `MainWindow.xaml:14-25`
**Varför:** Dubbla, motstridiga palettdefinitioner. App.xaml-styles används aldrig.
**Åtgärd:**
1. Bestäm single source of truth → `App.xaml`
2. Flytta MainWindow-palettens värden dit (de som faktiskt används)
3. Radera obsoleta App.xaml-styles (`ModernButton`, `HeaderText`, `ModernProgressBar` om ej använda)
4. MainWindow.xaml → inga `<SolidColorBrush x:Key=...>`-deklarationer, bara `StaticResource`
**DoD:** `Grep` efter `<SolidColorBrush` i MainWindow.xaml = 0 träffar.
**Estimat:** 2h

### T4.5 [P0] Accessibility: AutomationProperties + TabIndex + FocusVisual
**Var:** `MainWindow.xaml` — alla interaktiva kontroller och data-readouts
**Varför:** Narrator säger "button" utan kontext. TAB gör inget meningsfullt. Tangentbordsanvändare blockeras.
**Åtgärd:**
```xml
<!-- Knappar i chrome -->
<Button Content="&#xE921;"
        AutomationProperties.Name="Minimera fönster"
        ToolTip="Minimera (Win+Down)"
        TabIndex="101"/>

<!-- Data-readouts -->
<TextBlock Text="{Binding CpuUsageDisplay}"
           AutomationProperties.Name="CPU-belastning"
           AutomationProperties.LiveSetting="Polite"/>

<!-- FocusVisualStyle i App.xaml -->
<Style x:Key="AccessibleFocus" TargetType="Control">
    <Setter Property="FocusVisualStyle">
        <Setter.Value>
            <Style>
                <Setter Property="Control.Template">
                    <Setter.Value>
                        <ControlTemplate>
                            <Rectangle StrokeThickness="2" Stroke="{StaticResource AccentBrush}"
                                       StrokeDashArray="1 2" Margin="-2"/>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
        </Setter.Value>
    </Setter>
</Style>
```
**DoD:**
- Narrator (Win+Ctrl+Enter) läser meningsfulla namn på alla kontroller
- TAB genom fönstret träffar knappar i logisk ordning
- Fokuserad kontroll har synlig outline
**Estimat:** 5h

### T4.6 [P0] Settings-dialog
**Var:** Ny fil `Views/SettingsWindow.xaml(.cs)` + `ViewModels/SettingsViewModel.cs`
**Varför:** Polling-intervall och °C/°F ska vara användarkonfigurerbart.
**Åtgärd:**
- ModalWindow, `Owner = MainWindow`, `WindowStartupLocation="CenterOwner"`
- Fält:
  - Pollingintervall: ComboBox (500ms, 1s, 2s, 5s)
  - Temperaturenhet: ToggleButton (°C / °F)
  - Pausa när minimerad: Checkbox
  - Start minimerad: Checkbox
- "Spara" sparar via `ISettingsService`, stänger dialogen
- "Avbryt" stänger utan att spara
- Öppnas från kugghjulsknapp i MainWindow-headern (ny knapp)
**DoD:** Ändring av polling-intervall tar effekt direkt efter save (inte kräver omstart).
**Estimat:** 6h

### T4.7 [P1] About-dialog
**Var:** Ny fil `Views/AboutWindow.xaml(.cs)`
**Åtgärd:**
- Appikonen stor, appnamn, version (läs från `Assembly.GetExecutingAssembly().GetName().Version`)
- Byggdatum (via linker timestamp eller embedded constant)
- Länk: GitHub-repo, rapportera bug, licens
- Avsnitt "Tredjepartsbibliotek":
  - LibreHardwareMonitor — MPL 2.0 — länk
  - .NET 9 — MIT — länk
- "OK"-knapp
**DoD:** Öppnas från info-knapp i header. Licens- och GitHub-länkar funktionella.
**Estimat:** 3h

### T4.8 [P1] Empty states + fel-UI
**Var:** `MainWindow.xaml` — alla paneler som kan vara tomma
**Varför:** När LHM inte hittar sensorer (t.ex. på gamla CPU:er eller utan admin för MSR) visas tomt kort. Förvirrar användare.
**Åtgärd:** Lägg till `DataTrigger` eller ny `IValueConverter` `CollectionEmptyToVisibility`:
```xml
<StackPanel Visibility="{Binding CpuCores.Count, Converter={StaticResource EmptyToVisibility}}">
    <TextBlock Text="Inga CPU-kärnsensorer tillgängliga"
               Foreground="{StaticResource TextMutedBrush}"/>
    <TextBlock Text="Prova att starta appen som administratör för fler sensorer."
               FontSize="11" Foreground="{StaticResource TextMutedBrush}"/>
</StackPanel>
```
Gör samma för fläkt-, termal- och GPU-paneler.
**DoD:** När `CpuCores.Count == 0` visas vänligt meddelande istället för tom yta.
**Estimat:** 3h

### T4.9 [P1] Splash: progress + timeout
**Var:** `SplashWindow.xaml(.cs)` + `App.xaml.cs`
**Varför:** "Initialiserar hårdvaruövervakare..." utan progress. Om LHM hänger står splash för evigt.
**Åtgärd:**
- Lägg till `ProgressBar` med `IsIndeterminate="True"` eller stegvis progress
- Stegvisa meddelanden: "Öppnar sensor-API...", "Läser CPU-konfiguration...", "Läser GPU...", "Laddar inställningar..."
- 30s timeout i `InitializeAsync` — om init inte klar: visa felmeddelande "Kunde inte starta hårdvaruövervakning. Fortsätt ändå?" med "Fortsätt" + "Avbryt"
**DoD:** Ingen väg där splash fastnar utan feedback.
**Estimat:** 3h

### T4.10 [P1] Tooltips på hero-kort
**Var:** `MainWindow.xaml` — alla hero-kort
**Åtgärd:** Hover på "45°C" → tooltip "CPU Package (Tctl) · Varning vid 85°C · Kritisk vid 95°C". Hover på "GPU 67%" → "Används för 3D-grafik och compute".
**DoD:** Alla 4 hero-kort har meningsfulla tooltips.
**Estimat:** 2h

### T4.11 [P2] Live-badge → senast uppdaterad timestamp
**Var:** `MainWindow.xaml:150-158`
**Varför:** Pulserande "LIVE" förmedlar ingen info. Timestamp är mer användbart.
**Åtgärd:** `<TextBlock Text="{Binding LastUpdateDisplay}" />` — format: "Uppdaterad: 21:14:32". Uppdateras varje tick.
**DoD:** Timestamp ticker sekundvis.
**Estimat:** 1h

### T4.12 [P2] WCAG-kontrast: höj muted-text
**Var:** `App.xaml` — `TextMutedBrush`
**Åtgärd:** `#94A3B8` → `#A8B2C0` på `#191B23`-bakgrund. Ratio ökar från ~4.9:1 till ~5.5:1. Höj minsta font-storlek för muted-text till 12pt.
**DoD:** WebAIM contrast checker godkänner AA för "normal text" överallt.
**Estimat:** 1h

### T4.13 [P2] Mica/Acrylic-option (Windows 11)
**Var:** `MainWindow.xaml(.cs)` — runtime detection
**Åtgärd:** På Windows 11+ aktivera Mica via DWM-interop:
```csharp
// Efter SourceInitialized
DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref miсaValue, sizeof(int));
```
Fallback till platt bakgrund på Windows 10.
**DoD:** På Windows 11 visas transparent/blurrig bakgrund som speglar desktop (Mica).
**Estimat:** 4h

### T4.14 [P3] Screenshot-uppdatering
**Var:** `screenshot.png` i rot
**Åtgärd:** Ta ny screenshot av UI:t efter redesign. Uppdatera README.
**DoD:** Ny screenshot committad.
**Estimat:** 0.5h

---

## Risk & beroenden

- **T4.2 (WindowChrome)** kan kräva justering av skuggor/radius som var beroende av `AllowsTransparency`. Räkna med 1-2 timmar extra för finjustering.
- **T4.6 (Settings)** kräver att `SettingsService` från Sprint 3 fungerar — verifiera innan start.
- **T4.13 (Mica)** är nice-to-have och Windows 11-specifikt. Skjut om tid saknas.
- Fluent Icons glyph-koder kan variera mellan Windows-versioner → testa på både Win10 och Win11.

---

## Retro (fyll i efter sprint)
