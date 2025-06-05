# System Monitor for Windows 11 - Simple PowerShell Version
# Run as administrator for best results

Clear-Host
Write-Host "System Monitor for Windows 11" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Check admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as administrator" -ForegroundColor Yellow
    Write-Host "Some functions may not work properly" -ForegroundColor Yellow
    Write-Host ""
}

# System Information
Write-Host "SYSTEM INFORMATION" -ForegroundColor Green
Write-Host "Computer: $env:COMPUTERNAME"
Write-Host "OS: $((Get-WmiObject Win32_OperatingSystem).Caption)"
Write-Host "User: $env:USERNAME"
Write-Host ""

# CPU Information
$cpu = Get-WmiObject Win32_Processor
Write-Host "CPU INFORMATION" -ForegroundColor Yellow
Write-Host "Processor: $($cpu.Name)"
Write-Host "Cores: $($cpu.NumberOfCores)"
Write-Host "Logical Processors: $($cpu.NumberOfLogicalProcessors)"
Write-Host "Max Clock Speed: $($cpu.MaxClockSpeed) MHz"
Write-Host ""

# Memory Information
$memory = Get-WmiObject Win32_ComputerSystem
$totalMemory = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
Write-Host "MEMORY INFORMATION" -ForegroundColor Blue
Write-Host "Total Memory: $totalMemory GB"

$osMemory = Get-WmiObject Win32_OperatingSystem
$freeMemory = [math]::Round($osMemory.FreePhysicalMemory / 1MB, 2)
$usedMemory = [math]::Round($totalMemory - $freeMemory, 2)
Write-Host "Used Memory: $usedMemory GB"
Write-Host "Free Memory: $freeMemory GB"
Write-Host ""

# CPU Usage
Write-Host "CPU USAGE" -ForegroundColor Green
try {
    $cpuUsage = Get-WmiObject -Class win32_processor | Measure-Object -Property LoadPercentage -Average | Select-Object -ExpandProperty Average
    Write-Host "Total CPU: $cpuUsage percent"
    
    # Individual cores
    $cpuCores = Get-Counter "\Processor(*)\% Processor Time" -ErrorAction SilentlyContinue
    if ($cpuCores) {
        Write-Host "CPU Cores:"
        $coreNumber = 0
        foreach ($core in $cpuCores.CounterSamples | Where-Object {$_.InstanceName -ne "_total"}) {
            $usage = [math]::Round($core.CookedValue, 1)
            Write-Host "  Core $coreNumber : $usage percent"
            $coreNumber++
            if ($coreNumber -ge 8) { break }
        }
    }
} catch {
    Write-Host "CPU usage data not available"
}
Write-Host ""

# GPU Information
Write-Host "GPU INFORMATION" -ForegroundColor Magenta
$gpus = Get-WmiObject Win32_VideoController | Where-Object {$_.Name -notlike "*Basic*"}
foreach ($gpu in $gpus) {
    Write-Host "GPU: $($gpu.Name)"
    if ($gpu.AdapterRAM) {
        $gpuMemory = [math]::Round($gpu.AdapterRAM / 1GB, 2)
        Write-Host "VRAM: $gpuMemory GB"
    }
}
Write-Host ""

# Temperature Information
Write-Host "TEMPERATURE INFORMATION" -ForegroundColor Red
try {
    $temps = Get-WmiObject -Namespace "root\WMI" -Class "MSAcpi_ThermalZoneTemperature" -ErrorAction SilentlyContinue
    if ($temps) {
        foreach ($temp in $temps) {
            $celsius = [math]::Round(($temp.CurrentTemperature - 2732) / 10, 1)
            Write-Host "Thermal Zone: $celsius C"
        }
    } else {
        Write-Host "Temperature data not available (requires admin)"
    }
} catch {
    Write-Host "Temperature data not available"
}
Write-Host ""

# Fan Information
Write-Host "FAN INFORMATION" -ForegroundColor Cyan
try {
    $fans = Get-WmiObject Win32_Fan -ErrorAction SilentlyContinue
    if ($fans) {
        foreach ($fan in $fans) {
            Write-Host "Fan: $($fan.Name) - Status: $($fan.Status)"
            if ($fan.DesiredSpeed) {
                Write-Host "  Speed: $($fan.DesiredSpeed) RPM"
            }
        }
    } else {
        Write-Host "Fan data not available via WMI"
    }
} catch {
    Write-Host "Fan data not available"
}
Write-Host ""

# Disk Information
Write-Host "DISK INFORMATION" -ForegroundColor Yellow
$disks = Get-WmiObject Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
foreach ($disk in $disks) {
    $totalSize = [math]::Round($disk.Size / 1GB, 2)
    $freeSpace = [math]::Round($disk.FreeSpace / 1GB, 2)
    $usedSpace = [math]::Round($totalSize - $freeSpace, 2)
    $diskPercent = [math]::Round(($usedSpace / $totalSize) * 100, 1)
    
    Write-Host "Disk $($disk.DeviceID) - $totalSize GB"
    Write-Host "  Used: $usedSpace GB ($diskPercent percent)"
    Write-Host "  Free: $freeSpace GB"
}
Write-Host ""

Write-Host "System monitoring complete!" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit" 