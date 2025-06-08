# 🚀 GitHub Release Guide - SystemFlow Pro

## 📦 Skapa din första Release

### 1️⃣ **Förbered filen**
```bash
# Bygg single-file exe
.\create_single_exe.bat

# Komprimera för snabbare nedladdning (optional)
Compress-Archive -Path "publish-true-single\SystemMonitorApp.exe" -DestinationPath "SystemFlow-Pro-v1.0.0.zip"
```

### 2️⃣ **Skapa Release på GitHub**

1. **Gå till ditt repository** på GitHub
2. **Klicka "Releases"** (höger sida)
3. **"Create a new release"**
4. **Fyll i formuläret:**

**Tag version:** `v1.0.0`  
**Release title:** `SystemFlow Pro v1.0.0 - Initial Release`

**Release notes mall:**
```markdown
# 🎉 SystemFlow Pro v1.0.0

## ✨ Features
- Real-time system monitoring (CPU, GPU, RAM, Storage)
- Hardware temperature tracking
- Fan speed monitoring with Zero RPM detection
- Modern Windows 11 design
- Single executable file - no installation required

## 📦 Download
- **SystemFlow-Pro.exe** (131MB) - Complete standalone application
- **SystemFlow-Pro-v1.0.0.zip** (50MB) - Compressed version

## 💻 System Requirements
- Windows 11 (recommended) / Windows 10
- Administrator privileges required
- No .NET installation needed

## 🔧 Installation
1. Download `SystemFlow-Pro.exe`
2. Right-click → "Run as administrator"
3. Accept UAC prompt
4. Enjoy real-time monitoring!

## 🐛 Known Issues
- Requires admin rights for hardware access
- First startup may take 5-10 seconds

---
Built with ❤️ using .NET 9.0 and LibreHardwareMonitor
```

5. **Lägg till filer:**
   - Dra `SystemFlow-Pro.exe` till "Attach binaries"
   - Dra `SystemFlow-Pro-v1.0.0.zip` (optional)

6. **Publicera:** "Publish release"

### 3️⃣ **Framtida versioner**

**Semantisk versioning:**
- `v1.0.0` - Initial release
- `v1.0.1` - Bug fixes
- `v1.1.0` - New features
- `v2.0.0` - Breaking changes

## 🔄 Automatisering (Advanced)

Skapa `.github/workflows/release.yml`:
```yaml
name: Create Release
on:
  push:
    tags:
      - 'v*'
      
jobs:
  build-and-release:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Build single-file exe
      run: |
        dotnet publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
        
         - name: Create Release
       uses: softprops/action-gh-release@v1
       with:
         files: publish/SystemFlow-Pro.exe
         generate_release_notes: true
```

## 📊 Statistik & Analytics
- GitHub visar automatiskt nedladdnings-statistik
- Användare kan rapportera issues
- Community kan bidra med förbättringar

## 🎯 Best Practices
1. **Testa alltid** på clean Windows innan release
2. **Skriv tydliga release notes**
3. **Använd beskrivande taggar**
4. **Inkludera både exe och zip**
5. **Svara på användar-feedback** 