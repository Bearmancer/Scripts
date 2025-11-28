$global:ScriptRoot = $PSScriptRoot
$ps1Files = Get-ChildItem "$ScriptRoot\*.ps1" -Exclude "Microsoft.PowerShell_profile.ps1"

Set-StrictMode -Version Latest

foreach ($file in $ps1Files) {
    . $file.FullName
}

function Enter-ScriptsDirectory { Set-Location $ScriptRoot }
function Enter-DesktopDirectory { Set-Location "$env:USERPROFILE\Desktop" }

Set-Alias -Name top -Value Enter-DesktopDirectory
Set-Alias -Name scripts -Value Enter-ScriptsDirectory
