# 🚀 GitHub Release Guide - SystemFlow Pro

## 📦 Create your first Release

### 1️⃣ **Prepare the file**
```bash
# Build single-file exe
.\create_single_exe.bat

# Compress for faster download (optional)
Compress-Archive -Path "publish-true-single\SystemMonitorApp.exe" -DestinationPath "SystemFlow-Pro-v1.0.0.zip"
```

### 2️⃣ **Create a Release on GitHub**

1. **Go to your repository** on GitHub
2. **Click "Releases"** (right side)
3. **"Create a new release"**
4. **Fill in the form:**

**Tag version:** `v1.0.0`  
**Release title:** `SystemFlow Pro v1.0.0 - Initial Release`

**Release notes template:**
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
Built with .NET 9.0 and LibreHardwareMonitor
```

5. **Add files:**
   - Drag `SystemFlow-Pro.exe` to "Attach binaries"
   - Drag `SystemFlow-Pro-v1.0.0.zip` (optional)

6. **Publish:** "Publish release"

### 3️⃣ **Future versions**

**Semantic versioning:**
- `v1.0.0` - Initial release
- `v1.0.1` - Bug fixes
- `v1.1.0` - New features
- `v2.0.0` - Breaking changes

## 🔄 Automation (Advanced)

Create `.github/workflows/release.yml`:
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

## 📊 Statistics & Analytics
- GitHub automatically shows download statistics
- Users can report issues
- The community can contribute improvements

## 🎯 Best Practices
1. **Always test** on clean Windows before release
2. **Write clear release notes**
3. **Use descriptive tags**
4. **Include both exe and zip**
5. **Respond to user feedback**
