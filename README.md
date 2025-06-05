# 🐒 SystemMonkey - Advanced System Monitor for Windows 11

A real-time system monitoring application that displays CPU usage, memory consumption, temperatures, and fan information for Windows 11. Built with .NET 9 and WPF for modern Windows systems.

![System Monitor](https://img.shields.io/badge/Platform-Windows%2011-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

- 🖥️ **CPU Monitoring**: Real-time total CPU usage and per-core utilization
- 🧠 **Memory Usage**: Live memory consumption tracking
- 🌡️ **Temperature Monitoring**: System temperatures via WMI thermal zones
- 🌀 **Fan Information**: Fan speeds and status (where available)
- 🎮 **GPU Information**: Graphics card details and VRAM usage
- 📊 **System Information**: Computer name, OS, processor specifications
- ⚡ **Real-time Updates**: Data refreshes every second
- 🎨 **Modern UI**: Dark theme optimized for Windows 11

## 🛠️ Installation

### Prerequisites

1. **Windows 11** (or Windows 10)
2. **.NET 9.0 SDK** or **.NET 9.0 Desktop Runtime**
3. **Administrator privileges** (recommended for full functionality)

### Step 1: Install .NET 9.0

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download ".NET 9.0 Desktop Runtime" (for end users) or ".NET 9.0 SDK" (for developers)
3. Run the installer and follow the instructions

### Step 2: Clone and Build

```bash
# Clone the repository
git clone https://github.com/screamm/SystemMonkey.git
cd SystemMonkey

# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Alternative: Visual Studio

1. Open `SystemMonitorApp.csproj` in Visual Studio 2022
2. Press F5 to build and run

## 🚀 Usage

### Running as Administrator (Recommended)

For full hardware data access:

1. Right-click on PowerShell or the application executable
2. Select "Run as administrator"
3. Launch the application

### Application Features

- **Real-time Data**: Automatically updates every second
- **Refresh Button**: Manual system information refresh
- **Scrollable Interface**: View all information comfortably

## 📊 What's Displayed

### CPU Usage
- Total CPU utilization percentage
- Individual load per CPU core/thread

### Memory
- Available memory in GB
- Used vs. free memory breakdown

### Temperatures
- System thermal zone temperatures
- GPU temperature information

### Fans
- Fan names and speeds (where available via WMI)

### System Information
- Computer name and user
- Operating system details
- Processor specifications
- Total memory capacity

## 📂 Included Versions

This repository includes multiple implementations:

1. **WPF Application** (`SystemMonitorApp.csproj`) - Main graphical application
2. **PowerShell Script** (`SystemMonitor-Simple.ps1`) - Command-line version

### Running the PowerShell Version

```powershell
# Simple one-time system report
powershell -ExecutionPolicy Bypass -File "./SystemMonitor-Simple.ps1"
```

## ⚠️ Limitations

- **Fan Data**: May be limited on laptops and certain systems
- **Temperature Data**: Varies depending on hardware and drivers
- **GPU Data**: Basic information (can be extended with NVIDIA/AMD-specific APIs)
- **Administrator Rights**: Some sensors require administrative access

## 🔧 Technical Information

- **Framework**: .NET 9.0 WPF
- **UI**: Windows Presentation Foundation with modern dark design
- **Hardware Data**: Windows Management Instrumentation (WMI)
- **Performance Counters**: For CPU and memory monitoring
- **Update Frequency**: 1 second intervals

## 📁 Project Structure

```
SystemMonkey/
├── SystemMonitorApp.csproj          # Main WPF project file
├── MainWindow.xaml                  # UI design
├── MainWindow.xaml.cs               # Main application logic
├── App.xaml                         # Application definition
├── App.xaml.cs                      # Application startup logic
├── app.manifest                     # Windows UAC manifest
├── SystemMonitor-Simple.ps1         # PowerShell version
└── README.md                        # This file
```

## 🚨 Troubleshooting

### Application Won't Start
- Verify .NET 9.0 Desktop Runtime is installed
- Run as administrator for full functionality
- Check that all dependencies are properly installed

### Missing Temperature/Fan Data
- Some systems don't expose this data via WMI
- Try running as administrator
- Ensure hardware drivers are up to date

### Performance Counter Errors
- Run `winmgmt /verifyrepository` in cmd as administrator
- If corrupted: `winmgmt /salvagerepository`

### .NET Command Not Found
If `dotnet` command isn't recognized, use the full path:
```powershell
& "C:\Program Files\dotnet\dotnet.exe" run
```

## 🔮 Future Enhancements

- 📈 Graphical charts for usage history
- 🎮 NVIDIA/AMD-specific GPU monitoring
- 🌡️ Additional temperature sensors
- 💾 Export data to file
- ⚙️ Customizable update intervals
- 🚨 Temperature threshold alerts
- 📱 System tray integration

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📜 License

This project is open source and available under the [MIT License](LICENSE).

## 🙏 Acknowledgments

- Built with ❤️ for the Windows community
- Uses Windows Management Instrumentation (WMI) for hardware data
- Inspired by the need for a modern, lightweight system monitor

---

**Developed for Windows 11** 🪟 | **Powered by .NET 9** ⚡ 