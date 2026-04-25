# SystemFlow Pro

**Lightweight and fast system monitoring for Windows — CPU, GPU, memory, temperatures, and fans in real time.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-0078d4?style=flat-square&logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-00d084?style=flat-square)](LICENSE)

---

## Overview

SystemFlow Pro is an open source system monitor for Windows built with
.NET 9 + WPF. It displays per-core CPU load, GPU status, memory usage,
temperature sensors, and fan speeds in real time. Designed to be a
faster, lighter alternative to HWiNFO64 and other large
monitoring tools.

![SystemFlow Pro](screenshot.png)

---

## Installation

### Option 1 — Download the prebuilt exe (recommended)

1. Go to [GitHub Releases](https://github.com/screamm/SystemFlow_Pro/releases/latest)
2. Download `SystemFlow-Pro-vX.Y.Z-win-x64.zip`
3. Extract and run `SystemFlow-Pro.exe` — no installation, no .NET
   runtime installation required (self-contained single-file build)

The first time you run it, Windows SmartScreen may warn "Windows Defender
protected your PC". Click "More info" → "Run anyway". SystemFlow Pro is open source and
distributed unsigned — all code can be reviewed on GitHub. The warning diminishes
over time as the app builds SmartScreen reputation through downloads.

### Option 2 — Build from source

Requirements: .NET 9 SDK (9.0.305 or newer, see `global.json`).

```bash
git clone https://github.com/screamm/SystemFlow_Pro.git
cd SystemFlow_Pro
dotnet restore
dotnet run --project SystemMonitorApp.csproj
```

To create a self-contained distribution:

```bash
build.bat 1.1.0
```

This produces `publish\v1.1.0\SystemFlow-Pro.exe` and `releases\SystemFlow-Pro-v1.1.0-win-x64.zip`.

### Administrator privileges

The app requires administrator privileges (`requireAdministrator` in the manifest).
The Windows UAC prompt appears at every startup. This is necessary to read
MSR registers (CPU temperature on many modern CPUs), fan sensors via
SuperIO chips, and certain GPU sensors that are otherwise protected.

Without admin, most sensors would return "N/A" on modern systems —
so we chose admin-by-default rather than a degraded experience.

---

## Features

| Category | Details |
|----------|---------|
| CPU | Total load, per-core, temperature, Package power |
| GPU | NVIDIA / AMD / Intel load, temperature, VRAM |
| Memory | Total / used / available GB, percent, progress bar |
| Temperatures | All available sensors with color coding |
| Fans | CPU / GPU / chassis / pump — RPM or PWM %, properly distinguished |
| System | OS version, CPU name, core count, active user |
| Settings | Polling interval, °C/°F, pause on minimize, start minimized |
| Diagnostics | File logger in `%APPDATA%\SystemFlow Pro\logs\` |

---

## Keyboard shortcuts

| Gesture | Function |
|---------|----------|
| Win+↑ | Maximize |
| Win+↓ | Minimize / restore |
| Win+Z | Snap Layouts (Windows 11) |
| Alt+F4 | Close |
| Tab | Cycle focus through chrome buttons |

---

## Technical information

- **Framework:** .NET 9, WPF
- **Hardware reading:** LibreHardwareMonitor 0.9.3 (MPL 2.0) + NVAPI for GPU
- **Architecture:** MVVM-lite with an extracted service layer
- **Testing:** 40 xUnit tests (status logic, OS name mapping, FanReading model)
- **Threading:** Background thread for all hardware reading, UI updates via
  snapshot pattern
- **Polling:** 2 seconds default (500ms–60s configurable)
- **Startup time:** ~6-8 seconds (LibreHardwareMonitor enumeration is the main cost)

### What works on every Windows 11 PC

These data sources are **motherboard-agnostic** and work without any BIOS changes:

| Source | Used for | API |
|--------|----------|-----|
| CPU MSR (DTS) | Per-core temperature, package temp, package power | LibreHardwareMonitor |
| NVIDIA NVAPI | GPU fan RPM, GPU temperature, hotspot, memory, power | LibreHardwareMonitor |
| AMD ADL | AMD GPU equivalents | LibreHardwareMonitor |
| Performance Counters | CPU load per core, available memory | Win32 PerfCounter |
| WMI | CPU name, total RAM, OS version | System.Management |
| S.M.A.R.T. | Storage temperatures | LibreHardwareMonitor |

### What requires BIOS configuration

CPU and chassis fan RPM through SuperIO chips (IT8688E, NCT6798D, etc.)
requires the BIOS to release PWM control to the OS. See the **Fan RPM is missing**
section under Troubleshooting below.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for layer diagrams and
design decisions.

---

## Troubleshooting

**The app does not start — "Windows protected your PC" / SmartScreen**
Right-click the .exe → Properties → check "Unblock" → OK. Or in the warning dialog,
click "More info" → "Run anyway". SystemFlow Pro is distributed unsigned as
open source — SmartScreen does not recognize the publisher until the app has
enough downloads. The code can be reviewed on GitHub.

**Fan RPM is missing (CPU/chassis fans)**
This is the most common issue. The motherboard's BIOS often locks the SuperIO
chip when fan headers are set to "Auto" or "Normal" mode, blocking software
from reading RPM. The fix lives in BIOS, not the app:

| Vendor | BIOS menu | Setting |
|--------|-----------|---------|
| Gigabyte | Smart Fan 5 | Set each fan header to "Manual" or "Full Speed" |
| ASUS | Q-Fan Control | Set to "Manual"; disable Q-Fan Tuning |
| MSI | Fan Xpert / Smart Fan Mode | Set to "Customize" or off |
| ASRock | A-Tuning / Fan Control | Usually open by default |

After saving and rebooting, restart SystemFlow Pro — fan RPM will appear
automatically in the **CPU COOLING** panel.

GPU fans in zero-RPM mode correctly display "Zero-RPM Mode" and do not
require any BIOS change — they just spin up automatically when the GPU
crosses its temperature threshold (~55-60°C).

**Temperatures missing**
Older CPUs have limited LibreHardwareMonitor support. See
`%APPDATA%\SystemFlow Pro\logs\hardware-report-*.txt` for details — it
lists every sensor LHM detected on your system.

**The app freezes or is slow**
Increase the polling interval in Settings (⚙ in the header) to 5 seconds. Please
send the log file to a [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues)
if the problem persists.

---

## Privacy

SystemFlow Pro does not collect or send any personal data. The only
network call at startup is a version check against the GitHub Releases API.
See [`PRIVACY.md`](PRIVACY.md) for details.

---

## Contributing & feedback

- Bugs, feature requests: [GitHub Issues](https://github.com/screamm/SystemFlow_Pro/issues)
- Discussion: [GitHub Discussions](https://github.com/screamm/SystemFlow_Pro/discussions)
- Pull requests welcome — see `docs/ARCHITECTURE.md` before major changes

---

## License

MIT License — see [`LICENSE`](LICENSE).

Third-party libraries (in particular LibreHardwareMonitor MPL 2.0) are listed in
[`THIRD_PARTY_LICENSES.txt`](THIRD_PARTY_LICENSES.txt).

---

**Developed by David Rydgren · [@screamm](https://github.com/screamm)**
