# Skapa ren turkos ikon utan gradient
Write-Host "Skapar ren turkos SystemFlow Pro ikon..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing

# Skapa en högkvalitativ 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing för snygg rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

# Skapa rundad bakgrund med ren turkos färg
$rect = New-Object System.Drawing.Rectangle(20, 20, 216, 216)

# Ren turkos #00e5c7
$turkosBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 229, 199))

# Skapa rundad rektangel för bakgrund
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$radius = 30
$path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
$path.AddArc($rect.Right - $radius, $rect.Y, $radius, $radius, 270, 90)
$path.AddArc($rect.Right - $radius, $rect.Bottom - $radius, $radius, $radius, 0, 90)
$path.AddArc($rect.X, $rect.Bottom - $radius, $radius, $radius, 90, 90)
$path.CloseFigure()

# Fyll bakgrund med ren turkos
$graphics.FillPath($turkosBrush, $path)

# Skapa "S" med modern font
$font = New-Object System.Drawing.Font("Segoe UI", 140, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Centrera "S"
$text = "S"
$textSize = $graphics.MeasureString($text, $font)
$x = (256 - $textSize.Width) / 2
$y = (256 - $textSize.Height) / 2 + 5

# Rita subtil skugga för djup
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60, 0, 0, 0))
$graphics.DrawString($text, $font, $shadowBrush, $x + 2, $y + 2)

# Rita huvudtext (vitt S)
$graphics.DrawString($text, $font, $whiteBrush, $x, $y)

# Spara som PNG
$pngPath = "$PWD\clean_turkos_icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Ren turkos ikon skapad: $pngPath" -ForegroundColor Green

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$turkosBrush.Dispose()
$font.Dispose()
$whiteBrush.Dispose()
$shadowBrush.Dispose()
$path.Dispose()

Write-Host ""
Write-Host "Clean turkos #00e5c7 ikon är klar!" -ForegroundColor Cyan

# Konvertera direkt till ICO och ersätt
Write-Host "Konverterar till ICO och ersätter app.ico..." -ForegroundColor Yellow

try {
    # Ladda PNG
    $image = [System.Drawing.Image]::FromFile($pngPath)
    
    # Skapa ICO från PNG
    $icon = [System.Drawing.Icon]::FromHandle($image.GetHicon())
    
    # Spara som ICO
    $iconStream = New-Object System.IO.FileStream("$PWD\clean_turkos.ico", [System.IO.FileMode]::Create)
    $icon.Save($iconStream)
    $iconStream.Close()
    
    # Ersätt app.ico
    Copy-Item "clean_turkos.ico" "app.ico"
    
    Write-Host "ICO skapad och app.ico ersatt!" -ForegroundColor Green
    
    # Cleanup
    $image.Dispose()
    $icon.Dispose()
    
} catch {
    Write-Host "Konvertering misslyckades: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Bygger ny .exe med turkos ikon..." -ForegroundColor Yellow 