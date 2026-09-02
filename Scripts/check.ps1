[CmdletBinding()]
param(
    [string]$UnityEditor
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    foreach ($required in @(
        'Assets\Scenes\Main.unity',
        'Assets\Resources\Fonts\NotoSansJP-Regular.otf',
        'Assets\Resources\Fonts\OFL.txt',
        'Assets\Art\AppIcon.png'
    )) {
        if (-not (Test-Path -LiteralPath $required)) { throw "Required asset missing: $required" }
    }

    & dotnet run --project 'Tools\CoreSmoke\CoreSmoke.csproj'
    if ($LASTEXITCODE -ne 0) { throw "Core smoke checks failed: $LASTEXITCODE" }

    if (-not $UnityEditor) {
        $version = (Get-Content -LiteralPath 'ProjectSettings\ProjectVersion.txt' -TotalCount 1) -replace '^m_EditorVersion:\s*', ''
        $key = "HKLM:\SOFTWARE\Unity Technologies\Installer\Unity $version"
        if (Test-Path -LiteralPath $key) {
            $UnityEditor = Join-Path (Get-ItemPropertyValue -LiteralPath $key -Name 'Location x64') 'Editor\Unity.exe'
        }
    }

    if ($UnityEditor -and (Test-Path -LiteralPath $UnityEditor)) {
        $managed = Join-Path (Split-Path -Parent $UnityEditor) 'Data\Managed'
        & dotnet build 'Tools\UnityCompileCheck\UnityCompileCheck.csproj' --nologo "-p:UnityEditorRoot=$managed"
        if ($LASTEXITCODE -ne 0) { throw "Unity API compile check failed: $LASTEXITCODE" }
    }
    else {
        Write-Warning 'Unity Editor not found; skipped the Unity API compile check.'
    }
}
finally {
    Pop-Location
}
