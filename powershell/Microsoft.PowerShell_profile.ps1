$global:ScriptRoot = $PSScriptRoot
Set-StrictMode -Version Latest

$moduleManifest = Join-Path -Path $ScriptRoot -ChildPath 'ScriptsToolkit.psd1'
Import-Module -Name $moduleManifest -Force

function Enter-ScriptsDirectory {
    param()
    Set-Location -Path $ScriptRoot
}

function Enter-DesktopDirectory {
    param()
    Set-Location -Path "$env:USERPROFILE\Desktop"
}
