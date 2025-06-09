# Förbättra den befintliga turkosa ikonen
Write-Host "Skapar förbättrad SystemFlow Pro ikon..." -ForegroundColor Green

Add-Type -AssemblyName System.Drawing

# Skapa en högkvalitativ 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Aktivera antialiasing för snygg rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

# Skapa en rundad bakgrund med gradient (mörk till turkos)
$rect = New-Object System.Drawing.Rectangle(20, 20, 216, 216)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 30, 40, 50),     # Mörkgrå
    [System.Drawing.Color]::FromArgb(255, 0, 150, 140),    # Turkos
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

# Lägg till glans-effekt
$gloss = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Rectangle(20, 20, 216, 108)),
    [System.Drawing.Color]::FromArgb(60, 255, 255, 255),
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

# Rita skugga för djup
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 0, 0, 0))
$graphics.DrawString($text, $font, $shadowBrush, $x + 3, $y + 3)

# Rita huvudtext (vitt S)
$graphics.DrawString($text, $font, $whiteBrush, $x, $y)

# Lägg till ljus kant för extra pop
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, 255, 255, 255), 2)
$graphics.DrawPath($pen, $path)

# Spara som PNG
$pngPath = "$PWD\improved_icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Förbättrad ikon skapad: $pngPath" -ForegroundColor Green

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
Write-Host "Nu kan du:" -ForegroundColor Cyan
Write-Host "1. Öppna improved_icon.png för att se den förbättrade versionen" -ForegroundColor White
Write-Host "2. Om du gillar den, konvertera den till ICO på: https://www.icoconverter.com/" -ForegroundColor White
Write-Host "3. Byt namn till app.ico och ersätt den nuvarande" -ForegroundColor White
Write-Host "4. Bygg om projektet" -ForegroundColor White
Write-Host ""
Write-Host "Eller vill du att jag justerar färgerna/designen?" -ForegroundColor Yellow 