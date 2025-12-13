Set-StrictMode -Version Latest


$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'



function Invoke-ToolkitPython {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$ArgumentList
    )

    $python = (Get-Command -Name python).Source
    $arguments = @($Script:PythonToolkit) + $ArgumentList
    & $python @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Python toolkit exited with code $LASTEXITCODE"
    }
}



function Get-ToolkitFunctions {
    <#
    .SYNOPSIS
        Lists all available functions in the ScriptsToolkit module.

    .DESCRIPTION
        Displays a categorized list of all functions exported by the ScriptsToolkit module,
        including their aliases and brief descriptions. Functions are grouped by category
        (Utilities, Logging, Filesystem, Video, Audio, Transcription, YouTube, Sync, Tasks).

    .EXAMPLE
        Get-ToolkitFunctions

        Lists all functions organized by category.

    .EXAMPLE
        tkfn

        Same as above using the alias.

    .OUTPUTS
        None. Writes formatted output to the console.

    .NOTES
        Alias: tkfn
    #>
    [CmdletBinding()]
    [Alias('tkfn')]
    param()

    $functions = @(
        @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions'; Alias = 'tkfn'; Description = 'List all toolkit functions' }
        @{ Category = 'Utilities'; Name = 'Open-CommandHistory'; Alias = 'hist'; Description = 'Open PowerShell history file' }
        @{ Category = 'Utilities'; Name = 'Show-ToolkitHelp'; Alias = 'tkhelp'; Description = 'Open toolkit documentation' }
        @{ Category = 'Utilities'; Name = 'Invoke-ToolkitAnalyzer'; Alias = 'tklint'; Description = 'Run PSScriptAnalyzer' }
        @{ Category = 'Logs'; Name = 'Show-SyncLog'; Alias = 'synclog'; Description = 'View JSONL sync logs as table' }
        @{ Category = 'Sync'; Name = 'Invoke-YouTubeSync'; Alias = 'syncyt'; Description = 'Sync YouTube playlists' }
        @{ Category = 'Sync'; Name = 'Invoke-LastFmSync'; Alias = 'synclf'; Description = 'Sync Last.fm scrobbles' }
        @{ Category = 'Sync'; Name = 'Invoke-AllSyncs'; Alias = 'syncall'; Description = 'Run all daily syncs' }
        @{ Category = 'Filesystem'; Name = 'Get-Directories'; Alias = 'dirs'; Description = 'List directories with sizes' }
        @{ Category = 'Filesystem'; Name = 'Get-FilesAndDirectories'; Alias = 'tree'; Description = 'List all items with sizes' }
        @{ Category = 'Filesystem'; Name = 'New-Torrents'; Alias = 'torrent'; Description = 'Create .torrent files' }
        @{ Category = 'Video'; Name = 'Start-DiscRemux'; Alias = 'remux'; Description = 'Remux video discs to MKV' }
        @{ Category = 'Video'; Name = 'Start-BatchCompression'; Alias = 'compress'; Description = 'Compress videos in batch' }
        @{ Category = 'Video'; Name = 'Get-VideoChapters'; Alias = 'chapters'; Description = 'Extract chapter timestamps' }
        @{ Category = 'Video'; Name = 'Get-VideoResolution'; Alias = 'res'; Description = 'Report video resolutions' }
        @{ Category = 'Audio'; Name = 'Convert-Audio'; Alias = 'audio'; Description = 'Convert audio files' }
        @{ Category = 'Audio'; Name = 'Convert-ToMP3'; Alias = 'tomp3'; Description = 'Convert to MP3' }
        @{ Category = 'Audio'; Name = 'Convert-ToFLAC'; Alias = 'toflac'; Description = 'Convert to FLAC' }
        @{ Category = 'Audio'; Name = 'Convert-SACD'; Alias = 'sacd'; Description = 'Extract SACD ISO files' }
        @{ Category = 'Audio'; Name = 'Rename-MusicFiles'; Alias = 'rename'; Description = 'Rename files using RED naming' }
        @{ Category = 'Audio'; Name = 'Get-EmbeddedImageSize'; Alias = 'artsize'; Description = 'Report embedded art sizes' }
        @{ Category = 'Audio'; Name = 'Invoke-Propolis'; Alias = 'propolis'; Description = 'Run Propolis analyzer' }
        @{ Category = 'Transcription'; Name = 'Invoke-Whisper'; Alias = 'whisp'; Description = 'Transcribe file or folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperFolder'; Alias = 'wpf'; Description = 'Transcribe folder (explicit)' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapanese'; Alias = 'wpj'; Description = 'Transcribe Japanese file/folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapaneseFolder'; Alias = 'wpjf'; Description = 'Transcribe Japanese folder (explicit)' }
        @{ Category = 'YouTube'; Name = 'Save-YouTubeVideo'; Alias = 'ytdl'; Description = 'Download YouTube videos' }
        @{ Category = 'Tasks'; Name = 'Register-ScheduledSyncTask'; Alias = 'regtask'; Description = 'Create scheduled task' }
        @{ Category = 'Tasks'; Name = 'Register-AllSyncTasks'; Alias = 'regall'; Description = 'Register all sync tasks' }
    )

    Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    Write-Host "========================`n" -ForegroundColor Cyan

    $functions | Group-Object -Property Category | ForEach-Object {
        Write-Host "$( $_.Name )" -ForegroundColor Yellow
        $_.Group | ForEach-Object {
            Write-Host "  $($_.Alias.PadRight(10) )" -ForegroundColor Green -NoNewline
            Write-Host "$($_.Name.PadRight(30) )" -ForegroundColor White -NoNewline
            Write-Host "$( $_.Description )" -ForegroundColor DarkGray
        }
        Write-Host ""
    }
}

function Open-CommandHistory {
    <#
    .SYNOPSIS
        Opens the PowerShell command history file in VS Code.

    .DESCRIPTION
        Opens the PSReadLine command history file located at
        %APPDATA%\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
        in Visual Studio Code for viewing or searching past commands.

    .EXAMPLE
        Open-CommandHistory

        Opens the history file in VS Code.

    .EXAMPLE
        hist

        Same as above using the alias.

    .NOTES
        Alias: hist
        Requires VS Code to be installed and available in PATH.
    #>
    [CmdletBinding()]
    [Alias('hist')]
    param()

    & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

function Show-ToolkitHelp {
    <#
    .SYNOPSIS
        Opens the ScriptsToolkit help documentation file.

    .DESCRIPTION
        Opens the ScriptsToolkit.Help.md file in Visual Studio Code, which contains
        comprehensive documentation for all toolkit functions and usage examples.

    .EXAMPLE
        Show-ToolkitHelp

        Opens the help documentation in VS Code.

    .EXAMPLE
        tkhelp

        Same as above using the alias.

    .NOTES
        Alias: tkhelp
        Requires VS Code to be installed and available in PATH.
    #>
    [CmdletBinding()]
    [Alias('tkhelp')]
    param()

    $helpFile = Join-Path -Path $PSScriptRoot -ChildPath 'ScriptsToolkit.Help.md'
    & code $helpFile
}

function Invoke-ToolkitAnalyzer {
    <#
    .SYNOPSIS
        Runs PSScriptAnalyzer on toolkit scripts.

    .DESCRIPTION
        Invokes PSScriptAnalyzer with the toolkit's custom settings file to check
        for errors and warnings in PowerShell scripts. Uses the settings defined
        in PSScriptAnalyzerSettings.psd1.

    .PARAMETER Path
        Path to the file or directory to analyze. Defaults to the toolkit directory.

    .EXAMPLE
        Invoke-ToolkitAnalyzer

        Analyzes all scripts in the toolkit directory.

    .EXAMPLE
        tklint -Path .\MyScript.ps1

        Analyzes a specific script file.

    .EXAMPLE
        Invoke-ToolkitAnalyzer -Path C:\Scripts

        Analyzes all scripts in the specified directory.

    .NOTES
        Alias: tklint
        Requires PSScriptAnalyzer module to be installed.
    #>
    [CmdletBinding()]
    [Alias('tklint')]
    param(
        [Parameter(Position = 0)]
        [Alias('p')]
        [string]$Path = (Join-Path -Path $PSScriptRoot -ChildPath '.')
    )

    $settings = Join-Path -Path $PSScriptRoot -ChildPath 'PSScriptAnalyzerSettings.psd1'
    Invoke-ScriptAnalyzer -Path $Path -Settings $settings -Recurse -Severity Error, Warning
}



function Show-SyncLog {
    <#
    .SYNOPSIS
        View sync session logs with session-based grouping and status summaries.

    .DESCRIPTION
        Displays sync log entries from YouTube and LastFm services.
        Default view shows session summaries (last N sessions).
        Use -Entries to see individual log entries.
        Use -Detailed to include verbose events like PlaylistUpdated/ScrobblesProcessed.

    .PARAMETER Service
        Filter by service: 'youtube', 'lastfm', or 'all' (default).

    .PARAMETER Sessions
        Number of recent sessions to display (default: 10).

    .PARAMETER Entries
        Switch to entry-based view instead of session summaries.

    .PARAMETER Tail
        Number of entries to show when using -Entries (default: 20).

    .PARAMETER SessionId
        Filter to a specific session ID. Use this to investigate a specific sync run.

    .PARAMETER Detailed
        Include verbose events (PlaylistUpdated, PlaylistCreated, ScrobblesProcessed).
        By default these are hidden to reduce noise.

    .PARAMETER Full
        Show full details without truncation.

    .EXAMPLE
        synclog
        Shows last 10 session summaries for all services.

    .EXAMPLE
        Show-SyncLog -Service youtube -Sessions 5
        Shows last 5 YouTube sync sessions.

    .EXAMPLE
        synclog -Entries -Tail 50
        Shows last 50 individual log entries across all services.

    .EXAMPLE
        synclog -SessionId '2fd1b74d' -Detailed
        Shows all entries for a specific session, including verbose events.

    .EXAMPLE
        synclog -Entries -Detailed -Full
        Shows detailed log entries without truncation - useful for debugging.

    .NOTES
        Log files: logs/youtube.jsonl, logs/lastfm.jsonl

        Default view filters out noise (PlaylistUpdated with 0 changes, etc.)
        Use -Detailed to see everything for investigating issues.

        Session statuses:
        - Completed: Normal finish
        - Failed: Ended with exception
        - Interrupted: User cancelled (Ctrl+C)
        - Crashed: Session never closed (detected on next run)
        - Running: Started recently, no end event yet
    #>
    [CmdletBinding(DefaultParameterSetName = 'Sessions')]
    [Alias('viewlog', 'synclog')]
    param(
        [Parameter()]
        [ValidateSet('youtube', 'lastfm', 'all')]
        [string]$Service = 'all',

        [Parameter(ParameterSetName = 'Sessions')]
        [Alias('n')]
        [int]$Sessions = 10,

        [Parameter(ParameterSetName = 'Entries')]
        [Alias('e')]
        [switch]$Entries,

        [Parameter(ParameterSetName = 'Entries')]
        [Alias('Last')]
        [int]$Tail = 20,

        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [Alias('d')]
        [switch]$Detailed,

        [Parameter()]
        [Alias('f')]
        [switch]$Full
    )

    $logFiles = @()
    $ytLog = Join-Path -Path $Script:LogDirectory -ChildPath 'youtube.jsonl'
    $lfmLog = Join-Path -Path $Script:LogDirectory -ChildPath 'lastfm.jsonl'

    if ($Service -in 'all', 'youtube' -and (Test-Path $ytLog)) { $logFiles += $ytLog }
    if ($Service -in 'all', 'lastfm' -and (Test-Path $lfmLog)) { $logFiles += $lfmLog }

    if ($logFiles.Count -eq 0) {
        Write-Warning "No log files found in $Script:LogDirectory"
        return
    }

    $allEntries = @()
    foreach ($logFile in $logFiles) {
        $serviceName = [System.IO.Path]::GetFileNameWithoutExtension($logFile)
        foreach ($line in [System.IO.File]::ReadLines($logFile)) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            try {
                $obj = $line | ConvertFrom-Json
                $obj | Add-Member -NotePropertyName 'Source' -NotePropertyValue $serviceName -Force
                $parsed = [datetime]::ParseExact($obj.Timestamp, 'yyyy/MM/dd HH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture)
                $obj | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsed -Force
                $allEntries += $obj
            }
            catch {
                Write-Warning "Failed to parse log line in ${logFile}: $_"
            }
        }
    }

    if ($SessionId) {
        $allEntries = $allEntries | Where-Object { $_.SessionId -eq $SessionId }
    }

    if ($PSCmdlet.ParameterSetName -eq 'Entries' -or $Entries) {
        Show-SyncLogEntries -Entries $allEntries -Tail $Tail -Full:$Full -Detailed:$Detailed
    }
    else {
        Show-SyncLogSessions -Entries $allEntries -MaxSessions $Sessions -Full:$Full
    }
}

function Show-SyncLogSessions {
    param(
        [Parameter(Mandatory)]
        [array]$Entries,
        [int]$MaxSessions = 10,
        [switch]$Full
    )

    $sessionData = @{}
    foreach ($entry in $Entries) {
        $sid = $entry.SessionId
        if (-not $sid) { continue }

        if (-not $sessionData.ContainsKey($sid)) {
            $sessionData[$sid] = @{
                SessionId  = $sid
                Source     = $entry.Source
                StartTime  = $null
                EndTime    = $null
                Status     = 'Unknown'
                EventCount = 0
                Summary    = ''
                HasError   = $false
            }
        }

        $sessionData[$sid].EventCount++

        switch ($entry.Event) {
            'SessionStart' {
                $sessionData[$sid].StartTime = $entry.ParsedTimestamp
            }
            'SessionEnd' {
                $sessionData[$sid].EndTime = $entry.ParsedTimestamp
                $sessionData[$sid].Status = $entry.Data.Status ?? 'Completed'
                $sessionData[$sid].Summary = $entry.Data.Summary ?? ''
            }
            'SessionInterrupted' {
                $sessionData[$sid].EndTime = $entry.ParsedTimestamp
                $sessionData[$sid].Status = 'Interrupted'
                $sessionData[$sid].Summary = $entry.Data.Progress ?? ''
            }
            'SessionCrashed' {
                $sessionData[$sid].Status = 'Crashed'
                $sessionData[$sid].Summary = "Detected at $($entry.Data.DetectedAt)"
            }
            'Exception' {
                $sessionData[$sid].HasError = $true
            }
        }
    }

    $sessionList = $sessionData.Values | ForEach-Object { [PSCustomObject]$_ } |
    Where-Object { $_.StartTime } |
    Sort-Object -Property StartTime -Descending |
    Select-Object -First $MaxSessions |
    Sort-Object -Property StartTime

    if (-not $sessionList -or @($sessionList).Count -eq 0) {
        Write-Host "`nNo sessions found." -ForegroundColor Yellow
        return
    }

    $statusColors = @{
        'Completed'   = 'Green'
        'Failed'      = 'Red'
        'Interrupted' = 'Yellow'
        'Crashed'     = 'Magenta'
        'Running'     = 'Cyan'
        'Unknown'     = 'DarkGray'
    }

    $terminalWidth = $Host.UI.RawUI.WindowSize.Width
    $now = Get-Date

    Write-Host ""
    Write-Host "Session History" -ForegroundColor Cyan
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    $headerFormat = "{0,-12} {1,-8} {2,-20} {3,-10} {4,-12} {5,-6} {6}"
    Write-Host ($headerFormat -f "Date", "Service", "Time Range", "Duration", "Status", "Events", "Summary") -ForegroundColor White
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    foreach ($s in $sessionList) {
        $date = $s.StartTime.ToString('yyyy/MM/dd')
        $startStr = $s.StartTime.ToString('HH:mm:ss')
        $endStr = if ($s.EndTime) { $s.EndTime.ToString('HH:mm:ss') } else { 'running' }
        $timeRange = "$startStr → $endStr"

        $duration = if ($s.EndTime) {
            $span = $s.EndTime - $s.StartTime
            if ($span.TotalHours -ge 1) { "{0:0}h {1:0}m" -f $span.Hours, $span.Minutes }
            elseif ($span.TotalMinutes -ge 1) { "{0:0}m {1:0}s" -f $span.Minutes, $span.Seconds }
            else { "{0:0}s" -f $span.TotalSeconds }
        }
        else {
            $span = $now - $s.StartTime
            "~{0:0}m" -f $span.TotalMinutes
        }

        $status = if (-not $s.EndTime -and ($now - $s.StartTime).TotalHours -lt 2) { 'Running' } 
        elseif (-not $s.EndTime) { 'Crashed' }
        elseif ($s.HasError) { 'Failed' }
        else { $s.Status }

        $color = $statusColors[$status] ?? 'White'
        $summary = if ($Full) { $s.Summary } else { 
            if ($s.Summary.Length -gt 40) { $s.Summary.Substring(0, 37) + "..." } else { $s.Summary }
        }

        Write-Host ($headerFormat -f $date, $s.Source, $timeRange, $duration, $status, $s.EventCount, $summary) -ForegroundColor $color
    }

    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    $taskInfo = Get-ScheduledTask -TaskName "*Sync" -ErrorAction SilentlyContinue | 
    Where-Object { $_.TaskName -in 'YouTubeSync', 'LastFmSync' } |
    Get-ScheduledTaskInfo -ErrorAction SilentlyContinue

    if ($taskInfo) {
        Write-Host ""
        Write-Host "Scheduled Tasks" -ForegroundColor Cyan
        Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
        foreach ($task in $taskInfo) {
            $lastRun = if ($task.LastRunTime -and $task.LastRunTime.Year -gt 2000) { 
                $task.LastRunTime.ToString('yyyy/MM/dd HH:mm:ss') 
            }
            else { 'Never' }
            $nextRun = if ($task.NextRunTime) { $task.NextRunTime.ToString('yyyy/MM/dd HH:mm:ss') } else { 'Not scheduled' }
            $resultCode = switch ($task.LastTaskResult) {
                0 { 'Success' }
                1 { 'Incorrect function' }
                267009 { 'Task running' }
                267011 { 'Task never run' }
                default { "Code: $($task.LastTaskResult)" }
            }
            $resultColor = if ($task.LastTaskResult -eq 0) { 'Green' } elseif ($task.LastTaskResult -eq 267009) { 'Cyan' } else { 'Yellow' }

            Write-Host "  $($task.TaskName.PadRight(15))" -ForegroundColor White -NoNewline
            Write-Host "Last: $lastRun  " -ForegroundColor DarkGray -NoNewline
            Write-Host "Next: $nextRun  " -ForegroundColor DarkGray -NoNewline
            Write-Host "Result: $resultCode" -ForegroundColor $resultColor
        }
        Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
    }
}

function Show-SyncLogEntries {
    param(
        [Parameter(Mandatory)]
        [array]$Entries,
        [int]$Tail = 20,
        [switch]$Full,
        [switch]$Detailed
    )

    if (-not $Detailed) {
        # Filter out verbose events that create noise in default view
        $verboseEvents = @('PlaylistCreated', 'ScrobblesProcessed')
        $Entries = $Entries | Where-Object { $_.Event -notin $verboseEvents }
        
        # Also filter out PlaylistUpdated/PlaylistDeleted/PlaylistRenamed with no actual changes
        $Entries = $Entries | Where-Object {
            if ($_.Event -eq 'PlaylistUpdated') {
                # Only show if something actually changed
                $added = $_.Data.Added ?? 0
                $removed = $_.Data.Removed ?? 0
                return ($added -gt 0 -or $removed -gt 0)
            }
            return $true
        }
    }

    $result = $Entries | Sort-Object -Property ParsedTimestamp | Select-Object -Last $Tail

    $levelColors = @{
        'Debug'   = 'DarkGray'
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Fatal'   = 'Magenta'
    }

    $terminalWidth = $Host.UI.RawUI.WindowSize.Width

    Write-Host ""
    $headerFormat = "{0,-20} {1,-8} {2,-8} {3,-20} {4,-10} {5}"
    Write-Host ($headerFormat -f "Timestamp", "Service", "Level", "Event", "Session", "Details") -ForegroundColor White
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    foreach ($entry in $result) {
        $color = $levelColors[$entry.Level] ?? 'White'

        $details = if ($entry.Data) {
            if ($entry.Data.PSObject.Properties['Text']) {
                $entry.Data.Text
            }
            else {
                ($entry.Data.PSObject.Properties | Where-Object { $_.Name -notin 'Service', 'ProcessId' } | ForEach-Object {
                    $val = if ($_.Value -is [array]) { $_.Value -join ", " } else { $_.Value }
                    "$($_.Name): $val"
                }) -join " | "
            }
        }
        else { '' }

        $maxDetails = if ($Full) { $terminalWidth - 70 } else { 50 }
        if ($details.Length -gt $maxDetails) {
            $details = $details.Substring(0, $maxDetails - 3) + "..."
        }

        $sid = if ($entry.SessionId) { $entry.SessionId.Substring(0, [Math]::Min(8, $entry.SessionId.Length)) } else { '' }

        Write-Host ($headerFormat -f $entry.Timestamp, $entry.Source, $entry.Level, $entry.Event, $sid, $details) -ForegroundColor $color
    }

    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
}

function Get-Directories {
    [CmdletBinding()]
    [Alias('dirs')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort)
}

function Get-FilesAndDirectories {
    [CmdletBinding()]
    [Alias('tree')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort, '--include-files')
}

function New-Torrents {
    [CmdletBinding()]
    [Alias('torrent')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('r')]
        [switch]$IncludeSubdirectories
    )

    $arguments = @('filesystem', 'torrents', '--directory', $Directory.FullName)
    if ($IncludeSubdirectories.IsPresent) {
        $arguments += '--include-subdirectories'
    }
    Invoke-ToolkitPython -ArgumentList $arguments
}



function Start-DiscRemux {
    [CmdletBinding()]
    [Alias('remux')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [switch]$SkipMediaInfo
    )

    $arguments = @('video', 'remux', '--path', $Directory.FullName)
    if ($SkipMediaInfo.IsPresent) {
        $arguments += '--skip-mediainfo'
    }
    Invoke-ToolkitPython -ArgumentList $arguments
}

function Start-BatchCompression {
    [CmdletBinding()]
    [Alias('compress')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'compress', '--directory', $Directory.FullName)
}

function Get-VideoChapters {
    [CmdletBinding()]
    [Alias('chapters')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'chapters', '--path', $Directory.FullName)
}

function Get-VideoResolution {
    [CmdletBinding()]
    [Alias('res')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'resolutions', '--path', $Directory.FullName)
}



function Convert-Audio {
    [CmdletBinding()]
    [Alias('audio')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('fmt')]
        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'convert', '--format', $Format)
}

function Convert-ToMP3 {
    [CmdletBinding()]
    [Alias('tomp3')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'mp3'
}

function Convert-ToFLAC {
    [CmdletBinding()]
    [Alias('toflac')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'flac'
}

function Convert-SACD {
    [CmdletBinding()]
    [Alias('sacd')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('fmt')]
        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'extract', '--format', $Format)
}

function Rename-MusicFiles {
    [CmdletBinding()]
    [Alias('rename')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'rename', '--directory', $Directory.FullName)
}

function Get-EmbeddedImageSize {
    [CmdletBinding()]
    [Alias('artsize')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'art-report', '--directory', $Directory.FullName)
}

function Invoke-Propolis {
    [CmdletBinding()]
    [Alias('propolis')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}



function Invoke-Whisper {
    [CmdletBinding()]
    [Alias('whisp')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName')]
        [string]$Path,

        [Parameter()]
        [Alias('l')]
        [string]$Language,

        [Parameter()]
        [Alias('m')]
        [ValidateSet('tiny', 'tiny.en', 'base', 'base.en', 'small', 'small.en', 'medium', 'medium.en', 'large-v1', 'large-v2', 'large-v3', 'large-v3-turbo', 'turbo', 'distil-large-v2', 'distil-large-v3', 'distil-large-v3.5', 'distil-medium.en', 'distil-small.en')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter()]
        [Alias('b')]
        [switch]$Batched,

        [Parameter()]
        [int]$BatchSize = 4,

        [Parameter()]
        [switch]$NoVadFilter,

        [Parameter()]
        [Alias('rp')]
        [double]$RepetitionPenalty = 1.1,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    $item = Get-Item -Path $Path
    if ($item.PSIsContainer) {
        Invoke-WhisperFolder -Directory $item -Language $Language -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -Batched:$Batched -BatchSize $BatchSize -NoVadFilter:$NoVadFilter -RepetitionPenalty $RepetitionPenalty -ExtraArgs $ExtraArgs
        return
    }

    $srtPath = Join-Path $OutputDir ($item.BaseName + '.srt')

    if ((Test-Path $srtPath) -and -not $Force) {
        Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Skipped: $( $item.Name ) (SRT exists)" -ForegroundColor Yellow
        return
    }

    $effectiveModel = $Model ? $Model : ($Language -eq 'en' ? 'distil-large-v3.5' : 'large-v3')
    $languageDisplay = $Language ? $Language : '(auto-detect)'

    Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Transcribing: $( $item.Name )" -ForegroundColor Cyan
    Write-Host "             Model: $effectiveModel | Language: $languageDisplay" -ForegroundColor DarkGray

    $whisperArgs = @(
        '--model', $effectiveModel
        '--compute_type', 'auto'
        '--output_format', 'srt'
        '--output_dir', $OutputDir
        '--verbose', 'False'
        '--repetition_penalty', $RepetitionPenalty.ToString()
    )

    if (-not $NoVadFilter) {
        $whisperArgs += '--vad_filter', 'True'
        $whisperArgs += '--vad_min_silence_duration_ms', '500'
    }

    if ($Batched) {
        $whisperArgs += '--batched', 'True'
        $whisperArgs += '--batch_size', $BatchSize.ToString()
    }

    if ($Language) {
        $whisperArgs += '--language', $Language
    }

    if ($Translate) {
        $whisperArgs += '--task', 'translate'
    }

    if ($ExtraArgs) {
        $whisperArgs += $ExtraArgs
    }

    $whisperArgs += $item.FullName

    & whisper-ctranslate2 @whisperArgs

    Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Completed: $( $item.Name )" -ForegroundColor Green
}

function Invoke-WhisperFolder {
    [CmdletBinding()]
    [Alias('wpf')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [Parameter()]
        [Alias('l')]
        [string]$Language = 'en',

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter()]
        [Alias('b')]
        [switch]$Batched,

        [Parameter()]
        [int]$BatchSize = 4,

        [Parameter()]
        [switch]$NoVadFilter,

        [Parameter()]
        [Alias('rp')]
        [double]$RepetitionPenalty = 1.1,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    $extensions = @('.mp4', '.mkv', '.avi', '.mp3', '.flac', '.wav', '.webm', '.m4a', '.opus', '.ogg')
    $files = Get-ChildItem $Directory -Recurse -File | Where-Object { $_.Extension.ToLower() -in $extensions }

    $skipped = @()
    $toProcess = @()

    foreach ($file in $files) {
        $srtPath = [System.IO.Path]::ChangeExtension($file.FullName, '.srt')
        if ((Test-Path $srtPath) -and -not $Force) {
            $skipped += $file
        }
        else {
            $toProcess += $file
        }
    }

    if ($skipped.Count -gt 0) {
        Write-Host "`nSkipped $( $skipped.Count ) files (SRT exists):" -ForegroundColor Yellow
        $skipped | ForEach-Object { Write-Host "  $( $_.Name )" -ForegroundColor DarkGray }
        Write-Host ""
    }

    if ($toProcess.Count -eq 0) {
        Write-Host "Nothing to transcribe. Use -Force to overwrite existing files." -ForegroundColor Yellow
        return
    }

    Write-Host "Transcribing $( $toProcess.Count ) files:`n" -ForegroundColor Cyan

    $current = 0
    foreach ($file in $toProcess) {
        $current++
        Write-Host "[$current/$( $toProcess.Count )] " -ForegroundColor DarkGray -NoNewline
        Invoke-Whisper -Path $file.FullName -Language $Language -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -Batched:$Batched -BatchSize $BatchSize -NoVadFilter:$NoVadFilter -RepetitionPenalty $RepetitionPenalty -ExtraArgs $ExtraArgs
        Write-Host ""
    }

    Write-Host "Completed: $current/$( $toProcess.Count ) transcribed" -ForegroundColor Green
    if ($skipped.Count -gt 0) {
        Write-Host "           $( $skipped.Count ) skipped" -ForegroundColor DarkGray
    }
}

function Invoke-WhisperJapanese {
    [CmdletBinding()]
    [Alias('wpj')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName')]
        [string]$Path,

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    Invoke-Whisper -Path $Path -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -ExtraArgs $ExtraArgs
}

function Invoke-WhisperJapaneseFolder {
    [CmdletBinding()]
    [Alias('wpjf')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    Invoke-WhisperFolder -Directory $Directory -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -ExtraArgs $ExtraArgs
}



function Save-YouTubeVideo {
    [CmdletBinding()]
    [Alias('ytdl')]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]]$Urls,

        [Parameter()]
        [switch]$Transcribe,

        [Parameter()]
        [Alias('l')]
        [string]$Language = 'en',

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('o')]
        [System.IO.DirectoryInfo]$OutputDir = (Get-Item .)
    )

    Push-Location $OutputDir

    foreach ($url in $Urls) {
        Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] " -NoNewline
        Write-Host 'Downloading: ' -ForegroundColor Cyan -NoNewline
        Write-Host $url

        $filePath = & yt-dlp --print filename $url --windows-filenames -o '%(title)s.%(ext)s'

        if (Test-Path -Path $filePath) {
            Remove-Item $filePath -Force
            Write-Host '  Replaced existing file' -ForegroundColor Yellow
        }

        & yt-dlp $url --windows-filenames -o '%(title)s.%(ext)s'

        if ($Transcribe -and (Test-Path $filePath)) {
            Invoke-Whisper -Path $filePath -Language $Language -Model $Model -Translate:$Translate -OutputDir $OutputDir.FullName
        }
    }
    Pop-Location
}



function Invoke-YouTubeSync {
    <#
    .SYNOPSIS
        Synchronizes YouTube playlists to a Google Sheets spreadsheet.

    .DESCRIPTION
        Fetches all playlists from your YouTube account and syncs them to Google Sheets.
        Uses ETag-based change detection to minimize API calls on subsequent runs.
        Supports resumable syncs - if interrupted, will continue from where it left off.

    .PARAMETER Force
        Forces a complete re-sync by clearing the local cache before starting.
        Use this if you suspect the cache is corrupted or out of sync.

    .EXAMPLE
        syncyt
        Runs an optimized sync, only fetching changed playlists based on ETag comparison.

    .EXAMPLE
        Invoke-YouTubeSync -Force
        Clears all cached state and performs a complete re-sync of all playlists.

    .EXAMPLE
        syncyt -Verbose
        Runs sync with debug-level logging to see detailed progress and API calls.

    .NOTES
        Required environment variables:
        - YOUTUBE_GOOGLE_CLIENT_ID
        - YOUTUBE_GOOGLE_CLIENT_SECRET
        - YOUTUBE_SPREADSHEET_ID (optional - will create new if not set)

        Log file: logs/youtube.jsonl
        State file: state/youtube/sync.json
    #>
    [CmdletBinding()]
    [Alias('syncyt')]
    param(
        [Parameter()]
        [Alias('f')]
        [switch]$Force
    )

    Push-Location $Script:CSharpRoot
    try {
        $arguments = @('run', '--', 'sync', 'yt')
        if ($Force) { $arguments += '--force' }
        if ($VerbosePreference -eq 'Continue') { $arguments += '--verbose' }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-LastFmSync {
    <#
    .SYNOPSIS
        Synchronizes Last.fm scrobbles to a Google Sheets spreadsheet.

    .DESCRIPTION
        Fetches scrobble history from Last.fm and syncs to Google Sheets.
        Uses incremental sync by default, only fetching scrobbles newer than the latest in cache.
        Supports resumable syncs - if interrupted mid-page, will resume from last completed page.

    .PARAMETER Since
        Force re-sync from a specific date. Format: yyyy/MM/dd
        Deletes existing data from that date forward and re-fetches from Last.fm.
        Use with caution - this WILL delete data from the spreadsheet.

    .EXAMPLE
        synclf
        Runs an incremental sync, fetching only new scrobbles since last sync.

    .EXAMPLE
        Invoke-LastFmSync -Since '2024/01/01'
        Deletes all scrobbles from 2024/01/01 onwards and re-fetches from Last.fm.

    .EXAMPLE
        synclf -Verbose
        Runs sync with debug-level logging to see each page fetch and API call.

    .NOTES
        Required environment variables:
        - LASTFM_API_KEY
        - LASTFM_USERNAME
        - LASTFM_SPREADSHEET_ID (optional - will create new if not set)

        Log file: logs/lastfm.jsonl
        State file: state/lastfm/sync.json
        Cache file: state/lastfm/scrobbles.json
    #>
    [CmdletBinding()]
    [Alias('synclf')]
    param(
        [Parameter()]
        [Alias('s')]
        [string]$Since
    )

    Push-Location $Script:CSharpRoot
    try {
        $arguments = @('run', '--', 'sync', 'lastfm')
        if ($Since) { $arguments += '--since', $Since }
        if ($VerbosePreference -eq 'Continue') { $arguments += '--verbose' }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-AllSyncs {
    <#
    .SYNOPSIS
        Runs all sync operations in sequence.

    .DESCRIPTION
        Executes YouTube sync followed by Last.fm sync.
        Useful for daily scheduled syncs or manual full sync runs.

    .EXAMPLE
        syncall
        Runs YouTube sync then Last.fm sync.

    .EXAMPLE
        Invoke-AllSyncs -Verbose
        Runs all syncs with debug logging enabled.

    .NOTES
        This runs syncs sequentially, not in parallel.
        If YouTube sync fails, Last.fm sync will still run.
    #>
    [CmdletBinding()]
    [Alias('syncall')]
    param()

    Write-Host "`n[YouTube Sync]" -ForegroundColor Cyan
    if ($VerbosePreference -eq 'Continue') {
        Invoke-YouTubeSync -Verbose
    }
    else {
        Invoke-YouTubeSync
    }

    Write-Host "`n[Last.fm Sync]" -ForegroundColor Cyan
    if ($VerbosePreference -eq 'Continue') {
        Invoke-LastFmSync -Verbose
    }
    else {
        Invoke-LastFmSync
    }

    Write-Host "`nAll syncs complete!" -ForegroundColor Green
}



function Register-ScheduledSyncTask {
    <#
    .SYNOPSIS
        Creates a Windows scheduled task for running sync operations.

    .DESCRIPTION
        Registers a scheduled task that runs a sync command daily at the specified time.
        Uses a wrapper script to avoid argument quoting issues with Task Scheduler.
        Opens in Windows Terminal if it's the default terminal, otherwise PowerShell window.
        Window stays open after completion so you can review output.

    .PARAMETER TaskName
        Name for the scheduled task. Must be unique.

    .PARAMETER Command
        The dotnet CLI command to run (e.g., 'sync yt' or 'sync lastfm').

    .PARAMETER DailyTime
        Time of day to run the task. Default: 09:00:00

    .PARAMETER Description
        Description for the scheduled task.

    .EXAMPLE
        Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00'
        Creates a task that syncs YouTube playlists daily at 10 AM.

    .EXAMPLE
        regtask -TaskName 'LastFmSync' -Command 'sync lastfm'
        Creates a task that syncs Last.fm scrobbles daily at the default time (9 AM).

    .NOTES
        Must be run as Administrator.
        Uses Interactive logon (runs only when user is logged in).
        Task settings: runs if on battery, wakes computer if needed, skips if running.
        Wrapper scripts are stored in: $env:LOCALAPPDATA\ScriptsToolkit\tasks\
    #>
    [CmdletBinding()]
    [Alias('regtask')]
    param(
        [Parameter(Mandatory)]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter()]
        [TimeSpan]$DailyTime = '09:00:00',

        [Parameter()]
        [string]$Description = 'Scheduled sync task'
    )

    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        throw "Run PowerShell as Administrator."
    }

    if (-not (Test-Path -Path $Script:CSharpRoot)) {
        throw "Project path not found: $Script:CSharpRoot"
    }

    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existingTask) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "Removed existing task '$TaskName'" -ForegroundColor Yellow
    }

    # Create wrapper script to avoid quoting issues with Task Scheduler
    $taskDir = Join-Path -Path $env:LOCALAPPDATA -ChildPath 'ScriptsToolkit\tasks'
    if (-not (Test-Path -Path $taskDir)) {
        New-Item -ItemType Directory -Path $taskDir -Force | Out-Null
    }

    $scriptPath = Join-Path -Path $taskDir -ChildPath "$TaskName.ps1"
    $scriptContent = @"
`$Host.UI.RawUI.WindowTitle = '$TaskName'
Set-Location -Path '$Script:CSharpRoot'
Write-Host "[$TaskName] Starting at `$(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host ""

dotnet run -- $Command
`$exitCode = `$LASTEXITCODE

Write-Host ""
if (`$exitCode -eq 0) {
    Write-Host "[$TaskName] Completed successfully" -ForegroundColor Green
    Write-Host "Window will close in 10 seconds..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 10
}
else {
    Write-Host "[$TaskName] Failed with exit code `$exitCode" -ForegroundColor Red
    Read-Host "Press Enter to close"
}
"@
    Set-Content -Path $scriptPath -Value $scriptContent -Force

    # Use pwsh with full path - Windows Terminal will be used if it's the default terminal
    $executable = (Get-Command -Name pwsh).Source
    $argument = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""

    $action = New-ScheduledTaskAction -Execute $executable -Argument $argument -WorkingDirectory $Script:CSharpRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) {
        $start = $start.AddDays(1)
    }

    $trigger = New-ScheduledTaskTrigger -Daily -At $start
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description $Description | Out-Null

    Write-Host "Registered '$TaskName' for $($start.ToString('HH:mm')) daily" -ForegroundColor Green
    Write-Host "  Script: $scriptPath" -ForegroundColor DarkGray
}

function Register-AllSyncTasks {
    <#
    .SYNOPSIS
        Registers all sync scheduled tasks with default settings.

    .DESCRIPTION
        Creates scheduled tasks for both Last.fm and YouTube syncs.
        Last.fm runs at 9 AM, YouTube at 10 AM (staggered to avoid overlap).

    .EXAMPLE
        regall
        Creates 'LastFmSync' task at 9 AM and 'YouTubeSync' task at 10 AM.

    .NOTES
        Must be run as Administrator.
        Removes existing tasks with the same names before creating.
    #>
    [CmdletBinding()]
    [Alias('regall')]
    param()

    Register-ScheduledSyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}



Export-ModuleMember -Function * -Alias *
