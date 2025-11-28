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

    if (-not (Test-Path -Path $projectPath)) {
        throw "Project path not found: $projectPath"
    }

    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existingTask) { Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false }

    $script = "Set-Location '$projectPath'; dotnet run $Command"
    $pwshPath = (Get-Command pwsh).Source
    $action = New-ScheduledTaskAction -Execute $pwshPath -Argument "-NoProfile -Command `"$script`"" -WorkingDirectory $projectPath
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger @(
        (New-ScheduledTaskTrigger -Daily -At $start),
        (New-ScheduledTaskTrigger -AtLogOn)
    ) -Settings $settings -Principal $principal -Description $Description | Out-Null

    Write-Host "Registered '$TaskName' for $($start.ToString('HH:mm')) daily" -ForegroundColor Green
}

function Register-AllSyncTasks {
    [CmdletBinding()]
    param()

    Register-ScheduledSyncTask -TaskName LastFmSync -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName YouTubeSync -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}
