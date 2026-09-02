[CmdletBinding()]
param(
    [string]$UnityEditor
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') -TotalCount 1) -replace '^m_EditorVersion:\s*', ''

if (-not $UnityEditor) {
    $key = "HKLM:\SOFTWARE\Unity Technologies\Installer\Unity $version"
    if (Test-Path -LiteralPath $key) {
        $UnityEditor = Join-Path (Get-ItemPropertyValue -LiteralPath $key -Name 'Location x64') 'Editor\Unity.exe'
    }
}

if (-not $UnityEditor -or -not (Test-Path -LiteralPath $UnityEditor)) {
    throw "Unity $version was not found. Install it in Unity Hub or pass -UnityEditor <path-to-Unity.exe>."
}

$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$log = Join-Path $logDirectory 'build-windows.log'

$arguments = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', "`"$projectRoot`"",
    '-executeMethod', 'MiracleDiamond.Editor.ProjectSetup.BuildWindows',
    '-logFile', "`"$log`""
)
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()

if ($process.ExitCode -ne 0) {
    throw "Unity build failed with exit code $($process.ExitCode). See $log"
}

$executable = Join-Path $projectRoot 'Builds\Windows\MiracleDiamond.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Unity reported success but the player was not found at $executable. See $log"
}

Write-Output "Windows build ready: $executable"
