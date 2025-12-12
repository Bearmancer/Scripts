$profileDir = $PSScriptRoot
if (-not $profileDir -or -not (Test-Path -Path $profileDir)) {
    $profileDir = Join-Path -Path $env:USERPROFILE -ChildPath 'Dev\Scripts\powershell'
}
$global:ScriptRoot = $profileDir
Set-StrictMode -Version Latest

$env:PYTHONWARNINGS = 'ignore::UserWarning'

$moduleManifest = Join-Path -Path $ScriptRoot -ChildPath 'ScriptsToolkit.psd1'
if (Test-Path -Path $moduleManifest) {
    Import-Module -Name $moduleManifest -Force
}
else {
    Write-Warning "ScriptsToolkit module not found at $moduleManifest"
}

function Enter-ScriptsDirectory {
    param()
    Set-Location -Path $ScriptRoot
}

function Enter-DesktopDirectory {
    param()
    Set-Location -Path "$env:USERPROFILE\Desktop"
}
