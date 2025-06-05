# System Monitor för Windows 11 - PowerShell Version
# Kör som administratör för bästa resultat

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Get-SystemInfo {
    Write-Host "🖥️ SYSTEM MONITOR FÖR WINDOWS 11" -ForegroundColor Cyan
    Write-Host "=================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Grundläggande systeminfo
    Write-Host "💻 SYSTEMINFORMATION" -ForegroundColor Green
    Write-Host "Datornamn: $env:COMPUTERNAME"
    Write-Host "OS: $((Get-WmiObject Win32_OperatingSystem).Caption)"
    Write-Host "Arkitektur: $((Get-WmiObject Win32_OperatingSystem).OSArchitecture)"
    Write-Host "Anvandare: $env:USERNAME"
    Write-Host ""
    
    # CPU Information
    $cpu = Get-WmiObject Win32_Processor
    Write-Host "🔧 CPU INFORMATION" -ForegroundColor Yellow
    Write-Host "Processor: $($cpu.Name)"
    Write-Host "Karnor: $($cpu.NumberOfCores)"
    Write-Host "Logiska processorer: $($cpu.NumberOfLogicalProcessors)"
    Write-Host "Max Clock Speed: $($cpu.MaxClockSpeed) MHz"
    Write-Host ""
    
    # Minne
    $memory = Get-WmiObject Win32_ComputerSystem
    $totalMemory = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
    Write-Host "🧠 MINNESINFORMATION" -ForegroundColor Blue
    Write-Host "Totalt minne: $totalMemory GB"
    Write-Host ""
}

function Get-RealtimeStats {
    while ($true) {
        Clear-Host
        
        # Header
        Write-Host "🖥️ SYSTEM MONITOR - REALTIDSDATA" -ForegroundColor Cyan
        Write-Host "=================================" -ForegroundColor Cyan
        Write-Host "Tryck Ctrl+C för att avsluta" -ForegroundColor Red
        Write-Host ""
        
        # CPU Användning
        Write-Host "💻 CPU ANVÄNDNING" -ForegroundColor Green
        $cpuUsage = Get-WmiObject -Class win32_processor | Measure-Object -Property LoadPercentage -Average | Select-Object -ExpandProperty Average
        Write-Host "Total CPU: $cpuUsage%" -ForegroundColor Green
        
        # Individuella karnor
        $cpuCores = Get-Counter "\Processor(*)\% Processor Time" -ErrorAction SilentlyContinue
        if ($cpuCores) {
            Write-Host "CPU Karnor:" -ForegroundColor Green
            $coreNumber = 0
            foreach ($core in $cpuCores.CounterSamples | Where-Object {$_.InstanceName -ne "_total"}) {
                $usage = [math]::Round($core.CookedValue, 1)
                Write-Host "  Core $coreNumber : $usage%" -ForegroundColor Green
                $coreNumber++
                if ($coreNumber -ge 8) { break } # Begränsa utskrift för stora system
            }
        }
        Write-Host ""
        
        # Minnesanvändning
        Write-Host "🧠 MINNESANVÄNDNING" -ForegroundColor Blue
        $memory = Get-WmiObject Win32_OperatingSystem
        $totalMemory = [math]::Round($memory.TotalVisibleMemorySize / 1MB, 2)
        $freeMemory = [math]::Round($memory.FreePhysicalMemory / 1MB, 2)
        $usedMemory = [math]::Round($totalMemory - $freeMemory, 2)
        $memoryPercent = [math]::Round(($usedMemory / $totalMemory) * 100, 1)
        
        Write-Host "Totalt: $totalMemory GB"
        Write-Host "Anvant: $usedMemory GB ($memoryPercent%)" -ForegroundColor Blue
        Write-Host "Ledigt: $freeMemory GB" -ForegroundColor Blue
        Write-Host ""
        
        # GPU Information
        Write-Host "🎮 GPU INFORMATION" -ForegroundColor Magenta
        $gpus = Get-WmiObject Win32_VideoController | Where-Object {$_.Name -notlike "*Basic*"}
        foreach ($gpu in $gpus) {
            Write-Host "GPU: $($gpu.Name)" -ForegroundColor Magenta
            if ($gpu.AdapterRAM) {
                $gpuMemory = [math]::Round($gpu.AdapterRAM / 1GB, 2)
                Write-Host "VRAM: $gpuMemory GB" -ForegroundColor Magenta
            }
        }
        Write-Host ""
        
        # Temperaturer
        Write-Host "🌡️ TEMPERATURER" -ForegroundColor Red
        try {
            $temps = Get-WmiObject -Namespace "root\WMI" -Class "MSAcpi_ThermalZoneTemperature" -ErrorAction SilentlyContinue
            if ($temps) {
                foreach ($temp in $temps) {
                    $celsius = [math]::Round(($temp.CurrentTemperature - 2732) / 10, 1)
                    Write-Host "Thermal Zone: $celsius°C" -ForegroundColor Red
                }
            } else {
                Write-Host "Temperaturdata ej tillganglig (krav admin)" -ForegroundColor Red
            }
        } catch {
            Write-Host "Temperaturdata ej tillganglig" -ForegroundColor Red
        }
        Write-Host ""
        
        # Flaktar
        Write-Host "🌀 FLAKTAR" -ForegroundColor Cyan
        try {
            $fans = Get-WmiObject Win32_Fan -ErrorAction SilentlyContinue
            if ($fans) {
                foreach ($fan in $fans) {
                    Write-Host "Flakt: $($fan.Name) - Status: $($fan.Status)" -ForegroundColor Cyan
                    if ($fan.DesiredSpeed) {
                        Write-Host "  Hastighet: $($fan.DesiredSpeed) RPM" -ForegroundColor Cyan
                    }
                }
            } else {
                Write-Host "Flaktdata ej tillganglig via WMI" -ForegroundColor Cyan
            }
        } catch {
            Write-Host "Flaktdata ej tillganglig" -ForegroundColor Cyan
        }
        Write-Host ""
        
        # Disk I/O
        Write-Host "💾 DISK INFORMATION" -ForegroundColor Yellow
        $disks = Get-WmiObject Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
        foreach ($disk in $disks) {
            $totalSize = [math]::Round($disk.Size / 1GB, 2)
            $freeSpace = [math]::Round($disk.FreeSpace / 1GB, 2)
            $usedSpace = [math]::Round($totalSize - $freeSpace, 2)
            $diskPercent = [math]::Round(($usedSpace / $totalSize) * 100, 1)
            
            Write-Host "Disk $($disk.DeviceID) - $totalSize GB" -ForegroundColor Yellow
            Write-Host "  Anvant: $usedSpace GB ($diskPercent%)" -ForegroundColor Yellow
            Write-Host "  Ledigt: $freeSpace GB" -ForegroundColor Yellow
        }
        Write-Host ""
        
        Write-Host "Uppdaterad: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
        Write-Host "Nasta uppdatering om 3 sekunder..." -ForegroundColor Gray
        
        Start-Sleep -Seconds 3
    }
}

function Show-Menu {
    Clear-Host
    Write-Host "🖥️ SYSTEM MONITOR FÖR WINDOWS 11" -ForegroundColor Cyan
    Write-Host "=================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Valj ett alternativ:" -ForegroundColor White
    Write-Host ""
    Write-Host "1. Visa systeminformation" -ForegroundColor Green
    Write-Host "2. Starta realtidsövervakning" -ForegroundColor Yellow
    Write-Host "3. Avsluta" -ForegroundColor Red
    Write-Host ""
    
    $choice = Read-Host "Ange ditt val (1-3)"
    
    switch ($choice) {
        "1" {
            Clear-Host
            Get-SystemInfo
            Write-Host ""
            Read-Host "Tryck Enter for att fortsatta"
            Show-Menu
        }
        "2" {
            Get-RealtimeStats
        }
        "3" {
            Write-Host "Avslutar..." -ForegroundColor Green
            exit
        }
        default {
            Write-Host "Ogiltigt val. Försök igen." -ForegroundColor Red
            Start-Sleep -Seconds 2
            Show-Menu
        }
    }
}

# Kontrollera administratörsrättigheter
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "VARNING: Kors inte som administrator" -ForegroundColor Yellow
    Write-Host "Vissa funktioner (temperaturer, flaktar) kanske inte fungerar fullt ut." -ForegroundColor Yellow
    Write-Host "For basta resultat, hogerklicka pa PowerShell och valj 'Kor som administrator'" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Tryck Enter for att fortsatta anda"
}

# Starta programmet
Show-Menu 