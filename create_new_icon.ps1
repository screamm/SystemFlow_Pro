# SystemFlow Pro Icon Generator och Builder
Write-Host "Skapar ny ikon för SystemFlow Pro..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# Skapa en 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing för snygg text
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Skapa gradient bakgrund
$rect = New-Object System.Drawing.Rectangle(0, 0, 256, 256)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 0, 212, 170),    # Cyan
    [System.Drawing.Color]::FromArgb(255, 26, 26, 46),     # Mörk lila
    45.0
)

# Lägg till mellanfärg för gradient
$colorBlend = New-Object System.Drawing.Drawing2D.ColorBlend
$colorBlend.Colors = @(
    [System.Drawing.Color]::FromArgb(255, 0, 212, 170),    # Cyan
    [System.Drawing.Color]::FromArgb(255, 168, 85, 247),   # Lila
    [System.Drawing.Color]::FromArgb(255, 26, 26, 46)      # Mörk lila
)
$colorBlend.Positions = @(0.0, 0.7, 1.0)
$brush.InterpolationColors = $colorBlend

# Fyll bakgrund
$graphics.FillRectangle($brush, $rect)

# Skapa font för "S"
$font = New-Object System.Drawing.Font("Segoe UI", 140, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Mät textstorleken för centrering
$textSize = $graphics.MeasureString("S", $font)
$x = (256 - $textSize.Width) / 2
$y = (256 - $textSize.Height) / 2

# Rita "S" med skugga för glödeffekt
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 0, 212, 170))
$graphics.DrawString("S", $font, $shadowBrush, $x + 2, $y + 2)
$graphics.DrawString("S", $font, $shadowBrush, $x - 2, $y - 2)
$graphics.DrawString("S", $font, $shadowBrush, $x + 2, $y - 2)
$graphics.DrawString("S", $font, $shadowBrush, $x - 2, $y + 2)

# Rita huvudtexten
$graphics.DrawString("S", $font, $whiteBrush, $x, $y)

# Spara som PNG först
$bitmap.Save("temp_icon.png", [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$font.Dispose()
$whiteBrush.Dispose()
$shadowBrush.Dispose()

Write-Host "PNG-ikon skapad, konverterar till ICO..." -ForegroundColor Yellow

# Konvertera PNG till ICO med PowerShell
Add-Type -AssemblyName System.Drawing

$pngImage = [System.Drawing.Image]::FromFile("$PWD\temp_icon.png")

# Skapa ICO-fil
$iconSizes = @(256, 128, 64, 48, 32, 16)
$iconStream = New-Object System.IO.MemoryStream

# ICO header
$iconHeader = [byte[]](0, 0, 1, 0, $iconSizes.Count, 0)
$iconStream.Write($iconHeader, 0, $iconHeader.Length)

$imageDataOffset = 6 + ($iconSizes.Count * 16)
$imageStreams = @()

foreach ($size in $iconSizes) {
    $resizedBitmap = New-Object System.Drawing.Bitmap($pngImage, $size, $size)
    $pngStream = New-Object System.IO.MemoryStream
    $resizedBitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageStreams += $pngStream.ToArray()
    
    # Directory entry
    $entry = [byte[]]@(
        $size,                           # Width
        $size,                           # Height  
        0,                               # Colors
        0,                               # Reserved
        1, 0,                           # Planes
        32, 0,                          # Bits per pixel
        $pngStream.Length -band 0xFF,    # Size (low)
        ($pngStream.Length -shr 8) -band 0xFF,
        ($pngStream.Length -shr 16) -band 0xFF,
        ($pngStream.Length -shr 24) -band 0xFF,
        $imageDataOffset -band 0xFF,     # Offset (low)
        ($imageDataOffset -shr 8) -band 0xFF,
        ($imageDataOffset -shr 16) -band 0xFF,
        ($imageDataOffset -shr 24) -band 0xFF
    )
    
    $iconStream.Write($entry, 0, $entry.Length)
    $imageDataOffset += $pngStream.Length
    
    $resizedBitmap.Dispose()
    $pngStream.Dispose()
}

# Skriv bilddata
foreach ($imageData in $imageStreams) {
    $iconStream.Write($imageData, 0, $imageData.Length)
}

# Spara ICO-fil
[System.IO.File]::WriteAllBytes("$PWD\app.ico", $iconStream.ToArray())

$iconStream.Dispose()
$pngImage.Dispose()

# Ta bort temporär PNG
Remove-Item "temp_icon.png" -Force

Write-Host "Ny ikon skapad som app.ico!" -ForegroundColor Green
Write-Host "Bygger om projektet..." -ForegroundColor Yellow

# Bygga projektet enligt minnet
try {
    & "C:\Program Files\dotnet\dotnet.exe" build SystemMonitorApp.csproj
    Write-Host "Projektet har byggts om framgångsrikt!" -ForegroundColor Green
    Write-Host "Den nya ikonen med vitt 'S' är nu aktiv!" -ForegroundColor Cyan
} catch {
    Write-Host "Fel vid byggning: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Försöker med alternativ sökväg..." -ForegroundColor Yellow
    
    # Försök hitta dotnet
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetPath) {
        & $dotnetPath.Source build SystemMonitorApp.csproj
        Write-Host "Projektet har byggts om framgångsrikt!" -ForegroundColor Green
    } else {
        Write-Host "Kunde inte hitta dotnet. Kör manuellt: dotnet build SystemMonitorApp.csproj" -ForegroundColor Red
    }
} 