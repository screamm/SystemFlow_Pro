# Ladda ner och skapa riktig ICO-fil
Write-Host "Skapar riktig ICO-fil för SystemFlow Pro..." -ForegroundColor Green

# Använd HTML-filen för att skapa en PNG först i kod
Add-Type -AssemblyName System.Drawing

# Skapa en enkel och tydlig ikon
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Skapa gradient bakgrund
$rect = New-Object System.Drawing.Rectangle(0, 0, 256, 256)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 0, 150, 136),    # Teal
    [System.Drawing.Color]::FromArgb(255, 63, 81, 181),    # Indigo
    45.0
)

# Fyll bakgrund
$graphics.FillRectangle($brush, $rect)

# Skapa font för "S"
$font = New-Object System.Drawing.Font("Arial", 180, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Centrera S
$text = "S"
$textSize = $graphics.MeasureString($text, $font)
$x = (256 - $textSize.Width) / 2
$y = (256 - $textSize.Height) / 2

# Rita skugga
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 0, 0, 0))
$graphics.DrawString($text, $font, $shadowBrush, $x + 4, $y + 4)

# Rita vitt S
$graphics.DrawString($text, $font, $whiteBrush, $x, $y)

# Spara som PNG
$pngPath = "$PWD\icon_source.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "PNG-källa skapad: $pngPath" -ForegroundColor Green

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$font.Dispose()
$whiteBrush.Dispose()
$shadowBrush.Dispose()

# Säkerhetskopiera gamla ikonen
if (Test-Path "app.ico") {
    Copy-Item "app.ico" "app_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').ico"
    Write-Host "Säkerhetskopiera av gamla ikonen gjord" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Alternativ 1: Använd online-konverterare" -ForegroundColor Cyan
Write-Host "1. Gå till https://convertio.co/png-ico/" -ForegroundColor White
Write-Host "2. Ladda upp icon_source.png" -ForegroundColor White
Write-Host "3. Konvertera till ICO" -ForegroundColor White  
Write-Host "4. Ladda ner och byt namn till app.ico" -ForegroundColor White
Write-Host ""

Write-Host "Alternativ 2: Använd ImageMagick" -ForegroundColor Cyan
Write-Host "Försöker ladda ner ImageMagick..." -ForegroundColor Yellow

try {
    # Försök använda chocolatey om det finns
    if (Get-Command choco -ErrorAction SilentlyContinue) {
        Write-Host "Installerar ImageMagick via Chocolatey..." -ForegroundColor Yellow
        & choco install imagemagick -y
        
        # Konvertera med ImageMagick
        & magick "$pngPath" -resize 256x256 "app.ico"
        Write-Host "ICO-fil skapad med ImageMagick!" -ForegroundColor Green
        $iconCreated = $true
    }
} catch {
    Write-Host "ImageMagick inte tillgängligt" -ForegroundColor Yellow
    $iconCreated = $false
}

if (-not $iconCreated) {
    Write-Host ""
    Write-Host "INSTRUKTIONER:" -ForegroundColor Red
    Write-Host "1. Använd icon_source.png som skapats" -ForegroundColor White
    Write-Host "2. Gå till https://www.icoconverter.com/" -ForegroundColor White
    Write-Host "3. Ladda upp icon_source.png" -ForegroundColor White
    Write-Host "4. Välj 'Convert to ICO'" -ForegroundColor White
    Write-Host "5. Ladda ner som app.ico till denna mapp" -ForegroundColor White
    Write-Host "6. Kör sedan: dotnet publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish-final" -ForegroundColor White
}

Write-Host ""
Write-Host "PNG-filen är redo i: $pngPath" -ForegroundColor Green 