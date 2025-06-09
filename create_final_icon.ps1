# Skapa final ikon med mjuk gradient och #00e5c7
Write-Host "Skapar final SystemFlow Pro ikon med #00e5c7 gradient..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing

# Skapa en högkvalitativ 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing för snygg rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

# Skapa en rundad bakgrund med subtil gradient
$rect = New-Object System.Drawing.Rectangle(20, 20, 216, 216)

# Mjuk gradient från något mörkare #00e5c7 till ljusare
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 0, 190, 160),    # Mörkare version av #00e5c7
    [System.Drawing.Color]::FromArgb(255, 0, 229, 199),    # #00e5c7
    45.0
)

# Skapa rundad rektangel för bakgrund
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$radius = 30
$path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
$path.AddArc($rect.Right - $radius, $rect.Y, $radius, $radius, 270, 90)
$path.AddArc($rect.Right - $radius, $rect.Bottom - $radius, $radius, $radius, 0, 90)
$path.AddArc($rect.X, $rect.Bottom - $radius, $radius, $radius, 90, 90)
$path.CloseFigure()

# Fyll bakgrund
$graphics.FillPath($brush, $path)

# Lägg till mycket subtil glans-effekt
$gloss = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Rectangle(20, 20, 216, 100)),
    [System.Drawing.Color]::FromArgb(40, 255, 255, 255),   # Mer transparent
    [System.Drawing.Color]::FromArgb(0, 255, 255, 255),
    90.0
)
$graphics.FillPath($gloss, $path)

# Skapa "S" med modern font
$font = New-Object System.Drawing.Font("Segoe UI", 140, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Centrera "S"
$text = "S"
$textSize = $graphics.MeasureString($text, $font)
$x = (256 - $textSize.Width) / 2
$y = (256 - $textSize.Height) / 2 + 5

# Rita subtil skugga för djup
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$graphics.DrawString($text, $font, $shadowBrush, $x + 2, $y + 2)

# Rita huvudtext (vitt S)
$graphics.DrawString($text, $font, $whiteBrush, $x, $y)

# Lägg till mjuk kant
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(100, 255, 255, 255), 1.5)
$graphics.DrawPath($pen, $path)

# Spara som PNG
$pngPath = "$PWD\final_icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Final ikon skapad med #00e5c7 gradient: $pngPath" -ForegroundColor Green

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$gloss.Dispose()
$font.Dispose()
$whiteBrush.Dispose()
$shadowBrush.Dispose()
$pen.Dispose()
$path.Dispose()

Write-Host ""
Write-Host "Mjuk gradient med #00e5c7 färg är klar!" -ForegroundColor Cyan
Write-Host "Öppnar för preview..." -ForegroundColor Yellow

# Öppna för att visa
Start-Process -FilePath $pngPath

Write-Host ""
Write-Host "Om du gillar den, konverterar jag den direkt till ICO och bygger ny .exe!" -ForegroundColor Green 