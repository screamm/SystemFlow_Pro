# Enkel SystemFlow Pro Icon Generator
Write-Host "Skapar ny ikon för SystemFlow Pro..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing

# Skapa en 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Skapa gradient bakgrund (radial från centrum)
$centerPoint = New-Object System.Drawing.PointF(128, 128)
$brush = New-Object System.Drawing.Drawing2D.PathGradientBrush(@($centerPoint))
$brush.CenterColor = [System.Drawing.Color]::FromArgb(255, 0, 212, 170)    # Cyan
$brush.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 26, 26, 46))  # Mörk lila

# Fyll bakgrund
$rect = New-Object System.Drawing.Rectangle(0, 0, 256, 256)
$graphics.FillRectangle($brush, $rect)

# Skapa font för "S"
$font = New-Object System.Drawing.Font("Arial", 160, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Mät textstorleken för centrering
$textSize = $graphics.MeasureString("S", $font)
$x = (256 - $textSize.Width) / 2
$y = (256 - $textSize.Height) / 2

# Rita "S" med skugga
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$graphics.DrawString("S", $font, $shadowBrush, $x + 3, $y + 3)

# Rita huvudtexten (vitt S)
$graphics.DrawString("S", $font, $whiteBrush, $x, $y)

# Spara som PNG
$pngPath = "$PWD\new_app_icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$font.Dispose()
$whiteBrush.Dispose()
$shadowBrush.Dispose()

Write-Host "PNG-ikon skapad: $pngPath" -ForegroundColor Green

# Säkerhetskopiera gamla ikonen
if (Test-Path "app.ico") {
    Copy-Item "app.ico" "app_backup.ico"
    Write-Host "Säkerhetskopiera av gamla ikonen: app_backup.ico" -ForegroundColor Yellow
}

# Kopiera PNG som ICO (Windows accepterar ofta PNG med .ico-ändelse)
Copy-Item $pngPath "app.ico"
Write-Host "Ny ikon kopierad som app.ico" -ForegroundColor Green

Write-Host "Bygger om projektet..." -ForegroundColor Yellow

# Bygga projektet
try {
    & "C:\Program Files\dotnet\dotnet.exe" clean SystemMonitorApp.csproj
    & "C:\Program Files\dotnet\dotnet.exe" build SystemMonitorApp.csproj
    Write-Host "Projektet har byggts om framgångsrikt!" -ForegroundColor Green
    Write-Host "Den nya ikonen med vitt 'S' är nu aktiv!" -ForegroundColor Cyan
} catch {
    Write-Host "Fel vid byggning: $($_.Exception.Message)" -ForegroundColor Red
    
    # Försök hitta dotnet
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetPath) {
        Write-Host "Försöker med systemets dotnet..." -ForegroundColor Yellow
        & $dotnetPath.Source clean SystemMonitorApp.csproj
        & $dotnetPath.Source build SystemMonitorApp.csproj
        Write-Host "Projektet har byggts om framgångsrikt!" -ForegroundColor Green
    } else {
        Write-Host "Kunde inte hitta dotnet. Kör manuellt: dotnet build SystemMonitorApp.csproj" -ForegroundColor Red
    }
} 