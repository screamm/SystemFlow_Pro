# Sprint 6 — Release 1.1 & QA

**Goal:** Systematic QA across hardware and Windows versions. Beta release to selected testers. Gather feedback. Address regressions. Publish v1.1.0 as the official stable version (unsigned, open source).

**Duration:** 1 week (~25-35h active + beta period of 3-5 days)
**Branch:** `sprint-06-release` → `main`
**Target version:** v1.1.0 (Production)
**Prerequisites:** Sprint 5 complete, CI green, build produces self-contained exe

---

## Sprint goal

- [ ] Manual QA on at least 3 Windows configurations (Win10, Win11 Intel, Win11 AMD)
- [ ] Beta release `v1.1.0-rc.1` to 5-10 testers
- [ ] All P0 bugs from beta fixed
- [ ] Release notes for v1.1.0 published
- [ ] v1.1.0 published on GitHub Releases
- [ ] Main branch tagged `v1.1.0`
- [ ] README badge updated
- [ ] Announcement (if relevant — subreddits, Twitter, etc.)

---

## Tasks

### T6.1 [P0] Manual QA matrix
**Where:** New file `docs/QA_CHECKLIST.md` + execution
**Why:** Automated tests only cover logic. Real hardware issues must be tested manually.
**Action:** Checklist per configuration:

**Hardware to test:**
- [ ] AMD Ryzen (7000 series or newer)
- [ ] AMD Ryzen (5000 series or older — tests fallback)
- [ ] Intel 13th gen+ (E-cores + P-cores)
- [ ] Intel 10th gen or older (older sensors)
- [ ] NVIDIA RTX 30/40/50 series
- [ ] AMD Radeon (if available)
- [ ] Laptop with hybrid graphics
- [ ] Desktop with multiple fans / AIO cooler / pump sensors

**Windows versions:**
- [ ] Windows 10 22H2 (latest)
- [ ] Windows 11 23H2
- [ ] Windows 11 24H2 / 25H2
- [ ] Windows 11 ARM64 (if time — requires separate build)

**Per configuration, verify:**
- The app starts without errors (no admin prompt, no SmartScreen block)
- Splash displays briefly, then disappears
- All hero cards show sensor data within 3 seconds
- CPU usage value matches Task Manager (within ±5%)
- GPU usage matches MSI Afterburner / Task Manager
- Temperatures are reasonable (no 0°C, no 200°C)
- RAM usage matches Task Manager
- Fans show RPM values that are accurate (verify against fan controller/motherboard BIOS)
- Zero-RPM fans show "0 RPM" without an error message
- Settings dialog opens, saves, changes polling interval
- About dialog shows version 1.1.0
- Minimize → CPU usage in Task Manager drops to ~0%
- Restore → updating continues
- Close → the process disappears from Task Manager within 2s
- Run for 30 min → no memory growth (verify in Task Manager)
- At 150% DPI: no text is clipped
- Alt-tab / Win+Up / Win+Down / Win+Z (Snap Layouts) work
- Narrator (Win+Ctrl+Enter) reads meaningful names

**DoD:** All items approved on at least 3 configurations. Bugs documented as GitHub Issues.
**Estimate:** 10-15h (including test setup)

### T6.2 [P0] Publish beta `v1.1.0-rc.1`
**Where:** Git tag + GitHub Release
**Action:**
```bash
git tag v1.1.0-rc.1 -m "Release candidate 1 for v1.1.0"
git push origin v1.1.0-rc.1
```
GitHub Actions workflow (from Sprint 5) builds and publishes. Mark as "Pre-release" in GitHub UI.
Write beta message:
```markdown
# v1.1.0-rc.1 — Beta release

Testing all major improvements from Sprint 1-5.
**Want to help?** Download, run it for a week, report bugs in Issues.

## What's new since v1.0.8.1
- New architecture layer (MVVM) — 60% less code-behind
- ~3% CPU overhead (previously 8-15%)
- Self-contained distribution — no .NET installation required
- Unsigned binaries, documented — SmartScreen instructions in README/FAQ
- Settings dialog + About dialog
- Accessibility: screen reader support + keyboard navigation
- Fluent icons instead of emoji
- Aero Snap + Snap Layouts work

## Known risks
Beta version — may contain regressions. Please report.
```
**DoD:** GitHub Release published, testers have access.
**Estimate:** 2h

### T6.3 [P0] Recruit beta testers
**Where:** External — social media, acquaintances, communities
**Action:** Contact 5-10 people with varied hardware. Share the release link + a short description of what you want feedback on. Create a simple Google Form or GitHub Issue template for reports.
**DoD:** At least 5 confirmed testers.
**Estimate:** 2h

### T6.4 [P0] Beta period + feedback collection
**Where:** 3-5 days with the beta out
**Action:** Daily:
- Check GitHub Issues for new reports
- Check the Sentry dashboard (if enabled) for automatic crash reports
- Reply to testers with confirmation / clarification questions
- Prioritize bugs: crash / wrong data = P0, UX friction = P1, wishes = P2
**DoD:** All reports triaged, P0 bugs identified.
**Estimate:** 3-5 days passive + 4-6h active

### T6.5 [P0] Fix regressions from beta
**Where:** Affected files based on reports
**Action:** For each P0 bug:
1. Reproduce locally (or ask the reporter for more info)
2. Fix on a new feature branch `fix/issue-NN`
3. Add a regression test if possible
4. Merge after review
5. Publish `v1.1.0-rc.2` (and rc.3 if necessary)
**DoD:** Zero open P0 bugs. All rc.2+ releases tested by at least 2 testers.
**Estimate:** 8-15h (depending on the number of bugs)

### T6.6 [P1] Update documentation based on feedback
**Where:** `README.md`, `PRIVACY.md`, `docs/FAQ.md` (new)
**Action:** If multiple testers ask the same thing → add to FAQ. Common examples:
- "Why is 0°C shown on my CPU?" → sensor support per CPU generation
- "Why does antivirus block the app?" → SmartScreen + false positives
- "How do I uninstall?" → self-contained, just delete the folder + `%APPDATA%\SystemFlow Pro\`
**DoD:** FAQ published, linked from README + About.
**Estimate:** 2h

### T6.7 [P0] Final v1.1.0 release
**Where:** Git tag + GitHub Release
**Action:**
```bash
git checkout main
git pull
git tag v1.1.0 -m "v1.1.0 — Production release"
git push origin v1.1.0
```
GitHub Actions builds and publishes. Do **not** mark as pre-release.

Release notes (structured):
```markdown
# v1.1.0 — Production release

First production maturity since the v1.0.x series. Complete rewrite of the
hardware layer + new UI stack, ~3x better performance, accessible for
screen readers, portable open-source distribution.

## Installation
Download `SystemFlow-Pro-v1.1.0-win-x64.zip`, unzip, run the .exe.
No installation, no admin rights required.

## Highlights
[...list from T6.2 + fixed beta bugs]

## Migration notes from 1.0.x
- Settings moved from registry (previously not used) to
  `%APPDATA%\SystemFlow Pro\settings.json`
- The icon is new (Fluent design)
- Some older CPUs may now show fewer sensors than before — this is
  correct behavior rather than fabricated values

## Thanks
Thanks to the beta testers [names if ok to mention].
```
**DoD:** v1.1.0 live on GitHub Releases, README badge updated.
**Estimate:** 2h

### T6.8 [P1] README badges and shields
**Where:** `README.md` top
**Action:**
```markdown
![Version](https://img.shields.io/github/v/release/screamm/SystemFlow_Pro)
![Downloads](https://img.shields.io/github/downloads/screamm/SystemFlow_Pro/total)
![License](https://img.shields.io/github/license/screamm/SystemFlow_Pro)
![Build](https://github.com/screamm/SystemFlow_Pro/actions/workflows/ci.yml/badge.svg)
```
**DoD:** Badges show correct values.
**Estimate:** 0.5h

### T6.9 [P2] Announcement (optional)
**Where:** Reddit (r/Windows, r/pcmasterrace, r/amd, r/nvidia), Twitter/X, HN Show
**Action:** Short post:
> SystemFlow Pro 1.1 — open source Windows system monitor
> built in WPF/.NET 9. Tighter than Task Manager, lighter than HWiNFO.
> Screenshot + GitHub link.
**DoD:** Posts published where it fits. (Skip if the project is not ready for wider visibility.)
**Estimate:** 1h

### T6.10 [P2] Retrospective on the entire release cycle
**Where:** New file `docs/RETRO_v1.1.md`
**Action:** Summarize:
- What went well (technically — clear architecture, CI flow, etc.)
- What took longer than estimated (and why)
- Which P2/P3 tasks were moved to backlog / v1.2?
- Lessons for the next release cycle
- Thank-you list to beta testers, any external contributors, etc.
**DoD:** Document committed.
**Estimate:** 1h

### T6.11 [P2] Backlog cleanup for v1.2 planning
**Where:** GitHub Issues / `docs/BACKLOG.md`
**Action:** Gather all P2/P3 items from Sprint 1-6 that were not completed. Ideas from beta testers. Prioritize roughly for the next cycle. Examples:
- Mica support for Windows 11 (if not done in Sprint 4)
- Export of metrics to CSV
- History view (chart over time)
- Multi-language (English in addition to Swedish)
- Dashboard layouts (compact / expanded)
- Widget mode (always-on-top mini window)
**DoD:** Backlog exists, roughly prioritized. Not necessarily committed — can be GitHub Issues.
**Estimate:** 1h

---

## Risk & dependencies

- **The QA matrix (T6.1)** requires access to different hardware. If you lack it — recruit testers with specific setups in T6.3.
- **The beta period** may reveal architectural issues that require larger fixes → keep buffer in the schedule.
- **SmartScreen reputation** — unsigned .exe is warned about by SmartScreen until ~50-100 unique downloaders have chosen "Run anyway". Expected behavior; documented in README and FAQ.

---

## Definition of Ship (gate criteria for v1.1.0)

All must be satisfied before `git tag v1.1.0`:

- [x] All P0 tasks from Sprint 1-6 complete
- [ ] `dotnet test` green on CI
- [ ] Manual QA approved on at least 3 configurations
- [ ] Zero open P0 GitHub Issues
- [ ] exe produced by CI
- [ ] Self-contained binary tested on a clean Windows machine
- [ ] README and CHANGELOG updated
- [ ] `THIRD_PARTY_LICENSES.txt` included
- [ ] Privacy document published
- [ ] Auto-update check works

---

## Retrospective (fill in after release)

- Final version number:
- Release date:
- Number of beta testers:
- Number of P0 bugs from beta:
- Active time (h) across all 6 sprints:
- What would you do differently for v1.2?
