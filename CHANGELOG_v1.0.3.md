# SystemFlow Pro - Changelog v1.0.3

## Version 1.0.3 - Förbättrad Hårdvarudetektering (2025-09-10)

### 🚀 Nya Funktioner

#### Förbättrad Fläktdetektering för Äldre System
- **Multipla Detektionsstrategier**: Implementerat fallback-system med 3 nivåer
  1. LibreHardwareMonitor (primär)
  2. Windows WMI (Win32_Fan/Win32_TemperatureProbe)
  3. Temperaturbaserad uppskattning (sista utväg)

- **Smart Administratörsdetektering**: Automatisk identifiering av admin-läge med användarguide
- **Enhanced Error Messages**: Kontextuella förklaringar istället för generiska fel

#### Temperaturbaserad Fläktuppskattning
```
Temperatur → Uppskattad RPM:
<40°C     → 800 RPM   (låg)
40-50°C   → 1200 RPM  (medium)  
50-65°C   → 2000 RPM  (hög)
65-80°C   → 3000 RPM  (mycket hög)
>80°C     → 4000 RPM  (maximum)
```

#### Förbättrad Hårdvarukompatibilitet
- Bättre stöd för äldre GPU-drivrutiner som inte exponerar fläktdata
- Förbättrad detektering av systemfläktar på äldre moderkort
- WMI-integration för backup på system där LibreHardwareMonitor misslyckas

### 🔧 Tekniska Implementationer

#### Nya Metoder i MainWindow.xaml.cs:
```csharp
// Multi-strategy fan detection
private async Task TryLibreHardwareMonitorFans(Dictionary<string, float> fans)
private async Task TryWindowsManagementFans(Dictionary<string, float> fans)  
private async Task TryEstimatedFansFromTemperature(Dictionary<string, float> fans)

// Hardware capability tracking
private bool IsRunningAsAdministrator()
private string GetDetectionStatusMessage()
private float EstimateFanSpeedFromTemperature(float temperature, string componentName)
```

#### Nya Medlemsvariabler:
```csharp
private bool _isAdminMode = false;
private Dictionary<string, string> _hardwareCapabilities = new Dictionary<string, string>();
private Dictionary<string, DateTime> _lastFanDetection = new Dictionary<string, DateTime>();
```

### 🛠️ Förbättringar

#### User Experience
- **Status-indikatorer**: Visuella emojis (✅❌⚠️) för detektionsstatus
- **Kontextuella meddelanden**: Specifik guidance baserat på systemkonfiguration
- **Administratörshjälp**: Tydlig information om admin-läge och fördelar

#### Robust Felhantering
- **Graceful Degradation**: Applikationen fungerar även om vissa sensorer saknas
- **Capability Tracking**: Spårar vilka metoder som fungerar per session
- **Debug Logging**: Förbättrad felsökning med detaljerade meddelanden

### 📋 UI/UX-förändringar

#### Förbättrade Felmeddelanden
**Innan (v1.0.2):**
```
"Inga GPU-fläktar detekterade"
```

**Efter (v1.0.3):**
```
"Inga GPU-fläktar detekterade

✅ Administratörsbehörighet: Ja

Detektionsstatus:
✅ LibreHardwareMonitor: Supported
❌ Windows WMI: Failed: Access denied
✅ Temperaturbaserad uppskattning: Active

Detta är normalt på äldre system:
• GPU-fläktar ej exponerade av drivrutin
• Zero RPM Mode vid låga temperaturer
• Passiv kylning
• Rapporterar som procent istället för RPM"
```

### 🔄 Breaking Changes
Inga - fullt bakåtkompatibel med v1.0.2

### 🐛 Bugfixes från v1.0.2
- Förbättrad text-overflow hantering (från tidigare fix)
- Bättre RPM-konvertering för äldre system (från tidigare fix)

### 📦 Build Information
- **Target Framework**: NET 9.0-Windows (samma som v1.0.2)
- **Dependencies**: 
  - LibreHardwareMonitorLib 0.9.4
  - System.Management 9.0.0 (för WMI-stöd)
- **Kompatibilitet**: Windows 10/11

### 🚨 Release Status
**Source Code**: ✅ Komplett (version 1.0.3)  
**Binary Build**: ⏳ Kräver manual build (release directory innehåller v1.0.2 binaries)

För att få alla nya funktioner:
1. Öppna `SystemMonitorApp.csproj` i Visual Studio
2. Build → Build Solution (Release mode)
3. Eller se `releases/v1.0.3/BUILD_INFO.txt` för instruktioner

---
**Sammanfattning**: v1.0.3 är en stor förbättring för äldre system som tidigare hade problem med fläktdetektering. Huvudfokus på kompatibilitet och användarvänlighet.