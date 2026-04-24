# SystemFlow Pro — QA Checklist

**Applies to version:** v1.1.0-rc.1 → v1.1.0
**Responsibility:** All items must pass on at least 3 hardware configurations before
the v1.1.0 tag is pushed to `main`.

## Hardware to test

**Mandatory (at least 1 per group):**

- [ ] **Modern AMD** — Ryzen 7000 series or newer
- [ ] **Modern Intel** — 12th/13th/14th gen (E-cores + P-cores)
- [ ] **Windows 10 22H2** — latest Win10 version
- [ ] **Windows 11 23H2 or 24H2** — latest Win11 version
- [ ] **An NVIDIA GPU system** (RTX 30/40 series or newer)

**Optional but valuable:**

- [ ] AMD Radeon GPU
- [ ] Older Intel CPU (10th gen or earlier) — tests fallback
- [ ] Older AMD CPU (5000 series or earlier)
- [ ] Laptop with hybrid graphics (iGPU + dGPU)
- [ ] Desktop with AIO cooler (pump sensor)
- [ ] Desktop with many chassis fans (5+)

## Test per configuration

### A. Installation & startup

- [ ] Download `SystemFlow-Pro-v1.1.0-rc.1-win-x64.zip`
- [ ] Extract to a new folder
- [ ] SmartScreen warning appears (expected — the app is distributed unsigned as
      open source) — "More info" → "Run anyway" works
- [ ] The app starts without an admin prompt
- [ ] Splash shows for 0.8–2 seconds
- [ ] The main window opens centered on the primary screen
- [ ] No error messages at startup

### B. Data flow & correctness

- [ ] CPU value (hero card) matches Task Manager within ±5%
- [ ] GPU value matches MSI Afterburner or Task Manager within ±5%
- [ ] RAM value matches Task Manager within ±0.5 GB
- [ ] Temperatures display reasonable values (no 0°C, no >120°C except critical)
- [ ] CPU cores displayed per core (up to 16)
- [ ] Core usage color-coded correctly (<60% accent, 60-80% warn, >80% error)
- [ ] Thermal panels show at least CPU + GPU temperature
- [ ] Fan RPM displayed correctly (compare with BIOS or fan controller)
- [ ] Zero-RPM GPU fan shows "Zero RPM Mode" (not "0 RPM" with the wrong color)
- [ ] If not running as admin: the fan panels mention "Administrator privileges: No"
- [ ] The Hardware Info panel shows the correct CPU name, core count, OS version

### C. UI / UX

- [ ] All 4 hero cards render without clipping at 1920×1080
- [ ] The window fits on 1366×768 (minimum supported)
- [ ] WindowChrome: double-clicking the title bar maximizes/restores
- [ ] Win+↑ maximizes, Win+↓ minimizes
- [ ] Win+Z shows Snap Layouts (Windows 11)
- [ ] Drag to screen edge snaps (Aero Snap)
- [ ] The Settings icon (⚙) opens the Settings dialog
- [ ] The Info icon (🛈) opens the About dialog
- [ ] Minimize/Maximize/Close buttons work
- [ ] Hover on hero cards shows tooltips
- [ ] No multicolored emoji visible in the UI
- [ ] Font rendering looks sharp at 100%, 125%, 150%, 200% DPI

### D. Accessibility

- [ ] Narrator (Win+Ctrl+Enter) reads "CPU load current value" on the CPU card
- [ ] Narrator reads "Minimize window" on the minimize button
- [ ] Tab cycles through Settings → About → Minimize → Maximize → Close in that order
- [ ] Focused button has a visible focus outline (dashed accent color)
- [ ] High-contrast mode does not break the layout (Windows setting "Enable high contrast")

### E. Settings dialog

- [ ] Opens centered over the main window
- [ ] Default values: 2000ms, Celsius, pause=Yes, start-minimized=No
- [ ] Select 500ms → Save → ticks speed up to twice as fast
- [ ] Select 5000ms → Save → ticks slow down
- [ ] Select °F → Save → (visual verification — Sprint 4 does this in rendering)
- [ ] Cancel changes nothing
- [ ] Settings.json is written to `%APPDATA%\SystemFlow Pro\settings.json`

### F. About dialog

- [ ] Shows the correct version "v1.1.0-rc.1" or newer
- [ ] Shows the build date (not 1970-01-01 / placeholder)
- [ ] GitHub link opens the browser
- [ ] Issue link opens the browser
- [ ] License text visible
- [ ] OK button closes the dialog

### G. Longevity & performance

- [ ] Continuous run for 30 min without a crash
- [ ] CPU usage per Task Manager: <5% during polling
- [ ] Memory (in Task Manager for `SystemFlow-Pro.exe`): does not grow over 30 min
      (no leak)
- [ ] Minimize → CPU usage drops to ~0% (timer paused)
- [ ] Restore → ticks resume within 1-2 seconds
- [ ] Close (×) → the process disappears from Task Manager within 2 seconds

### H. Robustness

- [ ] Unplug a USB device mid-polling — no crash
- [ ] Move the window across two screens — rendering remains correct
- [ ] Change DPI during runtime (if possible) — layout recomposes reasonably
- [ ] Run simultaneously with another hardware monitor (HWiNFO64 / MSI Afterburner) —
      no conflict
- [ ] Disable internet → the app still runs (only the update check fails silently)

### I. Logs

- [ ] `%APPDATA%\SystemFlow Pro\logs\app-{date}.log` is created
- [ ] Contains a startup row with version + OS
- [ ] On a test-triggered crash: stack trace is written
- [ ] Rotation works when the file exceeds 5 MB (simulate via file size)

### J. Uninstallation

- [ ] Delete the program folder → the process exits cleanly
- [ ] Delete `%APPDATA%\SystemFlow Pro\` → no traces left in the registry or
      other locations

## Regression focus (from review comments)

These were new in v1.0.9+ — verify extra carefully:

- [ ] Admin prompt is gone (previously `requireAdministrator`)
- [ ] No UI freeze during polling (Sprint 2 moved it to a background thread)
- [ ] Panel flicker gone (Sprint 2 cached TextBlocks)
- [ ] Aero Snap works again (Sprint 4 WindowChrome)
- [ ] Fans show the correct unit — RPM vs % (Sprint 2 heuristic fix)

## Bug reporting

Report every defect as a GitHub Issue with:
- Hardware configuration (CPU, GPU, RAM, motherboard)
- OS version
- Steps to reproduce
- Content from `%APPDATA%\SystemFlow Pro\logs\app-{date}.log`
- Screenshot if relevant

## Approval documentation

When all items pass, write one row per configuration:

```
- [PASS] Ryzen 7 7700X + RTX 4070 + Win11 23H2 — tested 2026-05-XX by David
- [PASS] Intel i7-13700K + RTX 3080 + Win10 22H2 — tested 2026-05-XX by Tester A
- [PASS] Ryzen 5 5600 + RX 6700 XT + Win11 24H2 — tested 2026-05-XX by Tester B
```

Three PASSes are required for the green light to v1.1.0.
