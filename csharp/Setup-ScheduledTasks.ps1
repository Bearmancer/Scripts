param(
    [ValidateSet("lastfm", "youtube", "all")]
    [string]$Service = "all"
)

$ScriptRoot = $PSScriptRoot
$ProjectPath = Join-Path $ScriptRoot "CSharpScripts"
$LogPath = Join-Path $ScriptRoot "logs"

if (-not (Test-Path $ProjectPath)) {
    throw "Project path not found: $ProjectPath"
}

$Tasks = @{
    lastfm = @{
        Name = "LastFmSync"
        Command = "lastfm"
        Time = "9:00AM"
        Description = "Syncs Last.fm scrobbles to Google Sheets daily at 9 AM"
    }
    youtube = @{
        Name = "YouTubeSync"
        Command = "yt"
        Time = "10:00AM"
        Description = "Syncs YouTube playlists to Google Sheets daily at 10 AM"
    }
}

function New-SyncTask {
    param(
        [string]$TaskName,
        [string]$Command,
        [string]$Time,
        [string]$Description
    )

    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction Ignore
    
    $WrapperPath = Join-Path $ProjectPath "Run-$TaskName.ps1"
    Remove-Item -Path $WrapperPath -ErrorAction Ignore
    Remove-Item -Path (Join-Path $LogPath "$($TaskName.ToLower())_*.log") -ErrorAction Ignore

    $WrapperScript = @"
`$LogFile = Join-Path "$LogPath" "$($TaskName.ToLower())_`$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"
`$ErrorOccurred = `$false

Set-Location "$ProjectPath"
dotnet run $Command 2>&1 | Tee-Object -FilePath `$LogFile
if (`$LASTEXITCODE -ne 0) { `$ErrorOccurred = `$true }

if (`$ErrorOccurred) {
    Write-Host "`n[Error] $TaskName failed. Press any key to close..." -ForegroundColor Red
    `$null = `$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
"@

    $WrapperScript | Out-File -FilePath $WrapperPath -Encoding UTF8

    $PwshPath = (Get-Command pwsh -ErrorAction Stop).Source
    $Action = New-ScheduledTaskAction -Execute $PwshPath -Argument "-ExecutionPolicy Bypass -File `"$WrapperPath`"" -WorkingDirectory $ProjectPath
    $Trigger = New-ScheduledTaskTrigger -Daily -At $Time
    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RunOnlyIfNetworkAvailable

    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Settings $Settings -Description $Description | Out-Null

    Write-Host "[Info] Task '$TaskName' created - Daily at $Time" -ForegroundColor Cyan
}

New-Item -ItemType Directory -Path $LogPath -Force | Out-Null

$tasksToCreate = if ($Service -eq "all") { $Tasks.Keys } else { @($Service) }

foreach ($svc in $tasksToCreate) {
    $config = $Tasks[$svc]
    New-SyncTask -TaskName $config.Name -Command $config.Command -Time $config.Time -Description $config.Description
}

Write-Host "`n[Success] Scheduled task(s) created!" -ForegroundColor Green
Write-Host "Logs: $LogPath" -ForegroundColor Cyan
Write-Host "`nTo test:" -ForegroundColor Yellow
foreach ($svc in $tasksToCreate) {
    Write-Host "  Start-ScheduledTask -TaskName '$($Tasks[$svc].Name)'" -ForegroundColor Yellow
}
