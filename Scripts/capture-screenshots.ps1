[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$player = Join-Path $projectRoot 'Builds\Windows\MiracleDiamond.exe'
$output = Join-Path $projectRoot 'Marketing\Screenshots'

if (-not (Test-Path -LiteralPath $player)) {
    throw "Windows player not found. Run .\Scripts\build-windows.ps1 first."
}

New-Item -ItemType Directory -Force -Path $output | Out-Null
$arguments = @(
    '-screen-width', '1920',
    '-screen-height', '1080',
    '-screen-fullscreen', '0',
    '-captureStoreScreens', "`"$output`""
)
$process = Start-Process -FilePath $player -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    throw "Screenshot player exited with code $($process.ExitCode)."
}

$expected = @(
    'qa_01_title.png', 'qa_02_how_to.png', 'qa_03_results.png',
    'store_01_pitching.png', 'store_02_special_pitch.png', 'store_03_batting.png',
    'store_04_special_bat.png', 'store_05_moon_stadium.png'
)
Add-Type -AssemblyName System.Drawing
foreach ($name in $expected) {
    $path = Join-Path $output $name
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected screenshot missing: $name"
    }
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        if ($image.Width -ne 1920 -or $image.Height -ne 1080) {
            throw "Unexpected screenshot size for ${name}: $($image.Width)x$($image.Height)"
        }
    }
    finally {
        $image.Dispose()
    }
}

Write-Output "Screenshots ready: $output"
