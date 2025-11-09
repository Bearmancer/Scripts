$ps1Files = Get-ChildItem "C:\Users\Lance\Documents\PowerShell\*.ps1" -Exclude "Microsoft.PowerShell_profile.ps1"
function GoToPowerShell { Set-Location "C:\Users\Lance\Documents\Powershell" }
function Desktop { Set-Location "C:\Users\Lance\Desktop" }

Set-StrictMode -Version Latest

foreach ($file in $ps1Files) {
    . $file.FullName
}

Set-Alias top Desktop