[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$marketing = Join-Path $projectRoot 'Marketing'
$output = Join-Path $marketing 'Steam'
New-Item -ItemType Directory -Force -Path $output | Out-Null

$keyArt = [System.Drawing.Image]::FromFile((Join-Path $marketing 'key-art-master.png'))
$verticalArt = [System.Drawing.Image]::FromFile((Join-Path $marketing 'vertical-key-art-master.png'))
$capsule = [System.Drawing.Image]::FromFile((Join-Path $marketing 'steam-capsule-master.png'))
$logo = [System.Drawing.Image]::FromFile((Join-Path $marketing 'logo-master.png'))
$icon = [System.Drawing.Image]::FromFile((Join-Path $projectRoot 'Assets\Art\AppIcon.png'))
$navy = [System.Drawing.Color]::FromArgb(255, 7, 21, 33)

function New-Canvas([int]$width, [int]$height, [bool]$transparent) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear($(if ($transparent) { [System.Drawing.Color]::Transparent } else { $navy }))
    return [pscustomobject]@{ Bitmap = $bitmap; Graphics = $graphics }
}

function Draw-Cover($graphics, $image, [int]$width, [int]$height) {
    $scale = [Math]::Max($width / $image.Width, $height / $image.Height)
    $sourceWidth = $width / $scale
    $sourceHeight = $height / $scale
    $source = [System.Drawing.RectangleF]::new(
        [float](($image.Width - $sourceWidth) / 2),
        [float](($image.Height - $sourceHeight) / 2),
        [float]$sourceWidth,
        [float]$sourceHeight)
    $destination = [System.Drawing.RectangleF]::new(0, 0, $width, $height)
    $graphics.DrawImage($image, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Contain($graphics, $image, [System.Drawing.RectangleF]$bounds) {
    $scale = [Math]::Min($bounds.Width / $image.Width, $bounds.Height / $image.Height)
    $width = $image.Width * $scale
    $height = $image.Height * $scale
    $destination = [System.Drawing.RectangleF]::new(
        [float]($bounds.X + ($bounds.Width - $width) / 2),
        [float]($bounds.Y + ($bounds.Height - $height) / 2),
        [float]$width,
        [float]$height)
    $graphics.DrawImage($image, $destination)
}

function Save-Canvas($canvas, [string]$path, [bool]$jpeg = $false) {
    if ($jpeg) {
        $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    }
    else {
        $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Dispose()
}

function Export-Cover($image, [int]$width, [int]$height, [string]$name) {
    $canvas = New-Canvas $width $height $false
    Draw-Cover $canvas.Graphics $image $width $height
    Save-Canvas $canvas (Join-Path $output $name)
}

try {
    Export-Cover $capsule 1232 706 'store_main_1232x706.png'
    Export-Cover $capsule 920 430 'store_header_920x430.png'
    Export-Cover $capsule 920 430 'library_header_920x430.png'
    Export-Cover $keyArt 3840 1240 'library_hero_3840x1240.png'

    foreach ($spec in @(
        @{ Width = 748; Height = 896; Name = 'store_vertical_748x896.png' },
        @{ Width = 600; Height = 900; Name = 'library_capsule_600x900.png' }
    )) {
        $canvas = New-Canvas $spec.Width $spec.Height $false
        Draw-Cover $canvas.Graphics $verticalArt $spec.Width $spec.Height
        $scrim = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(175, 7, 21, 33))
        $canvas.Graphics.FillRectangle($scrim, 0, 0, $spec.Width, [int]($spec.Height * 0.39))
        $scrim.Dispose()
        Draw-Contain $canvas.Graphics $logo ([System.Drawing.RectangleF]::new(24, 18, $spec.Width - 48, $spec.Height * 0.34))
        Save-Canvas $canvas (Join-Path $output $spec.Name)
    }

    $small = New-Canvas 462 174 $false
    Draw-Cover $small.Graphics $keyArt 462 174
    $smallScrim = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(205, 7, 21, 33))
    $small.Graphics.FillRectangle($smallScrim, 0, 0, 462, 174)
    $smallScrim.Dispose()
    Draw-Contain $small.Graphics $logo ([System.Drawing.RectangleF]::new(10, 4, 442, 166))
    Save-Canvas $small (Join-Path $output 'store_small_462x174.png')

    $libraryLogo = New-Canvas 1280 640 $true
    Draw-Contain $libraryLogo.Graphics $logo ([System.Drawing.RectangleF]::new(0, 0, 1280, 640))
    Save-Canvas $libraryLogo (Join-Path $output 'library_logo_1280x640.png')

    $shortcut = New-Canvas 256 256 $true
    Draw-Contain $shortcut.Graphics $icon ([System.Drawing.RectangleF]::new(4, 4, 248, 248))
    Save-Canvas $shortcut (Join-Path $output 'shortcut_icon_256x256.png')

    $appIcon = New-Canvas 184 184 $false
    Draw-Contain $appIcon.Graphics $icon ([System.Drawing.RectangleF]::new(4, 4, 176, 176))
    Save-Canvas $appIcon (Join-Path $output 'app_icon_184x184.jpg') $true
}
finally {
    $keyArt.Dispose()
    $verticalArt.Dispose()
    $capsule.Dispose()
    $logo.Dispose()
    $icon.Dispose()
}

$expected = @{
    'app_icon_184x184.jpg' = @(184, 184)
    'library_capsule_600x900.png' = @(600, 900)
    'library_header_920x430.png' = @(920, 430)
    'library_hero_3840x1240.png' = @(3840, 1240)
    'library_logo_1280x640.png' = @(1280, 640)
    'shortcut_icon_256x256.png' = @(256, 256)
    'store_header_920x430.png' = @(920, 430)
    'store_main_1232x706.png' = @(1232, 706)
    'store_small_462x174.png' = @(462, 174)
    'store_vertical_748x896.png' = @(748, 896)
}

foreach ($entry in $expected.GetEnumerator()) {
    $image = [System.Drawing.Image]::FromFile((Join-Path $output $entry.Key))
    try {
        if ($image.Width -ne $entry.Value[0] -or $image.Height -ne $entry.Value[1]) {
            throw "Unexpected dimensions for $($entry.Key): $($image.Width)x$($image.Height)"
        }
    }
    finally {
        $image.Dispose()
    }
}

Get-ChildItem -LiteralPath $output -File | Sort-Object Name | Select-Object Name, Length
