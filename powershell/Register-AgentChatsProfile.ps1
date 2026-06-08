$profilePath = "$env:USERPROFILE\Documents\PowerShell\Microsoft.PowerShell_profile.ps1"
$profileDir  = Split-Path $profilePath
if (-not (Test-Path $profileDir)) { New-Item -ItemType Directory -Path $profileDir -Force | Out-Null }

$dotSource = '. "C:\Users\Lance\Dev\Scripts\powershell\Export-AgentChats.ps1"'
$existing  = if (Test-Path $profilePath) { Get-Content $profilePath -Raw -Encoding utf8 } else { '' }

if ($existing -notmatch 'Export-AgentChats') {
    Add-Content -Path $profilePath -Value "`n$dotSource" -Encoding utf8
    Write-Host "Added to profile: $profilePath"
} else {
    Write-Host "Already registered."
}
