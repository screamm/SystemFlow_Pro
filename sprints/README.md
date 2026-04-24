# SystemFlow Pro — Sprint-plan mot v1.1 Production

**Startversion:** v1.0.8.1
**Målversion:** v1.1.0 (self-contained, produktionsredo, öppen källkod osignerad)
**Total estimerad tid:** 6 sprints, ca 8-10 veckors deltidsarbete (solo)

## Sprintöversikt

| # | Sprint | Mål | Beräknad tid |
|---|--------|-----|--------------|
| 1 | [Stabilisering & säkerhet](sprint-01-stabilisering-sakerhet.md) | Fixa kritiska buggar (`.Result`, catch-block, admin, Accept-spam) | 1 vecka |
| 2 | [Prestanda & resurshantering](sprint-02-prestanda.md) | UI-tråd frikopplad, GC-tryck halverat, inga läckor | 1 vecka |
| 3 | [Arkitektur & testbarhet](sprint-03-arkitektur-tester.md) | HardwareService extraherad, MVVM-lite, första enhetstester | 2 veckor |
| 4 | [UI/UX modernisering](sprint-04-ui-ux.md) | Fluent-ikoner, accessibility, fixat chrome, Settings | 2 veckor |
| 5 | [Produktions-pipeline](sprint-05-produktions-pipeline.md) | Self-contained build, CI/CD, crash reporting, auto-update | 1-2 veckor |
| 6 | [Release 1.1 & QA](sprint-06-release.md) | Beta, buggjakt, release | 1 vecka |

## Prioritetsnivåer (används i alla sprints)

- **[P0]** Blockerare — sprint kan inte stängas utan detta
- **[P1]** Hög — bör göras i sprint, kan skjutas max en sprint
- **[P2]** Medel — trevligt att få med
- **[P3]** Låg — backlog

## Definition of Done (gäller alla sprints)

1. Kod kompilerar utan varningar
2. Appen startar och kör utan undantag i minst 15 minuter
3. Alla nya/ändrade kodvägar manuellt testade på utvecklingsmaskin
4. `git commit` med beskrivande meddelande per task
5. Feature-branch merged till `main` via PR (självgranskning räcker, men separata commits)
6. Inga nya tomma `catch {}`-block införda
7. Inga nya multicolored emojis i UI-koden

## Versionsstrategi under sprints

- Sprint 1 färdig → v1.0.9 (säkerhetsrelease)
- Sprint 2 färdig → v1.0.10 (prestandarelease)
- Sprint 3-4 färdiga → v1.1.0-beta.1
- Sprint 5-6 färdiga → v1.1.0 (production)

## Arbetsrutin per sprint

1. Skapa feature-branch `sprint-NN-kort-beskrivning`
2. Gå igenom tasks i ordning — tidiga tasks låser upp senare
3. Uppdatera `CHANGELOG_v1.0.X.md` löpande
4. Stäng sprint med tag + release-notes i `releases/`
5. Skapa en kort retro (1-3 bullets) i botten av sprint-filen när klar
