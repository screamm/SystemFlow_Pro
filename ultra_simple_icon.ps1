# Ultra enkel ikon-skapare
Write-Host "Skapar ultra enkel ikon..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing

# Skapa bitmap 64x64 (mindre för att undvika problem)
$bitmap = New-Object System.Drawing.Bitmap(64, 64)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enkel blå bakgrund
$blueBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::DarkBlue)
$graphics.FillRectangle($blueBrush, 0, 0, 64, 64)

# Vitt S
$font = New-Object System.Drawing.Font("Arial", 36, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Centrera S
$text = "S"
$textSize = $graphics.MeasureString($text, $font)
$x = (64 - $textSize.Width) / 2
$y = (64 - $textSize.Height) / 2

$graphics.DrawString($text, $font, $whiteBrush, $x, $y)

# Spara som BMP först
$bmpPath = "$PWD\simple_icon.bmp"
$bitmap.Save($bmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)

Write-Host "BMP skapad: $bmpPath" -ForegroundColor Green

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$blueBrush.Dispose()
$whiteBrush.Dispose()
$font.Dispose()

# Säkerhetskopiera gamla ikonen
if (Test-Path "app.ico") {
    Move-Item "app.ico" "app_old.ico"
    Write-Host "Gamla ikonen flyttad till app_old.ico" -ForegroundColor Yellow
}

# Skapa en mycket enkel ICO-fil manuellt
$icoData = [System.Collections.Generic.List[byte]]::new()

# ICO header (6 bytes)
$icoData.AddRange([byte[]](0, 0, 1, 0, 1, 0))  # Signature, Type, Count

# Image directory (16 bytes)
$icoData.AddRange([byte[]](64, 64, 0, 0, 1, 0, 32, 0))  # Width, Height, Colors, Reserved, Planes, BitCount
$icoData.AddRange([byte[]](0, 0, 0, 0, 22, 0, 0, 0))    # Size (placeholder), Offset

# Läs BMP data
$bmpBytes = [System.IO.File]::ReadAllBytes($bmpPath)

# Uppdatera size i directory
$bmpSize = $bmpBytes.Length
$icoData[14] = $bmpSize -band 0xFF
$icoData[15] = ($bmpSize -shr 8) -band 0xFF
$icoData[16] = ($bmpSize -shr 16) -band 0xFF
$icoData[17] = ($bmpSize -shr 24) -band 0xFF

# Lägg till BMP data
$icoData.AddRange($bmpBytes)

# Spara som ICO
[System.IO.File]::WriteAllBytes("$PWD\app.ico", $icoData.ToArray())

Write-Host "ICO-fil skapad som app.ico" -ForegroundColor Green

# Ta bort temp BMP
Remove-Item $bmpPath -Force

Write-Host "Bygger om projektet..." -ForegroundColor Yellow

# Bygga projektet
& "C:\Program Files\dotnet\dotnet.exe" clean SystemMonitorApp.csproj
& "C:\Program Files\dotnet\dotnet.exe" build SystemMonitorApp.csproj

Write-Host "Klart! Ny ikon med vitt S är nu aktiv!" -ForegroundColor Green 