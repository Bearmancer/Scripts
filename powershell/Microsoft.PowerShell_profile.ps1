$global:ScriptRoot = $PSScriptRoot
$ps1Files = Get-ChildItem "$ScriptRoot\*.ps1" -Exclude "Microsoft.PowerShell_profile.ps1"

Set-StrictMode -Version Latest

foreach ($file in $ps1Files) {
    . $file.FullName
}

function GoToScripts { Set-Location $ScriptRoot }
function Desktop { Set-Location "$env:USERPROFILE\Desktop" }

Set-Alias top Desktop
Set-Alias scripts GoToScripts
