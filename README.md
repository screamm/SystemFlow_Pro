# SystemFlow Pro

**Lätt och snabb systemövervakning för Windows — CPU, GPU, minne, temperaturer och fläktar i realtid.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-0078d4?style=flat-square&logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-00d084?style=flat-square)](LICENSE)

---

## Översikt

SystemFlow Pro är en öppen källkod systemövervakare för Windows byggd i
.NET 9 + WPF. Visar CPU-belastning per kärna, GPU-status, minnesanvändning,
temperatursensorer och fläkthastigheter i realtid. Designad för att vara
ett snabbare, lättare alternativ till HWiNFO64 och andra stora
övervakningsverktyg.

![SystemFlow Pro](screenshot.png)

---

## Installation

### Alternativ 1 — Ladda ner färdig exe (rekommenderat)

1. Gå till [GitHub Releases](https://github.com/screamm/SystemFlow_Pro/releases/latest)
2. Ladda ner `SystemFlow-Pro-vX.Y.Z-win-x64.zip`
3. Packa upp och kör `SystemFlow-Pro.exe` — ingen installation, ingen .NET
   runtime-installation behövs (self-contained single-file build)

Första gången kan Windows SmartScreen varna "Windows Defender skyddade din
dator". Klicka "Mer info" → "Kör ändå". SystemFlow Pro är öppen källkod och
distribueras osignerat — all kod kan granskas på GitHub. Varningen minskar
med tiden när appen byggt upp SmartScreen-rykte via nedladdningar.

### Alternativ 2 — Bygg från källkod

Krav: .NET 9 SDK (9.0.305 eller nyare, se `global.json`).

```bash
git clone https://github.com/screamm/SystemFlow_Pro.git
cd SystemFlow_Pro
dotnet restore
dotnet run --project SystemMonitorApp.csproj
```

För att skapa en self-contained distribution:

```bash
build.bat 1.1.0
```

Ger `publish\v1.1.0\SystemFlow-Pro.exe` och `releases\SystemFlow-Pro-v1.1.0-win-x64.zip`.

### Administratörsbehörighet

Appen körs som `asInvoker` (vanliga användarrättigheter). Vissa sensorer —
främst MSR-läsningar på vissa CPU:er — kräver admin för full detaljnivå.
Appen visar "Administratörsbehörighet: Nej" i fläktpanelerna om du kör som
vanlig användare och relevanta sensorer inte är tillgängliga.

---

## Funktioner

| Kategori | Detaljer |
|----------|----------|
| CPU | Total belastning, per-kärna, temperatur, Package power |
| GPU | NVIDIA / AMD / Intel belastning, temperatur, VRAM |
| Minne | Total / använt / tillgängligt GB, procent, progressbar |
| Temperaturer | Alla tillgängliga sensorer med färgkodning |
| Fläktar | CPU / GPU / chassi / pump — RPM eller PWM-%, korrekt skilda |
| System | OS-version, CPU-namn, kärnantal, aktiv användare |
| Inställningar | Pollingintervall, °C/°F, pausa vid minimering |
| Diagnostik | Fil-logger i `%APPDATA%\SystemFlow Pro\logs\` |

---

## Tangentbordsgenvägar

| Gest | Funktion |
|------|----------|
| Win+↑ | Maximera |
| Win+↓ | Minimera / återställ |
| Win+Z | Snap Layouts (Windows 11) |
| Alt+F4 | Stäng |
| Tab | Cykla fokus genom chrome-knappar |

---

## Teknisk information

- **Ramverk:** .NET 9, WPF
- **Hårdvaruläsning:** LibreHardwareMonitor 0.9.4 (MPL 2.0)
- **Arkitektur:** MVVM-lite med extraherat service-lager
- **Testning:** 37 xUnit-tester (status-logik, OS-namnmappning, FanReading-modell)
- **Trådning:** Bakgrundstråd för all hårdvaruläsning, UI uppdateras via
  snapshot-mönster
- **Polling:** 2 sekunder default (500ms–60s konfigurerbart)

Se [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) för lagerdiagram och
designbeslut.

---

## Felsökning

**Appen startar inte — "Windows har skyddat din dator" / SmartScreen**
Höger-klicka .exe → Egenskaper → bocka "Tillåt" → OK. Eller i varningsdialogen,
klicka "Mer info" → "Kör ändå". SystemFlow Pro distribueras osignerat som
öppen källkod — SmartScreen känner inte igen utgivaren förrän appen fått
tillräckligt många nedladdningar. Koden kan granskas på GitHub.

**Inga fläktar visas**
Inte alla moderkort exponerar RPM via LHM/WMI. Starta appen som administratör
för fler sensorer. Vissa GPU-fläktar i zero-RPM-mode visar "0 RPM" korrekt.

**Temperaturer saknas**
Äldre CPU:er har begränsat LibreHardwareMonitor-stöd. Se
`%APPDATA%\SystemFlow Pro\logs\app-*.log` för detaljer.

**Appen fryser eller är långsam**
Öka pollingintervallet i Settings (⚙ i header) till 5 sekunder. Skicka
gärna logg-filen till en [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues)
om problemet kvarstår.

---

## Sekretess

SystemFlow Pro samlar inte in och skickar ingen personlig data. Uppstartens
enda nätverksanrop är en versionskoll mot GitHub Releases API.
Se [`PRIVACY.md`](PRIVACY.md) för detaljer.

---

## Bidrag & feedback

- Buggar, feature requests: [GitHub Issues](https://github.com/screamm/SystemFlow_Pro/issues)
- Diskussion: [GitHub Discussions](https://github.com/screamm/SystemFlow_Pro/discussions)
- Pull requests välkomnas — se `docs/ARCHITECTURE.md` innan större ändringar

---

## Licens

MIT License — se [`LICENSE`](LICENSE).

Tredjepartsbibliotek (särskilt LibreHardwareMonitor MPL 2.0) listas i
[`THIRD_PARTY_LICENSES.txt`](THIRD_PARTY_LICENSES.txt).

---

**Utvecklad av David Rydgren · [@screamm](https://github.com/screamm)**
