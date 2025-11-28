$LogFile = Join-Path "C:\Users\Lance\Desktop\CSharpScripts\logs" "youtubesync_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"
$ErrorOccurred = $false

Set-Location "C:\Users\Lance\Desktop\CSharpScripts\CSharpScripts"
dotnet run yt 2>&1 | Tee-Object -FilePath $LogFile
if ($LASTEXITCODE -ne 0) { $ErrorOccurred = $true }

if ($ErrorOccurred) {
    Write-Host "
[Error] YouTubeSync failed. Press any key to close..." -ForegroundColor Red
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
