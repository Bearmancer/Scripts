function Register-DotNetScheduledTask {
    param(
        [string] $TaskName,
        [string] $ScriptPath,
        [TimeSpan] $DailyTime = '09:00:00',
        [string] $Description = "Scheduled $TaskName task"
    )

    Set-StrictMode -Version Latest

    if (-not ([Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Administrator privileges required to register scheduled tasks"
    }

    if (-not (Test-Path -Path $ScriptPath)) { 
        throw "Source file missing: $ScriptPath" 
    }

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { 
        $start = $start.AddDays(1) 
    }

    $action = New-ScheduledTaskAction -Execute dotnet -Argument $ScriptPath
    $triggerDaily = New-ScheduledTaskTrigger -Daily -At $start
    $triggerLogon = New-ScheduledTaskTrigger -AtLogOn
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue | Unregister-ScheduledTask -Confirm:$false

    $taskParams = @{
        TaskName    = $TaskName
        Action      = $action
        Trigger     = @($triggerDaily, $triggerLogon)
        Principal   = $principal
        Settings    = $settings
        Description = $Description
    }
    Register-ScheduledTask @taskParams | Out-Null

    if (-not (dotnet --list-runtimes | Where-Object { $_ -like '*Microsoft.NETCore.App 10.*' })) {
        Write-Host 'Error: .NET 10 preview runtime not found' -ForegroundColor Red
        exit 1
    }

    dotnet $ScriptPath 

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Task $TaskName failed (exit=$LASTEXITCODE)" -ForegroundColor Red
        Get-WinEvent -LogName 'Microsoft-Windows-TaskScheduler/Operational' -MaxEvents 25 |
        Where-Object { $_.Message -like "*$TaskName*" } |
        Select-Object TimeCreated, Id, LevelDisplayName, Message |
        Format-Table -AutoSize
    }

    Write-Host "✓ Scheduled '$TaskName' for $($start.ToString('HH:mm:ss'))"
}
