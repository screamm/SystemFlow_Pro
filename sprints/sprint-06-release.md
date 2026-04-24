# Sprint 6 — Release 1.1 & QA

**Mål:** Systematisk QA över hårdvara och Windows-versioner. Beta-release till utvalda testare. Samla feedback. Åtgärda regressioner. Publicera v1.1.0 som officiell stabil version (osignerad, öppen källkod).

**Varaktighet:** 1 vecka (~25-35h aktiv + beta-period om 3-5 dagar)
**Branch:** `sprint-06-release` → `main`
**Målversion:** v1.1.0 (Production)
**Förutsättningar:** Sprint 5 klar, CI grön, build producerar self-contained exe

---

## Sprintmål

- [ ] Manuell QA på minst 3 Windows-konfigurationer (Win10, Win11 Intel, Win11 AMD)
- [ ] Beta-release `v1.1.0-rc.1` till 5-10 testare
- [ ] Alla P0-buggar från beta åtgärdade
- [ ] Release-notes för v1.1.0 publicerade
- [ ] v1.1.0 publicerad på GitHub Releases
- [ ] Main branch taggad `v1.1.0`
- [ ] README badge uppdaterad
- [ ] Annonsering (om relevant — subreddits, Twitter, etc.)

---

## Tasks

### T6.1 [P0] Manuell QA-matris
**Var:** Ny fil `docs/QA_CHECKLIST.md` + genomförande
**Varför:** Automatiska tester täcker bara logik. Reala hårdvaruproblem måste testas manuellt.
**Åtgärd:** Checklista per konfiguration:

**Hårdvara att testa:**
- [ ] AMD Ryzen (7000-serie eller nyare)
- [ ] AMD Ryzen (5000-serie eller äldre — testar fallback)
- [ ] Intel 13th gen+ (E-cores + P-cores)
- [ ] Intel 10th gen eller äldre (äldre sensorer)
- [ ] NVIDIA RTX 30/40/50-serie
- [ ] AMD Radeon (om tillgänglig)
- [ ] Laptop med hybrid-grafik
- [ ] Desktop med flera fläktar / AIO-kylare / pumpsensorer

**Windows-versioner:**
- [ ] Windows 10 22H2 (senaste)
- [ ] Windows 11 23H2
- [ ] Windows 11 24H2 / 25H2
- [ ] Windows 11 ARM64 (om tid — kräver separat build)

**Per konfiguration, verifiera:**
- Appen startar utan fel (ingen admin-prompt, ingen SmartScreen-blockering)
- Splash visas kort, sen försvinner
- Alla hero-kort visar sensorisk data inom 3 sekunder
- CPU-usage-värde matchar Task Manager (inom ±5%)
- GPU-usage matchar MSI Afterburner / Task Manager
- Temperaturer är rimliga (ingen 0°C, ingen 200°C)
- RAM-användning matchar Task Manager
- Fläktar visar RPM-värden som stämmer (verifiera mot fan-controller/moderkortets BIOS)
- Zero-RPM-fläktar visar "0 RPM" utan felmeddelande
- Settings-dialog öppnar, sparar, ändrar polling-intervall
- About-dialog visar version 1.1.0
- Minimera → CPU-användning i Task Manager faller ~0%
- Återställ → uppdateringen fortsätter
- Stäng → processen försvinner från Task Manager inom 2s
- Kör i 30 min → ingen minnestillväxt (verifiera i Task Manager)
- Vid 150% DPI: ingen text klipps
- Alt-tab / Win+Up / Win+Down / Win+Z (Snap Layouts) fungerar
- Narrator (Win+Ctrl+Enter) läser meningsfulla namn

**DoD:** Alla punkter godkända på minst 3 konfigurationer. Buggar dokumenterade som GitHub Issues.
**Estimat:** 10-15h (inkl. test-setup)

### T6.2 [P0] Publicera beta `v1.1.0-rc.1`
**Var:** Git tag + GitHub Release
**Åtgärd:**
```bash
git tag v1.1.0-rc.1 -m "Release candidate 1 for v1.1.0"
git push origin v1.1.0-rc.1
```
GitHub Actions-workflow (från Sprint 5) bygger och publicerar. Markera som "Pre-release" i GitHub UI.
Skriv beta-meddelande:
```markdown
# v1.1.0-rc.1 — Beta release

Testar alla stora förbättringar från Sprint 1-5.
**Vill du hjälpa till?** Ladda ner, kör i en vecka, rapportera buggar i Issues.

## Vad är nytt sedan v1.0.8.1
- Nytt arkitekturlager (MVVM) — 60% mindre code-behind
- ~3% CPU-overhead (tidigare 8-15%)
- Self-contained distribution — ingen .NET-installation behövs
- Osignerade binärer, dokumenterat — SmartScreen-instruktion i README/FAQ
- Settings-dialog + About-dialog
- Tillgänglighet: skärmläsarstöd + tangentbordsnavigation
- Fluent-ikoner istället för emoji
- Aero Snap + Snap Layouts fungerar

## Kända risker
Beta-version — kan innehålla regressioner. Rapportera gärna.
```
**DoD:** GitHub Release publicerad, testare har tillgång.
**Estimat:** 2h

### T6.3 [P0] Rekrytera beta-testare
**Var:** Extern — sociala medier, bekanta, communities
**Åtgärd:** Kontakta 5-10 personer med varierad hårdvara. Dela release-länk + kort beskrivning av vad du vill ha feedback på. Skapa en enkel Google Form eller GitHub Issue template för rapporter.
**DoD:** Minst 5 bekräftade testare.
**Estimat:** 2h

### T6.4 [P0] Beta-period + feedback-insamling
**Var:** 3-5 dagar med beta ute
**Åtgärd:** Dagligen:
- Kolla GitHub Issues för nya rapporter
- Kolla Sentry-dashboard (om aktiverad) för automatiska crash-reports
- Svara på testare med bekräftelse / klarifikationsfrågor
- Prioritera buggar: krasch / fel data = P0, UX-friktion = P1, önskemål = P2
**DoD:** Alla rapporter triagerade, P0-buggar identifierade.
**Estimat:** 3-5 dagar passiv + 4-6h aktiv

### T6.5 [P0] Fixa regressioner från beta
**Var:** Berörda filer baserat på rapporter
**Åtgärd:** För varje P0-bugg:
1. Reproducera lokalt (eller be rapportör om mer info)
2. Fixa på ny feature-branch `fix/issue-NN`
3. Lägg till regressionstest om möjligt
4. Merga efter review
5. Publicera `v1.1.0-rc.2` (och rc.3 vid behov)
**DoD:** Noll öppna P0-buggar. Alla rc.2+ releaser testade av minst 2 testare.
**Estimat:** 8-15h (beroende på antal buggar)

### T6.6 [P1] Uppdatera dokumentation baserat på feedback
**Var:** `README.md`, `PRIVACY.md`, `docs/FAQ.md` (ny)
**Åtgärd:** Om flera testare frågar samma sak → lägg till i FAQ. Vanliga exempel:
- "Varför visas 0°C på min CPU?" → sensor-stöd per CPU-generation
- "Varför blockerar antivirus appen?" → SmartScreen + false positives
- "Hur avinstallerar jag?" → self-contained, bara radera mappen + `%APPDATA%\SystemFlow Pro\`
**DoD:** FAQ publicerad, länkad från README + About.
**Estimat:** 2h

### T6.7 [P0] Slutlig v1.1.0-release
**Var:** Git tag + GitHub Release
**Åtgärd:**
```bash
git checkout main
git pull
git tag v1.1.0 -m "v1.1.0 — Production release"
git push origin v1.1.0
```
GitHub Actions bygger och publicerar. Markera **inte** som pre-release.

Release-notes (strukturerade):
```markdown
# v1.1.0 — Production release

Första produktionsmognad sedan v1.0.x-serien. Komplett omskrivning av
hårdvarulagret + ny UI-stack, ~3x bättre prestanda, tillgänglig för
skärmläsare, portabel öppen-källkod-distribution.

## Installation
Ladda ner `SystemFlow-Pro-v1.1.0-win-x64.zip`, packa upp, kör .exe.
Ingen installation, inga admin-rättigheter krävs.

## Höjdpunkter
[...lista från T6.2 + fixade beta-buggar]

## Migreringsnoter från 1.0.x
- Inställningar flyttas från registry (tidigare inte använt) till
  `%APPDATA%\SystemFlow Pro\settings.json`
- Ikonen är ny (Fluent-design)
- Vissa äldre CPU:er kan nu visa färre sensorer än tidigare — detta är
  korrekt beteende istället för påhittade värden

## Tack
Tack till beta-testarna [namn om ok att nämna].
```
**DoD:** v1.1.0 live på GitHub Releases, README badge uppdaterad.
**Estimat:** 2h

### T6.8 [P1] README-badge och shields
**Var:** `README.md` top
**Åtgärd:**
```markdown
![Version](https://img.shields.io/github/v/release/screamm/SystemFlow_Pro)
![Downloads](https://img.shields.io/github/downloads/screamm/SystemFlow_Pro/total)
![License](https://img.shields.io/github/license/screamm/SystemFlow_Pro)
![Build](https://github.com/screamm/SystemFlow_Pro/actions/workflows/ci.yml/badge.svg)
```
**DoD:** Badges visar korrekta värden.
**Estimat:** 0.5h

### T6.9 [P2] Annonsering (valfritt)
**Var:** Reddit (r/Windows, r/pcmasterrace, r/amd, r/nvidia), Twitter/X, HN Show
**Åtgärd:** Kort post:
> SystemFlow Pro 1.1 — öppen källkod Windows-systemövervakare
> byggd i WPF/.NET 9. Tightare än Task Manager, lättare än HWiNFO.
> Screenshot + GitHub-länk.
**DoD:** Inlägg postade där det passar. (Hoppa över om projektet inte är redo för bredare synlighet.)
**Estimat:** 1h

### T6.10 [P2] Retrospektiv på hela release-cykeln
**Var:** Ny fil `docs/RETRO_v1.1.md`
**Åtgärd:** Sammanfatta:
- Vad gick bra (rent tekniskt — tydlig arkitektur, CI-flöde, etc)
- Vad tog längre tid än estimerat (och varför)
- Vilka P2/P3 tasks flyttades till backlog / v1.2?
- Lärdomar för nästa release-cykel
- Tack-lista till beta-testare, eventuella externa bidragare, etc.
**DoD:** Dokument committat.
**Estimat:** 1h

### T6.11 [P2] Backlog-städ för v1.2-planering
**Var:** GitHub Issues / `docs/BACKLOG.md`
**Åtgärd:** Samla ihop alla P2/P3 från Sprint 1-6 som inte blev gjorda. Ideer från beta-testare. Prioritera grovt för nästa cykel. Exempel:
- Mica-stöd Windows 11 (om ej gjort i Sprint 4)
- Export av metrics till CSV
- Historikvyn (diagram över tid)
- Multi-språk (engelska utöver svenska)
- Dashboard-layouter (kompakt / utökad)
- Widget-läge (always-on-top mini-fönster)
**DoD:** Backlog existerar, grovt prioriterad. Inte nödvändigtvis committa — kan vara GitHub Issues.
**Estimat:** 1h

---

## Risk & beroenden

- **QA-matrisen (T6.1)** kräver tillgång till olika hårdvara. Om du saknar det — rekrytera testare med specifika setups i T6.3.
- **Beta-perioden** kan avslöja arkitekturproblem som kräver större fixar → ha buffert i tidsplanen.
- **SmartScreen-rykte** — osignerad .exe varnas av SmartScreen tills ~50-100 unika nedladdare valt "Kör ändå". Förväntat beteende; dokumenterat i README och FAQ.

---

## Definition of Ship (gate-kriterier för v1.1.0)

Alla måste vara uppfyllda innan `git tag v1.1.0`:

- [x] Alla P0-tasks från Sprint 1-6 klara
- [ ] `dotnet test` grön på CI
- [ ] Manuell QA godkänd på minst 3 konfigurationer
- [ ] Noll öppna P0 GitHub Issues
- [ ] exe producerad av CI
- [ ] Self-contained binär testad på clean Windows-maskin
- [ ] README och CHANGELOG uppdaterade
- [ ] `THIRD_PARTY_LICENSES.txt` inkluderat
- [ ] Privacy-dokument publicerat
- [ ] Auto-update-check fungerar

---

## Retro (fyll i efter release)

- Slutgiltig versionsnummer:
- Releasedatum:
- Antal beta-testare:
- Antal P0-buggar från beta:
- Aktiv tid (h) över alla 6 sprints:
- Vad skulle du göra annorlunda för v1.2?
