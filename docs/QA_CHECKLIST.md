# SystemFlow Pro — QA-checklista

**Gäller version:** v1.1.0-rc.1 → v1.1.0
**Ansvar:** Alla punkter godkända på minst 3 hårdvarukonfigurationer innan
v1.1.0-tag pushas till `main`.

## Hårdvara att testa

**Obligatoriskt (minst 1 per grupp):**

- [ ] **Modern AMD** — Ryzen 7000-serie eller nyare
- [ ] **Modern Intel** — 12th/13th/14th gen (E-cores + P-cores)
- [ ] **Windows 10 22H2** — senaste Win10-versionen
- [ ] **Windows 11 23H2 eller 24H2** — senaste Win11-versionen
- [ ] **Ett NVIDIA GPU-system** (RTX 30/40-serie eller nyare)

**Valfritt men värdefullt:**

- [ ] AMD Radeon GPU
- [ ] Äldre Intel CPU (10th gen eller tidigare) — testar fallback
- [ ] Äldre AMD CPU (5000-serie eller tidigare)
- [ ] Laptop med hybrid-grafik (iGPU + dGPU)
- [ ] Desktop med AIO-kylare (pumpsensor)
- [ ] Desktop med många chassifläktar (5+)

## Test per konfiguration

### A. Installation & start

- [ ] Ladda ner `SystemFlow-Pro-v1.1.0-rc.1-win-x64.zip`
- [ ] Packa upp till ny mapp
- [ ] SmartScreen-varning visas (förväntat — appen distribueras osignerat som
      öppen källkod) — "Mer info" → "Kör ändå" fungerar
- [ ] Appen startar utan admin-prompt
- [ ] Splash visas i 0.8–2 sekunder
- [ ] Huvudfönstret öppnas centrerat på primär skärm
- [ ] Inga felmeddelanden vid start

### B. Dataflöde & korrekthet

- [ ] CPU-värde (hero-kort) matchar Task Manager inom ±5%
- [ ] GPU-värde matchar MSI Afterburner eller Task Manager inom ±5%
- [ ] RAM-värde matchar Task Manager inom ±0.5 GB
- [ ] Temperaturer visas med rimliga värden (inga 0°C, inga >120°C utom kritiska)
- [ ] CPU-kärnor visas per kärna (upp till 16)
- [ ] Kärnanvändning färgkodas korrekt (<60% accent, 60-80% warn, >80% error)
- [ ] Termalpaneler visar minst CPU + GPU temperatur
- [ ] Fläkt-RPM visas korrekt (jämför med BIOS eller fan controller)
- [ ] Zero-RPM GPU-fläkt visar "Zero RPM Mode" (inte "0 RPM" med fel färg)
- [ ] Om admin ej körs: fläktpanelerna nämner "Administratörsbehörighet: Nej"
- [ ] Hårdvaru-Info panelen visar korrekt CPU-namn, kärnantal, OS-version

### C. UI / UX

- [ ] Alla 4 hero-kort renderar utan klippning på 1920×1080
- [ ] Fönstret ryms på 1366×768 (minimum-stöd)
- [ ] WindowChrome: dubbelklick på titelbar maximerar/återställer
- [ ] Win+↑ maximerar, Win+↓ minimerar
- [ ] Win+Z visar Snap Layouts (Windows 11)
- [ ] Drag till skärmkant snappar (Aero Snap)
- [ ] Settings-ikonen (⚙) öppnar Settings-dialogen
- [ ] Info-ikonen (🛈) öppnar About-dialogen
- [ ] Minimera/Maximera/Stäng-knappar fungerar
- [ ] Hover på hero-kort visar tooltips
- [ ] Inga multicolored emoji synliga i UI
- [ ] Font-rendering ser skarp ut på 100%, 125%, 150%, 200% DPI

### D. Accessibility

- [ ] Narrator (Win+Ctrl+Enter) läser "CPU belastning aktuellt värde" på CPU-kortet
- [ ] Narrator läser "Minimera fönster" på min-knappen
- [ ] Tab cyklar genom Settings → About → Minimize → Maximize → Close i den ordningen
- [ ] Fokuserad knapp har synlig fokus-outline (streckad accent-färg)
- [ ] High-contrast-läge bryter inte layout (Windows-inställning "Enable high contrast")

### E. Settings-dialog

- [ ] Öppnar centrerat över huvudfönstret
- [ ] Default-värden: 2000ms, Celsius, pausa=Ja, starta-minimerad=Nej
- [ ] Välj 500ms → Spara → tickar ökar till dubbelt så fort
- [ ] Välj 5000ms → Spara → tickar saktar ner
- [ ] Välj °F → Spara → (visuell verifiering — Sprint 4 gör detta i rendering)
- [ ] Avbryt ändrar ingenting
- [ ] Settings.json skrivs till `%APPDATA%\SystemFlow Pro\settings.json`

### F. About-dialog

- [ ] Visar korrekt version "v1.1.0-rc.1" eller nyare
- [ ] Visar bygge-datum (inte 1970-01-01 / placeholder)
- [ ] GitHub-länk öppnar webbläsare
- [ ] Issue-länk öppnar webbläsare
- [ ] Licenstext synlig
- [ ] OK-knapp stänger dialogen

### G. Livslängd & prestanda

- [ ] Kontinuerlig körning 30 min utan krasch
- [ ] CPU-användning enligt Task Manager: <5% under polling
- [ ] Minne (i Task Manager för `SystemFlow-Pro.exe`): ökar inte över 30 min
      (ingen läcka)
- [ ] Minimera → CPU-användning faller till ~0% (timer pausad)
- [ ] Återställ → tickar återupptas inom 1-2 sekunder
- [ ] Stäng (×) → processen försvinner från Task Manager inom 2 sekunder

### H. Robusthet

- [ ] Dra ur USB-enhet mitt under polling — ingen krasch
- [ ] Flytta fönstret över två skärmar — rendering förblir korrekt
- [ ] Byt DPI under körning (om möjligt) — layout rekomposerar rimligt
- [ ] Kör samtidigt som annan hårdvaruövervakare (HWiNFO64 / MSI Afterburner) —
      ingen konflikt
- [ ] Stäng internet → appen kör fortfarande (bara update-check misslyckas tyst)

### I. Loggar

- [ ] `%APPDATA%\SystemFlow Pro\logs\app-{datum}.log` skapas
- [ ] Innehåller startup-rad med version + OS
- [ ] Vid testvis framkallad krasch: stack trace skrivs
- [ ] Rotation fungerar när filen överskrider 5 MB (simulera via fil-storlek)

### J. Avinstallation

- [ ] Radera programmappen → processen avslutas rent
- [ ] Radera `%APPDATA%\SystemFlow Pro\` → inga spår kvar i registry eller
      andra platser

## Regression-fokus (från review-kommentarer)

Dessa var nya i v1.0.9+ — verifiera extra noga:

- [ ] Admin-prompt försvinner (tidigare `requireAdministrator`)
- [ ] Ingen UI-frysning vid polling (Sprint 2 flyttade till bakgrundstråd)
- [ ] Panel-flimmer borta (Sprint 2 cachade TextBlocks)
- [ ] Aero Snap fungerar igen (Sprint 4 WindowChrome)
- [ ] Fläktar visar rätt enhet — RPM vs % (Sprint 2 heuristik-fix)

## Buggrapportering

Rapportera varje fel som en GitHub Issue med:
- Hårdvarukonfiguration (CPU, GPU, RAM, moderkort)
- OS-version
- Steg för att reproducera
- Innehåll från `%APPDATA%\SystemFlow Pro\logs\app-{datum}.log`
- Skärmdump om relevant

## Godkänd-dokumentation

När alla punkter är godkända, skriv en rad per konfiguration:

```
- [PASS] Ryzen 7 7700X + RTX 4070 + Win11 23H2 — testad 2026-05-XX av David
- [PASS] Intel i7-13700K + RTX 3080 + Win10 22H2 — testad 2026-05-XX av Testare A
- [PASS] Ryzen 5 5600 + RX 6700 XT + Win11 24H2 — testad 2026-05-XX av Testare B
```

Tre PASS krävs för grönt ljus till v1.1.0.
