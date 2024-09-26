$ps1Files = Get-ChildItem "C:\Users\Lance\Documents\PowerShell\*.ps1" -Exclude "Microsoft.PowerShell_profile.ps1"
$scripts = "C:\Users\Lance\Documents\Powershell\Python Scripts"

Set-StrictMode -Version Latest

foreach ($file in $ps1Files) {
    . $file.FullName
}