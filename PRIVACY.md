# SystemFlow Pro — Sekretesspolicy

**Senast uppdaterad:** 2026-04-22 (Sprint 5)

SystemFlow Pro är en lokalkörd Windows-applikation som läser
hårdvaruinformation direkt från din dator. Vi samlar inte in, lagrar eller
överför någon personligt identifierbar information till externa servrar som
default.

## Vad programmet läser lokalt

SystemFlow Pro läser följande från din Windows-dator:

- **Hårdvaruinfo:** CPU-namn, antal kärnor, GPU-namn, total RAM-mängd
- **Sensorer:** CPU/GPU-belastning, temperaturer, fläkthastigheter
- **Systeminfo:** OS-version (t.ex. "Windows 11 build 22621"), aktuell användares
  inloggningsnamn (t.ex. "david") — visas i Hårdvaru Info-panelen
- **Processorräknare:** `% Processor Time`, `Available MBytes` (Windows Performance Counters)

All läsning sker **lokalt på din dator**. Inget av detta lämnar maskinen.

## Vad som lagras lokalt

Dessa filer skapas i `%APPDATA%\SystemFlow Pro\`:

| Fil | Innehåll | Syfte |
|-----|----------|-------|
| `settings.json` | Pollingintervall, temperaturenhet, "pausa vid minimering" | Användarinställningar |
| `logs/app-YYYY-MM-DD.log` | Diagnostik, fel, uppstartshändelser | Felsökning — visar inte känsliga värden som sensorutslag |

Loggfiler roteras automatiskt vid 5 MB (senaste 5 sparas). Du kan radera
mappen `%APPDATA%\SystemFlow Pro\` när som helst — programmet skapar den
igen vid nästa start.

## Vad som INTE samlas in eller skickas

- Inga telemetridata
- Inga analytics-pingar
- Ingen molnsynk
- Inga automatiska buggrapporter (default — se nedan)
- Ingen IP-adressinsamling
- Inga kakor / tracking

## Nätverksaktivitet

SystemFlow Pro gör **en** nätverksanslutning per appstart:

**Uppdateringskontroll** — en HTTPS-förfrågan till GitHub API
(`api.github.com/repos/screamm/SystemFlow_Pro/releases/latest`) för
att se om en nyare version finns tillgänglig. Endast HTTP User-Agent
("SystemFlow-Pro") och GitHub's standardlogg skapas. Ingen identifiering.

Du kan avstänga uppdateringskontrollen genom att sätta
`"CheckForUpdates": false` i `settings.json`.

## Crash-rapportering

**Opt-in endast.** Vid krasch visas en dialog som frågar om du vill skicka
crash-rapporten. Om du säger nej sparas rapporten bara lokalt i
`%APPDATA%\SystemFlow Pro\logs\`.

Om/när externa crash-rapporteringstjänster (t.ex. Sentry) aktiveras i
framtida versioner uppdateras detta dokument först och aktivering är
fortfarande opt-in.

## GDPR

Eftersom programmet inte samlar eller överför data till utvecklaren är
GDPR-relevansen minimal. Den användardata som existerar (`settings.json` +
loggfiler) lagras på din egen dator under din kontroll.

Du har full rätt att:
- Radera datan (ta bort `%APPDATA%\SystemFlow Pro\`)
- Inspektera datan (vanliga JSON + textfiler)
- Förhindra framtida lagring (ta bort mappen efter varje användning)

## Tredjepart

SystemFlow Pro använder LibreHardwareMonitor-biblioteket för
hårdvaruläsning. LibreHardwareMonitor kör lokalt och skickar ingen data.
Se `THIRD_PARTY_LICENSES.txt` för fullständig licenslista.

## Ändringar i denna policy

Ändringar publiceras i repot:
https://github.com/screamm/SystemFlow_Pro/blob/main/PRIVACY.md

## Kontakt

Frågor eller invändningar:
https://github.com/screamm/SystemFlow_Pro/issues
