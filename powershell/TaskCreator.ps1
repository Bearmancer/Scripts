function Register-ScheduledSyncTask {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [string]$Command,

        [TimeSpan]$DailyTime = '09:00:00',
        [string]$Description = "Scheduled $TaskName task"
    )

    $projectPath = Join-Path $global:ScriptRoot 'csharp'
    $logPath = Join-Path $projectPath 'logs'

    if (-not (Test-Path -Path $projectPath)) {
        throw "Project path not found: $projectPath"
    }

    New-Item -ItemType Directory -Path $logPath -Force | Out-Null

    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existingTask) { Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false }

    $wrapperPath = Join-Path $projectPath "Run-$TaskName.ps1"
    $wrapperContent = @"
`$LogFile = Join-Path '$logPath' '$($TaskName.ToLower())_`$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log'
Set-Location '$projectPath'
dotnet run $Command 2>&1 | Tee-Object -FilePath `$LogFile
if (`$LASTEXITCODE -ne 0) {
    Write-Host "``n[Error] $TaskName failed. Press any key..." -ForegroundColor Red
    `$null = `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
"@
    $wrapperContent | Out-File -FilePath $wrapperPath -Encoding UTF8

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $pwshPath = (Get-Command pwsh).Source
    $action = New-ScheduledTaskAction -Execute $pwshPath -Argument "-ExecutionPolicy Bypass -File `"$wrapperPath`"" -WorkingDirectory $projectPath
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger @(
        (New-ScheduledTaskTrigger -Daily -At $start),
        (New-ScheduledTaskTrigger -AtLogOn)
    ) -Settings $settings -Description $Description | Out-Null

    Write-Host "Registered '$TaskName' for $($start.ToString('HH:mm')) daily" -ForegroundColor Green
}

function Register-AllSyncTasks {
    [CmdletBinding()]
    param()

    Register-ScheduledSyncTask -TaskName LastFmSync -Command lastfm -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName YouTubeSync -Command yt -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}
