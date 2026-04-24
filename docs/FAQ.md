# SystemFlow Pro — Frequently Asked Questions (FAQ)

A list of common questions and answers. If your question is not here, please open
a [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues).

## Installation & startup

### Windows SmartScreen warns "Windows protected your PC"

This is because the .exe is not signed with a paid code-signing certificate.
Click "More info" → "Run anyway".

SystemFlow Pro is distributed unsigned as open source — code signing is
not planned. Certificates cost 200-500 USD/year and the project is free,
open, and can be reviewed line by line on
[GitHub](https://github.com/screamm/SystemFlow_Pro) instead.

The SmartScreen warning gradually diminishes as more users choose "Run anyway"
and Microsoft builds up reputation based on the hash signature of each built exe.

### Does the app require the .NET runtime?

No. It is distributed as a self-contained single file — the entire .NET runtime is
bundled into the exe. No installation required.

If you build it yourself with `dotnet build`, you need the .NET 9 SDK.

### Do I need to run as administrator?

No. The app runs as `asInvoker` since v1.0.9. Some hardware sensors
(particularly MSR readings on certain CPUs) require admin for full detail,
but most values work without. If a sensor is missing or shows "N/A",
try right-clicking the .exe → "Run as administrator".

### Where is the app installed?

Nowhere. Move the .exe wherever you like (e.g., `C:\Users\You\Programs\SystemFlow Pro\`)
and create a shortcut. It is a portable application.

User settings and logs are stored in `%APPDATA%\SystemFlow Pro\`.

## Data & values

### Why does my CPU show 0°C?

Not all CPUs have temperature sensors that LibreHardwareMonitor can read.
In particular, older Intel CPUs (10th gen or earlier) have limited support.
Try starting the app as administrator — some CPUs require it for
temperature access.

See `%APPDATA%\SystemFlow Pro\logs\app-{date}.log` for details about
sensor enumeration.

### GPU load shows "N/A"

- Hybrid graphics (laptop with iGPU + dGPU): the dGPU may be fully powered down when
  not in use → no sensor
- Drivers not installed or outdated
- The GPU driver does not expose a load sensor

### Fans are displayed as "%"

This is a PWM percentage (Pulse Width Modulation) from the motherboard's controller.
It is a percentage of max PWM, not a percentage of max RPM. The motherboard does not
report RPM for this fan, only duty cycle.

If the fan also has an RPM sensor, both rows are displayed. In Settings you will be able to filter
(future version).

### "Zero RPM Mode" on the GPU fan

Modern GPUs (NVIDIA RTX 20+, AMD RX 6000+) stop the fans when the
temperature is below ~55-60°C for quieter operation. This is normal and good —
no bearing cycles spent when not needed. When the GPU heats up, the fans
start automatically.

### The RPM values do not match BIOS

Possible causes:
- The motherboard reports pulses/revolution incorrectly (older motherboards)
- The fan does not have a 3-pin RPM wire (2-pin or 4-pin without pulse)
- The motherboard multiplies/divides RPM in an unknown way

Compare with BIOS in real time to determine which source is correct.

### Memory usage differs from Task Manager

SystemFlow Pro shows "% total" based on `Available MBytes` from Windows
Performance Counters. Task Manager may show different values depending on which
tab (Processes vs Performance) and whether "Compressed" is included.

The difference is normally 1-3%.

## Settings

### Where are my settings stored?

`%APPDATA%\SystemFlow Pro\settings.json`. Open in a text editor for manual
editing if needed.

### How do I change the polling interval?

Click the gear icon (⚙) in the upper right corner → choose the desired interval →
Save. The change applies immediately, no restart required.

### Can I start the app minimized?

Yes — in Settings, check "Start minimized". The app opens in the taskbar on
the next start.

### Can I use °F instead of °C?

Yes — in Settings, choose Fahrenheit. (Note: °F conversion is being completed in an upcoming
release.)

## Performance

### How much CPU does the app use?

Default polling (2 seconds): <3% continuously on modern machines.
At 500ms polling: 5-8%.
When minimized: ~0% (timer paused).

If you see higher usage, please report a bug with
the `%APPDATA%\SystemFlow Pro\logs\` log.

### The app freezes / lags

Possible causes:
- LibreHardwareMonitor finds a broken sensor that returns slowly
- WMI query hangs (but we have a 2s timeout — should not happen)
- Your machine has many (50+) sensors and rendering is slow

Workaround: raise the polling interval to 5 seconds in Settings.

Please report the bug with logs.

## Errors & crashes

### The app crashed — what do I do?

1. Check `%APPDATA%\SystemFlow Pro\logs\app-{date}.log` — the stack trace is there
2. Open a [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues)
3. Attach: OS version, CPU, GPU, the log, what you were doing before the crash

### "Hardware information unavailable"

WMI may be corrupt. Try:
1. Start `cmd` as administrator
2. Run `winmgmt /verifyrepository`
3. If "repository is not consistent": `winmgmt /resetrepository`
4. Restart the computer

Alternatively — SystemFlow Pro should still work, only the hardware panel may be empty.

## Privacy & security

### Does the app send data anywhere?

No — apart from an update check against the GitHub Releases API (at every start,
no data about you is sent, only an HTTP GET). See [`PRIVACY.md`](../PRIVACY.md).

### Can I run without internet?

Yes. The update check fails silently. All other functionality is local.

### Is there telemetry?

No. No user data is collected or sent.

## Development

### Where is the source code?

https://github.com/screamm/SystemFlow_Pro

### Can I contribute?

Yes — pull requests are welcome. Read `docs/ARCHITECTURE.md` first to understand
layers and design decisions.

### Is there a roadmap?

See GitHub Issues tagged `enhancement`. Major items on the backlog:
- Multi-language (English as default, Swedish as alternative)
- Export metrics to CSV
- History mode (charts over time)
- Widget mode (always-on-top mini)
- Mica on Windows 11

## Uninstallation

### How do I uninstall?

Portable app — just delete the folder. Also remove `%APPDATA%\SystemFlow Pro\`
if you want to clean up settings and logs. No registry entries are created.
