# SystemFlow Pro — Backlog

Punkter som inte togs med i v1.1.0 men är värda att överväga för framtida
versioner. Ordnad efter grov prioritet.

## Hög prioritet (v1.1.1 — bugfix-release)

- [ ] **°F-konvertering** i rendering. Settings-dialogen erbjuder °C/°F men
      konvertering av visade värden måste slutföras.
- [ ] **Git history-cleanup** — kör `git filter-repo --path releases
      --invert-paths` för att ta bort 800+ MB gamla release-artefakter ur
      historiken. Destruktiv — koordinera med ev. forks.
- [ ] **Ta bort obsoleta build-scripts** (`build_release_v1.0.X.bat`).
      Ersätts helt av enade `build.bat`.

## Avfört (inte planerat)

- **Code-signing.** Ej planerat. SystemFlow Pro distribueras osignerat som
  öppen källkod — kostnad (200-500 USD/år) motiveras inte för ett gratis
  projekt. Användare hanterar SmartScreen via "Mer info" → "Kör ändå" och
  kan granska all kod på GitHub. Dokumenterat i README och FAQ.

## Medel prioritet (v1.2.0 — feature-release)

- [ ] **Mica-backdrop** (Windows 11). DwmSetWindowAttribute-interop via
      Win32-API. Fallback till platt bakgrund på Win10.
- [ ] **Auto-update-nedladdning.** Idag visar update-checken bara en länk.
      Implementera Velopack eller Squirrel för in-app nedladdning +
      auto-patching.
- [ ] **Engelsk lokalisering.** Idag bara svenska. `Resources/Strings.resx`
      + `Strings.en.resx`.
- [ ] **Export metrics till CSV.** Knapp "Spara mätdata" i header eller
      Settings som skriver current snapshot till CSV.
- [ ] **Historik-läge.** Spara senaste N snapshots i minne, visa som
      mini-diagram per hero-kort.
- [ ] **Full XAML-binding konvertering.** MainWindow.xaml.cs renderar
      fortfarande paneler imperativt. Konvertera till `ItemsControl` +
      `DataTemplate` + value converters för ren MVVM.
- [ ] **Settings: filter sensorer.** Låt användaren dölja specifika
      sensorer som inte är intressanta.
- [ ] **Widget-läge.** Always-on-top mini-fönster med bara hero-värdena.

## Låg prioritet (v1.3+ eller backlog)

- [ ] **Cross-platform via MAUI eller Avalonia** — Linux/macOS-stöd.
      Kräver annan sensorkälla (`/sys/class/hwmon/` på Linux).
- [ ] **Grafer över tid** (full historikvy med diagram per sensor).
- [ ] **Mörka/ljusa teman** — idag bara mörkt.
- [ ] **Fler språk** — tyska, finska, norska.
- [ ] **Åldersspridda sensor-stöd** — tydlig indikation när en sensor inte
      stöds + varför (log-entry i UI).
- [ ] **Benchmarkintegration** — kör Cinebench/3DMark och visa resultat
      bredvid live-data.
- [ ] **MQTT/REST-endpoint** — exponera sensorer till Home Assistant.

## Tekniska skulder

- [ ] **Fler enhetstester** — ViewModel-beteende (tick-skip, paus-återuppta,
      settings-apply). Kräver mock IHardwareService + Dispatcher-stub.
- [ ] **Integration-tester** för HardwareService — kräver hårdvara, märk
      `[Trait("Category", "Integration")]`.
- [ ] **Performance-benchmarks** — BenchmarkDotNet på `HardwareService.CollectSnapshot`
      för att fånga regressioner.
- [ ] **Remove old build_release_v1.0.X.bat** — obsoleta efter Sprint 5.
- [ ] **Legacy scripts-mapp** — `scripts/claude_autonomous_loop.py` är
      dev-tooling, hör inte hemma i produktionsrepo.
- [ ] **README: bygga steg** — dokumentera `build.bat [version]` bättre,
      inkludera förväntad output-storlek.

## Öppna frågor

- Ska vi lägga till Sentry eller annan crash-reporting? Opt-in, GDPR-kompatibelt.
- Vilka fler lokaliseringar är värda — kolla GitHub Issues för förfrågningar.
- Finns det användare som vill ha CLI-läge (ingen UI, bara logging till CSV)?
