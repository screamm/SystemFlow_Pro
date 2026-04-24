# SystemFlow Pro — Privacy Policy

**Last updated:** 2026-04-22 (Sprint 5)

SystemFlow Pro is a locally executed Windows application that reads
hardware information directly from your computer. We do not collect, store, or
transmit any personally identifiable information to external servers by
default.

## What the program reads locally

SystemFlow Pro reads the following from your Windows computer:

- **Hardware info:** CPU name, number of cores, GPU name, total RAM amount
- **Sensors:** CPU/GPU load, temperatures, fan speeds
- **System info:** OS version (e.g., "Windows 11 build 22621"), the current user's
  login name (e.g., "david") — shown in the Hardware Info panel
- **Performance counters:** `% Processor Time`, `Available MBytes` (Windows Performance Counters)

All reading happens **locally on your computer**. None of this leaves the machine.

## What is stored locally

These files are created in `%APPDATA%\SystemFlow Pro\`:

| File | Content | Purpose |
|------|---------|---------|
| `settings.json` | Polling interval, temperature unit, "pause on minimize" | User settings |
| `logs/app-YYYY-MM-DD.log` | Diagnostics, errors, startup events | Troubleshooting — does not record sensitive values such as sensor readings |

Log files rotate automatically at 5 MB (the latest 5 are kept). You can delete
the folder `%APPDATA%\SystemFlow Pro\` at any time — the program recreates it
on the next start.

## What is NOT collected or sent

- No telemetry data
- No analytics pings
- No cloud sync
- No automatic bug reports (by default — see below)
- No IP address collection
- No cookies / tracking

## Network activity

SystemFlow Pro makes **one** network connection per app start:

**Update check** — an HTTPS request to the GitHub API
(`api.github.com/repos/screamm/SystemFlow_Pro/releases/latest`) to
check whether a newer version is available. Only the HTTP User-Agent
("SystemFlow-Pro") and GitHub's standard log entry are created. No identification.

You can disable the update check by setting
`"CheckForUpdates": false` in `settings.json`.

## Crash reporting

**Opt-in only.** When a crash occurs, a dialog asks whether you want to send
the crash report. If you decline, the report is only saved locally in
`%APPDATA%\SystemFlow Pro\logs\`.

If/when external crash reporting services (e.g., Sentry) are enabled in
future versions, this document will be updated first and enabling remains
opt-in.

## GDPR

Because the program does not collect or transmit data to the developer,
GDPR relevance is minimal. The user data that exists (`settings.json` +
log files) is stored on your own computer under your control.

You have the full right to:
- Delete the data (remove `%APPDATA%\SystemFlow Pro\`)
- Inspect the data (plain JSON + text files)
- Prevent future storage (remove the folder after each use)

## Third-party

SystemFlow Pro uses the LibreHardwareMonitor library for
hardware reading. LibreHardwareMonitor runs locally and sends no data.
See `THIRD_PARTY_LICENSES.txt` for the full license list.

## Changes to this policy

Changes are published in the repo:
https://github.com/screamm/SystemFlow_Pro/blob/main/PRIVACY.md

## Contact

Questions or objections:
https://github.com/screamm/SystemFlow_Pro/issues
