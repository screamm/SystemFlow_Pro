# SystemFlow Pro — Vanliga frågor (FAQ)

Lista över vanliga frågor och svar. Om din fråga inte finns här, öppna gärna
en [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues).

## Installation & start

### Windows SmartScreen varnar "Windows har skyddat din dator"

Det är för att .exe:n inte är signerad med ett betalt kodsignerings-certifikat.
Klicka "Mer info" → "Kör ändå".

SystemFlow Pro distribueras osignerat som öppen källkod — kodsignering är
inte planerat. Certifikat kostar 200-500 USD/år och projektet är gratis,
öppet, och kan granskas rad för rad på
[GitHub](https://github.com/screamm/SystemFlow_Pro) istället.

SmartScreen-varningen minskar gradvis när fler användare väljer "Kör ändå"
och Microsoft bygger upp rykte baserat på hash-signatur av varje byggd exe.

### Kräver appen .NET-runtime?

Nej. Den distribueras som self-contained single-file — all .NET-runtime är
inbakad i exe:n. Ingen installation krävs.

Om du bygger själv med `dotnet build` behöver du .NET 9 SDK.

### Behöver jag köra som administratör?

Nej. Appen körs som `asInvoker` sedan v1.0.9. Vissa hårdvarusensorer
(särskilt MSR-läsningar på vissa CPU:er) kräver admin för full detaljnivå,
men de flesta värden fungerar utan. Om en sensor saknas eller visar "N/A",
prova att högerklicka .exe → "Kör som administratör".

### Var installeras appen?

Ingenstans. Flytta .exe:n vart du vill (t.ex. `C:\Users\Dig\Program\SystemFlow Pro\`)
och skapa en genväg. Det är en portabel applikation.

Användarinställningar och loggar lagras i `%APPDATA%\SystemFlow Pro\`.

## Data & värden

### Varför visar min CPU 0°C?

Inte alla CPU:er har temperatursensorer som LibreHardwareMonitor kan läsa.
Särskilt äldre Intel CPU:er (10th gen eller tidigare) har begränsat stöd.
Prova att starta appen som administratör — vissa CPU:er kräver det för
temperaturåtkomst.

Se `%APPDATA%\SystemFlow Pro\logs\app-{datum}.log` för detaljer om
sensor-uppräkningen.

### GPU-belastning visar "N/A"

- Hybrid-grafik (laptop med iGPU + dGPU): dGPU kan vara helt avstängd när
  den inte används → ingen sensor
- Drivrutiner inte installerade eller föråldrade
- GPU-drivrutinen exponerar inte load-sensor

### Fläktar visas som "%"

Det är en PWM-procent (Pulse Width Modulation) från moderkortets controller.
Det är procent av max-PWM, inte procent av max-RPM. Moderkortet rapporterar
inte RPM för denna fläkt, bara duty cycle.

Om fläkten också har en RPM-sensor visas båda rader. I Settings kan du filtrera
(framtida version).

### "Zero RPM Mode" på GPU-fläkten

Moderna GPU:er (NVIDIA RTX 20+, AMD RX 6000+) stannar fläktarna när
temperaturen är under ~55-60°C för tystare drift. Detta är normalt och bra —
inga cykler på lager om det inte behövs. När GPU:n värms upp startar
fläktarna automatiskt.

### RPM-värdena matchar inte BIOS

Möjliga orsaker:
- Moderkortet rapporterar pulser/varv felaktigt (gamla moderkort)
- Fläkten har inte 3-pin RPM-tråd (2-pin eller 4-pin utan pulse)
- Moderkortet multiplicerar/delar RPM på okänt sätt

Jämför med BIOS i realtid för att fastställa vilken källa som är korrekt.

### Minnesanvändningen skiljer sig från Task Manager

SystemFlow Pro visar "% total" baserat på `Available MBytes` från Windows
Performance Counters. Task Manager kan visa olika värden beroende på vilken
flik (Processes vs Performance) och om "Compressed" räknas in.

Skillnaden är normalt 1-3%.

## Inställningar

### Var lagras mina inställningar?

`%APPDATA%\SystemFlow Pro\settings.json`. Öppna i en text-editor för manuell
redigering om behövs.

### Hur ändrar jag pollingintervallet?

Klicka kugghjulsikonen (⚙) i övre högra hörnet → välj önskat intervall →
Spara. Ändringen gäller direkt, ingen omstart behövs.

### Kan jag starta appen minimerad?

Ja — i Settings, bocka för "Starta minimerad". Appen öppnas i taskbar vid
nästa start.

### Kan jag använda °F istället för °C?

Ja — i Settings, välj Fahrenheit. (Note: °F-konvertering slutförs i en kommande
release.)

## Prestanda

### Hur mycket CPU använder appen?

Default polling (2 sekunder): <3% kontinuerligt på moderna maskiner.
Vid 500ms polling: 5-8%.
När minimerad: ~0% (timer pausad).

Om du ser högre användning, rapportera gärna en bugg med
`%APPDATA%\SystemFlow Pro\logs\`-loggen.

### Appen fryser / laggar

Möjliga orsaker:
- LibreHardwareMonitor hittar en trasig sensor som returnerar långsamt
- WMI-query hänger (men vi har 2s timeout — bör inte ske)
- Din maskin har många (50+) sensorer och rendering är långsam

Workaround: höj pollingintervallet till 5 sekunder i Settings.

Rapportera gärna buggen med loggar.

## Fel & krascher

### Appen kraschade — vad gör jag?

1. Kolla `%APPDATA%\SystemFlow Pro\logs\app-{datum}.log` — stack trace ligger där
2. Öppna en [GitHub Issue](https://github.com/screamm/SystemFlow_Pro/issues)
3. Bifoga: OS-version, CPU, GPU, loggen, vad du gjorde före kraschen

### "Hårdvaruinformation ej tillgänglig"

WMI kan vara korrupt. Prova:
1. Starta `cmd` som administratör
2. Kör `winmgmt /verifyrepository`
3. Om "repository is not consistent": `winmgmt /resetrepository`
4. Starta om datorn

Alternativt — SystemFlow Pro bör ändå fungera, bara hårdvarupanel kan vara tom.

## Sekretess & säkerhet

### Skickar appen data någonstans?

Nej — förutom en update-check mot GitHub Releases API (vid varje start,
ingen data om dig skickas, bara HTTP GET). Se [`PRIVACY.md`](../PRIVACY.md).

### Kan jag köra utan internet?

Ja. Update-check misslyckas tyst. All övrig funktionalitet är lokal.

### Finns telemetri?

Nej. Ingen användardata samlas eller skickas.

## Utveckling

### Var är källkoden?

https://github.com/screamm/SystemFlow_Pro

### Kan jag bidra?

Ja — PR-requests välkomnas. Läs `docs/ARCHITECTURE.md` först för att förstå
lager och designbeslut.

### Finns en roadmap?

Se GitHub Issues med tag `enhancement`. Stora punkter på backlog:
- Multi-språk (engelska som default, svenska som alternativ)
- Export av metrics till CSV
- Historik-läge (diagram över tid)
- Widget-läge (always-on-top mini)
- Mica på Windows 11

## Avinstallation

### Hur avinstallerar jag?

Portabel app — bara radera mappen. Ta också bort `%APPDATA%\SystemFlow Pro\`
om du vill rensa inställningar och loggar. Inga registry-entries skapas.
