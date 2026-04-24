# Sprint 4 — UI/UX modernization

**Goal:** Replace multicolored emoji with Fluent icons. Fix broken window chrome. Make the app accessible (WCAG level for desktop). Consolidate the color palette. Add Settings UI and About dialog. Elevate the visual impression from "2022 hobby dashboard" to "2026 Windows-native".

**Duration:** 2 weeks (~50-70h)
**Branch:** `sprint-04-ui-ux`
**Target version:** v1.1.0-beta.1
**Prerequisites:** Sprint 3 complete (MVVM in place, binding working)

---

## Sprint goal

- [ ] Zero multicolored emoji in XAML files
- [ ] Aero Snap, Snap Layouts (Win+Z), maximize double-click work
- [ ] MinWidth ≤ 1100, MinHeight ≤ 800 — fits on 1366×768
- [ ] All interactive controls have `AutomationProperties.Name` + ToolTip
- [ ] TabIndex on all buttons, FocusVisualStyle visible
- [ ] Color palette defined in one place (App.xaml), MainWindow uses StaticResource
- [ ] Settings dialog: polling interval, temperature unit, start minimized
- [ ] About dialog: version, license, GitHub link, third-party attribution
- [ ] Empty state when sensors are missing (not an empty panel)
- [ ] Tooltip on all hero cards ("CPU Package (Tctl) — threshold 95°C")

---

## Tasks

### T4.1 [P0] Replace multicolored emoji with Segoe Fluent Icons
**Where:** `MainWindow.xaml` lines 194, 230, 264, 298, 334, 339, 349, 354, 364, 377, 387, 390 + any others
**Why:** Violates global UI rules in `~/.claude/RULES.md`. Renders differently per Windows version, scales poorly at DPI 150%+.
**Action:**
```xml
<Style TargetType="TextBlock" x:Key="FluentIcon">
    <Setter Property="FontFamily" Value="Segoe Fluent Icons, Segoe MDL2 Assets"/>
    <Setter Property="FontSize" Value="18"/>
    <Setter Property="Foreground" Value="{StaticResource AccentBrush}"/>
</Style>
```
Replace each emoji with a glyph code (shown in "Character Map" or via fluenticons.co):
- `⚡` → `&#xE945;` (Lightning)
- `🎮` → `&#xE7FC;` (Game controller)
- `💾` → `&#xE105;` (Save, interpreted as RAM icon)
- `🌡️` → `&#xE9CA;` (Temperature / thermometer missing, use `&#xF152;`)
- `🔥` → `&#xE945;` or custom path
- `❄️` → `&#xE9CA;` (Snow)
- `🎯` → `&#xF272;` (Target)
- `⚙️` → `&#xE713;` (Settings)
- `🔧` → `&#xE90F;` (Repair)
**DoD:** `Grep` for unicode range `[\x{1F300}-\x{1FAFF}]` in .xaml files returns 0 hits.
**Estimate:** 4h

### T4.2 [P0] Fix window chrome (Aero Snap + custom titlebar)
**Where:** `MainWindow.xaml:1-50`
**Why:** `WindowStyle="None"` + `AllowsTransparency="True"` breaks Aero Snap, Snap Layouts, maximize double-click.
**Action:** Switch to the `WindowChrome` API which preserves Win32 functionality:
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
            <!-- Header: set WindowChrome.IsHitTestVisibleInChrome="True" on buttons -->
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
Remove `WindowStyle="None"` and `AllowsTransparency="True"`. Keep DropShadow via `WindowChrome.CornerRadius`.
**DoD:** Win+Up maximizes, Win+Down minimizes, drag to screen edge snaps, Win+Z shows Snap Layouts, double-click on the titlebar maximizes/restores.
**Estimate:** 6h

### T4.3 [P0] Reduce window dimensions + DPI-safe layout
**Where:** `MainWindow.xaml:4, 7-9`
**Why:** 1800×1300 + MinWidth 1400 gets clipped on 1080p. Hero card `Height="240"` gets clipped at 200% DPI.
**Action:**
- `Width="1400" Height="900" MinWidth="1100" MinHeight="750"`
- All `Height="X"` in cards → switch to `MinHeight="X"` or remove entirely
- Make the hero grid a `UniformGrid` with auto-sizing instead of fixed height
- Test at 125%, 150%, 200% DPI via Windows settings
**DoD:** The app fits on 1366×768. At 200% DPI no text is clipped.
**Estimate:** 3h

### T4.4 [P0] Consolidate color palette
**Where:** `App.xaml:6-17` and `MainWindow.xaml:14-25`
**Why:** Duplicate, conflicting palette definitions. App.xaml styles are never used.
**Action:**
1. Decide single source of truth → `App.xaml`
2. Move the MainWindow palette values there (those actually used)
3. Delete obsolete App.xaml styles (`ModernButton`, `HeaderText`, `ModernProgressBar` if unused)
4. MainWindow.xaml → no `<SolidColorBrush x:Key=...>` declarations, only `StaticResource`
**DoD:** `Grep` for `<SolidColorBrush` in MainWindow.xaml = 0 hits.
**Estimate:** 2h

### T4.5 [P0] Accessibility: AutomationProperties + TabIndex + FocusVisual
**Where:** `MainWindow.xaml` — all interactive controls and data readouts
**Why:** Narrator says "button" without context. TAB does nothing meaningful. Keyboard users are blocked.
**Action:**
```xml
<!-- Buttons in chrome -->
<Button Content="&#xE921;"
        AutomationProperties.Name="Minimize window"
        ToolTip="Minimize (Win+Down)"
        TabIndex="101"/>

<!-- Data readouts -->
<TextBlock Text="{Binding CpuUsageDisplay}"
           AutomationProperties.Name="CPU load"
           AutomationProperties.LiveSetting="Polite"/>

<!-- FocusVisualStyle in App.xaml -->
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
- Narrator (Win+Ctrl+Enter) reads meaningful names on all controls
- TAB through the window hits buttons in logical order
- Focused control has a visible outline
**Estimate:** 5h

### T4.6 [P0] Settings dialog
**Where:** New file `Views/SettingsWindow.xaml(.cs)` + `ViewModels/SettingsViewModel.cs`
**Why:** Polling interval and °C/°F should be user-configurable.
**Action:**
- ModalWindow, `Owner = MainWindow`, `WindowStartupLocation="CenterOwner"`
- Fields:
  - Polling interval: ComboBox (500ms, 1s, 2s, 5s)
  - Temperature unit: ToggleButton (°C / °F)
  - Pause when minimized: Checkbox
  - Start minimized: Checkbox
- "Save" saves via `ISettingsService`, closes the dialog
- "Cancel" closes without saving
- Opened from a gear button in the MainWindow header (new button)
**DoD:** Changing the polling interval takes effect immediately after save (does not require restart).
**Estimate:** 6h

### T4.7 [P1] About dialog
**Where:** New file `Views/AboutWindow.xaml(.cs)`
**Action:**
- The app icon large, app name, version (read from `Assembly.GetExecutingAssembly().GetName().Version`)
- Build date (via linker timestamp or embedded constant)
- Links: GitHub repo, report a bug, license
- Section "Third-party libraries":
  - LibreHardwareMonitor — MPL 2.0 — link
  - .NET 9 — MIT — link
- "OK" button
**DoD:** Opens from the info button in the header. License and GitHub links functional.
**Estimate:** 3h

### T4.8 [P1] Empty states + error UI
**Where:** `MainWindow.xaml` — all panels that can be empty
**Why:** When LHM does not find sensors (e.g. on older CPUs or without admin for MSR) an empty card is shown. Confuses users.
**Action:** Add a `DataTrigger` or new `IValueConverter` `CollectionEmptyToVisibility`:
```xml
<StackPanel Visibility="{Binding CpuCores.Count, Converter={StaticResource EmptyToVisibility}}">
    <TextBlock Text="No CPU core sensors available"
               Foreground="{StaticResource TextMutedBrush}"/>
    <TextBlock Text="Try starting the app as administrator for more sensors."
               FontSize="11" Foreground="{StaticResource TextMutedBrush}"/>
</StackPanel>
```
Do the same for fan, thermal, and GPU panels.
**DoD:** When `CpuCores.Count == 0` a friendly message is shown instead of empty space.
**Estimate:** 3h

### T4.9 [P1] Splash: progress + timeout
**Where:** `SplashWindow.xaml(.cs)` + `App.xaml.cs`
**Why:** "Initializing hardware monitor..." without progress. If LHM hangs, the splash stays forever.
**Action:**
- Add a `ProgressBar` with `IsIndeterminate="True"` or stepwise progress
- Stepwise messages: "Opening sensor API...", "Reading CPU configuration...", "Reading GPU...", "Loading settings..."
- 30s timeout in `InitializeAsync` — if init not complete: show error message "Could not start hardware monitoring. Continue anyway?" with "Continue" + "Cancel"
**DoD:** No path where the splash gets stuck without feedback.
**Estimate:** 3h

### T4.10 [P1] Tooltips on hero cards
**Where:** `MainWindow.xaml` — all hero cards
**Action:** Hover on "45°C" → tooltip "CPU Package (Tctl) · Warning at 85°C · Critical at 95°C". Hover on "GPU 67%" → "Used for 3D graphics and compute".
**DoD:** All 4 hero cards have meaningful tooltips.
**Estimate:** 2h

### T4.11 [P2] Live badge → last updated timestamp
**Where:** `MainWindow.xaml:150-158`
**Why:** A pulsating "LIVE" conveys no information. A timestamp is more useful.
**Action:** `<TextBlock Text="{Binding LastUpdateDisplay}" />` — format: "Updated: 21:14:32". Updated every tick.
**DoD:** The timestamp ticks every second.
**Estimate:** 1h

### T4.12 [P2] WCAG contrast: raise muted text
**Where:** `App.xaml` — `TextMutedBrush`
**Action:** `#94A3B8` → `#A8B2C0` on `#191B23` background. Ratio increases from ~4.9:1 to ~5.5:1. Raise minimum font size for muted text to 12pt.
**DoD:** WebAIM contrast checker approves AA for "normal text" throughout.
**Estimate:** 1h

### T4.13 [P2] Mica/Acrylic option (Windows 11)
**Where:** `MainWindow.xaml(.cs)` — runtime detection
**Action:** On Windows 11+ activate Mica via DWM interop:
```csharp
// After SourceInitialized
DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref micaValue, sizeof(int));
```
Fallback to flat background on Windows 10.
**DoD:** On Windows 11 a transparent/blurry background is shown that mirrors the desktop (Mica).
**Estimate:** 4h

### T4.14 [P3] Screenshot update
**Where:** `screenshot.png` in root
**Action:** Take a new screenshot of the UI after redesign. Update README.
**DoD:** New screenshot committed.
**Estimate:** 0.5h

---

## Risk & dependencies

- **T4.2 (WindowChrome)** may require adjustment of shadows/radius that depended on `AllowsTransparency`. Expect 1-2 extra hours for fine-tuning.
- **T4.6 (Settings)** requires that `SettingsService` from Sprint 3 works — verify before starting.
- **T4.13 (Mica)** is nice-to-have and Windows 11-specific. Defer if time is short.
- Fluent Icons glyph codes may vary between Windows versions → test on both Win10 and Win11.

---

## Retrospective (fill in after sprint)
