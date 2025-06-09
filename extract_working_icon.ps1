# Extrahera den fungerande ikonen från publish-with-icon versionen
Write-Host "Extraherar fungerande ikon från SystemMonitorApp.exe..." -ForegroundColor Green

# Använd PowerShell för att extrahera ikon från .exe
Add-Type -AssemblyName System.Drawing

try {
    # Extrahera ikon från den fungerande .exe-filen
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon("$PWD\publish-with-icon\SystemMonitorApp.exe")
    
    if ($icon) {
        # Konvertera till bitmap och spara som PNG först
        $bitmap = $icon.ToBitmap()
        $pngPath = "$PWD\extracted_working_icon.png"
        $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "Fungerande ikon extraherad som PNG: $pngPath" -ForegroundColor Green
        
        # Spara även som ICO direkt
        $iconStream = New-Object System.IO.FileStream("$PWD\extracted_working_icon.ico", [System.IO.FileMode]::Create)
        $icon.Save($iconStream)
        $iconStream.Close()
        
        Write-Host "Fungerande ikon extraherad som ICO: extracted_working_icon.ico" -ForegroundColor Green
        
        # Säkerhetskopiera nuvarande app.ico
        if (Test-Path "app.ico") {
            Copy-Item "app.ico" "app_broken_$(Get-Date -Format 'yyyyMMdd_HHmmss').ico"
            Write-Host "Nuvarande (trasiga) ikon säkerhetskopierad" -ForegroundColor Yellow
        }
        
        # Kopiera den fungerande ikonen
        Copy-Item "extracted_working_icon.ico" "app.ico"
        Write-Host "Fungerande ikon återställd som app.ico!" -ForegroundColor Green
        
        # Cleanup
        $bitmap.Dispose()
        $icon.Dispose()
        
        Write-Host ""
        Write-Host "FRAMGÅNG! Den fungerande gröna ikonen är nu återställd." -ForegroundColor Cyan
        Write-Host "Nu kan du bygga en ny .exe med den fungerande ikonen:" -ForegroundColor White
        Write-Host "dotnet publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish-restored-icon" -ForegroundColor Yellow
        
    } else {
        Write-Host "Kunde inte extrahera ikon från .exe-filen" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Fel vid extraktion: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternativ: Kopiera från säkerhetskopia..." -ForegroundColor Yellow
    
    # Försök med säkerhetskopia
    if (Test-Path "app_backup.ico") {
        Copy-Item "app_backup.ico" "app.ico"
        Write-Host "Kopierade från app_backup.ico" -ForegroundColor Green
    }
} 