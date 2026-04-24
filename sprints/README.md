# SystemFlow Pro — Sprint plan toward v1.1 Production

**Starting version:** v1.0.8.1
**Target version:** v1.1.0 (self-contained, production-ready, open source unsigned)
**Total estimated time:** 6 sprints, approx. 8-10 weeks part-time work (solo)

## Sprint overview

| # | Sprint | Goal | Estimated time |
|---|--------|------|----------------|
| 1 | [Stabilization & security](sprint-01-stabilisering-sakerhet.md) | Fix critical bugs (`.Result`, catch blocks, admin, Accept spam) | 1 week |
| 2 | [Performance & resource management](sprint-02-prestanda.md) | UI thread decoupled, GC pressure halved, no leaks | 1 week |
| 3 | [Architecture & testability](sprint-03-arkitektur-tester.md) | HardwareService extracted, MVVM-lite, first unit tests | 2 weeks |
| 4 | [UI/UX modernization](sprint-04-ui-ux.md) | Fluent icons, accessibility, fixed chrome, Settings | 2 weeks |
| 5 | [Production pipeline](sprint-05-produktions-pipeline.md) | Self-contained build, CI/CD, crash reporting, auto-update | 1-2 weeks |
| 6 | [Release 1.1 & QA](sprint-06-release.md) | Beta, bug hunting, release | 1 week |

## Priority levels (used in all sprints)

- **[P0]** Blocker — sprint cannot be closed without this
- **[P1]** High — should be done in sprint, can be deferred max one sprint
- **[P2]** Medium — nice to include
- **[P3]** Low — backlog

## Definition of Done (applies to all sprints)

1. Code compiles without warnings
2. App starts and runs without exceptions for at least 15 minutes
3. All new/changed code paths manually tested on development machine
4. `git commit` with descriptive message per task
5. Feature branch merged to `main` via PR (self-review is sufficient, but separate commits)
6. No new empty `catch {}` blocks introduced
7. No new multicolored emojis in UI code

## Version strategy during sprints

- Sprint 1 complete → v1.0.9 (security release)
- Sprint 2 complete → v1.0.10 (performance release)
- Sprints 3-4 complete → v1.1.0-beta.1
- Sprints 5-6 complete → v1.1.0 (production)

## Work routine per sprint

1. Create feature branch `sprint-NN-short-description`
2. Work through tasks in order — early tasks unlock later ones
3. Update `CHANGELOG_v1.0.X.md` continuously
4. Close sprint with tag + release notes in `releases/`
5. Create a short retro (1-3 bullets) at the bottom of the sprint file when complete
