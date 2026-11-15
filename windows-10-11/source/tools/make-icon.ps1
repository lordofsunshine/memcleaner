param(
    [string]$Source = "src/MemCleaner/Assets/app-logo.png",
    [string]$Output = "src/MemCleaner/Assets/app.ico",
    [string]$PngOutput = "src/MemCleaner/Assets/app.png"
)

Add-Type -AssemblyName System.Drawing

$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $Source).Path)
$sizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256)

function New-Scaled([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($sourceImage, 0, 0, $size, $size)
    $g.Dispose()
    return $bmp
}

$images = @()
foreach ($size in $sizes) {
    $images += , (New-Scaled $size)
}

$dir = Split-Path -Parent $Output
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$pngDir = Split-Path -Parent $PngOutput
if (-not (Test-Path $pngDir)) { New-Item -ItemType Directory -Path $pngDir -Force | Out-Null }

$png = New-Scaled 512
$png.Save((Join-Path (Resolve-Path -LiteralPath $pngDir).Path ([System.IO.Path]::GetFileName($PngOutput))), [System.Drawing.Imaging.ImageFormat]::Png)
$png.Dispose()

$tempFiles = @()
$payloads = @()
foreach ($image in $images) {
    $target = Join-Path ([System.IO.Path]::GetTempPath()) ("mc_" + $image.Width + "_" + [Guid]::NewGuid().ToString("N") + ".png")
    $image.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
    $tempFiles += $target
    $payloads += , [System.IO.File]::ReadAllBytes($target)
}

$targetIco = Join-Path (Resolve-Path -LiteralPath $dir).Path ([System.IO.Path]::GetFileName($Output))
$fs = [System.IO.File]::Create($targetIco)
$writer = New-Object System.IO.BinaryWriter($fs)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
$index = 0
foreach ($image in $images) {
    $bytes = $payloads[$index]
    $dimension = if ($image.Width -ge 256) { 0 } else { $image.Width }
    $writer.Write([Byte]$dimension)
    $writer.Write([Byte]$dimension)
    $writer.Write([Byte]0)
    $writer.Write([Byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$bytes.Length)
    $writer.Write([UInt32]$offset)
    $offset += $bytes.Length
    $index++
}

foreach ($bytes in $payloads) {
    $writer.Write($bytes)
}

$writer.Flush()
$writer.Close()
$fs.Close()

foreach ($image in $images) { $image.Dispose() }
$sourceImage.Dispose()
foreach ($file in $tempFiles) { Remove-Item $file -ErrorAction SilentlyContinue }

Write-Output "icon written"
