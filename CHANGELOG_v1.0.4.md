# SystemFlow Pro v1.0.4 Changelog

**Release Date**: 2025-01-09  
**Build**: 1.0.4.0

## 🎯 Key Improvements

### ✅ Fixed Fan Display Flickering
- **Issue**: GPU and CPU fan readings were flickering/blinking continuously
- **Solution**: Implemented UI caching system to prevent unnecessary panel recreation
- **Technical**: Added `_fanTextBlocks` dictionary in `MainWindow.xaml.cs:712-720`
- **Impact**: Smooth, stable fan speed display without visual disruption

### 🏷️ Version Number Display
- **Feature**: Added version number to application header bar
- **Location**: Top-right header area next to logo
- **Style**: Subtle gray text matching modern UI design
- **Purpose**: Easy identification for support and updates

### ⏳ Professional Loading Screen
- **Feature**: Elegant splash screen during 3-5 second app startup
- **Design**: Modern dark theme with animated loading spinner
- **Text**: Swedish loading messages with progress indication
- **Animation**: Smooth fade-in/fade-out transitions
- **Technical**: Asynchronous main window loading with proper timing

## 🔧 Technical Changes

### File Modifications
- **MainWindow.xaml**: Added version display element
- **MainWindow.xaml.cs**: Implemented fan UI caching system
- **App.xaml**: Removed automatic startup URI
- **App.xaml.cs**: Added splash screen orchestration
- **SystemMonitorApp.csproj**: Updated to version 1.0.4

### New Files
- **SplashWindow.xaml**: Modern splash screen interface
- **SplashWindow.xaml.cs**: Loading sequence management
- **build_release_v1.0.4.bat**: Automated build script

## 🎨 User Experience Enhancements

### Visual Improvements
- **No Flickering**: Fan readings update smoothly without disruption
- **Clear Versioning**: Version number prominently displayed in header
- **Professional Loading**: Polished startup experience with progress feedback
- **Consistent Theming**: All new elements match existing dark modern theme

### Performance Optimizations
- **UI Efficiency**: Reduced UI recreation through intelligent caching
- **Smooth Animations**: Hardware-accelerated transitions and effects
- **Async Loading**: Non-blocking startup sequence for better responsiveness

## 📋 Previous Features (Maintained)

### Enhanced Hardware Detection (v1.0.3)
- Multi-strategy fan detection (LibreHardwareMonitor → WMI → Temperature estimation)
- Administrator detection with user guidance
- Better error handling for older hardware

### Robust System Monitoring
- Real-time CPU, GPU, memory, disk monitoring
- Modern dark UI with gradient accents
- Comprehensive hardware sensor support
- Automatic refresh and live updates

## 🔍 Testing Recommendations

### Startup Testing
1. Launch application and verify smooth loading screen appearance
2. Confirm 2+ second loading time with proper animations  
3. Validate main window appears after splash screen closes
4. Check version number displays correctly in header

### Fan Display Testing
1. Monitor CPU and GPU fan readings for stability
2. Verify no flickering or rapid text changes
3. Test with various load conditions
4. Confirm readings update appropriately

## 💻 Build Information

**Framework**: .NET 9.0-windows  
**Dependencies**: LibreHardwareMonitorLib 0.9.4, System.Management 9.0.0  
**Build Target**: Release configuration  
**Deployment**: Single executable with all dependencies

## 🚀 Installation

1. Download `SystemFlow-Pro.exe` from releases/v1.0.4/
2. Run as Administrator for full hardware access
3. No additional installation required - portable executable
4. Supports Windows 10 1809+ and Windows 11

---

**Previous Versions**: [v1.0.3](CHANGELOG_v1.0.3.md) | [v1.0.2](CHANGELOG_v1.0.2.md)