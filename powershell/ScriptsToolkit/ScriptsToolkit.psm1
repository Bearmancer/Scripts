# ScriptsToolkit Module File

class WhisperLanguageCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperLanguages }
}

class WhisperModelCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperModels }
}

$Script:RepositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Script:CSharpRoot = Join-Path $Script:RepositoryRoot 'csharp'
$Script:CSharpPublish = Join-Path $Script:CSharpRoot 'publish'
$Script:ScriptsExe = Join-Path $Script:CSharpPublish 'scripts.exe'
$Script:PythonRoot = Join-Path $Script:RepositoryRoot 'python'
$Script:LogDirectory = Join-Path $Script:RepositoryRoot 'logs'

. "$PSScriptRoot\ScriptsToolkit.Data.ps1"

#region CLI Wrappers
function Invoke-Scripts {
    <#
    .SYNOPSIS
    Invoke the C# CLI for sync, music metadata, and utilities.

    .DESCRIPTION
    Runs the compiled scripts.exe with the specified arguments. This is the main entry point
    for all C# CLI commands including sync operations, music metadata queries, and utilities.
    Falls back to dotnet run if the compiled exe is not found.

    .PARAMETER Arguments
    Arguments to pass to the C# CLI.

    .EXAMPLE
    Invoke-Scripts sync yt
    Syncs YouTube playlists to Google Sheets.

    .EXAMPLE
    scripts music search "Beethoven Symphony 5"
    Searches for music metadata using the 'scripts' alias.

    .EXAMPLE
    Invoke-Scripts --help
    Shows available CLI commands.
    #>
    [Alias('scripts')]
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    if (Test-Path $Script:ScriptsExe) {
        & $Script:ScriptsExe @Arguments
    }
    else {
        Write-Warning "scripts.exe not found. Run 'regall' to compile. Falling back to dotnet run..."
        dotnet run --project $Script:CSharpRoot -- @Arguments
    }
}

function Sync-YouTube {
    <#
    .SYNOPSIS
    Sync YouTube playlists to Google Sheets.

    .DESCRIPTION
    Fetches video metadata from YouTube playlists and syncs them to a configured Google Sheets
    spreadsheet. Tracks changes, handles rate limiting, and maintains local state cache.

    .EXAMPLE
    Sync-YouTube
    Runs a full YouTube playlist sync.

    .EXAMPLE
    syncyt --dry-run
    Preview sync without making changes.
    #>
    [Alias('syncyt')]
    [CmdletBinding()]
    param()

    Invoke-Scripts sync yt @args
}

function Sync-LastFm {
    <#
    .SYNOPSIS
    Sync Last.fm scrobbles to Google Sheets.

    .DESCRIPTION
    Fetches scrobble history from Last.fm API and syncs to a configured Google Sheets spreadsheet.
    Supports incremental sync from last known timestamp and optional date filtering.
    Always rebuilds from source to ensure latest changes are reflected.

    .PARAMETER Date
    Sync scrobbles from a specific date onwards. Format: dd/MM/yyyy (e.g., 20/01/2026).
    This will delete existing scrobbles from that date forward and re-sync them.

    .EXAMPLE
    Sync-LastFm
    Runs full Last.fm scrobble sync from last known timestamp.

    .EXAMPLE
    music 20/01/2026
    Sync scrobbles from January 20, 2026 onwards using the 'music' alias.

    .EXAMPLE
    Sync-LastFm -Date 01/12/2024
    Re-sync scrobbles from December 1, 2024.
    #>
    [Alias('music')]
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, ValueFromPipeline)]
        [string]$Date
    )

    $cliArgs = @('sync', 'lastfm')

    if ($Date) {
        try {
            $dateObj = [datetime]::ParseExact($Date, 'dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture)
            # C# CLI expects --since with yyyy/MM/dd format (use InvariantCulture to preserve slashes)
            $formattedDate = $dateObj.ToString('yyyy/MM/dd', [System.Globalization.CultureInfo]::InvariantCulture)
            $cliArgs += '--since', $formattedDate
        }
        catch {
            Write-Error -Message "Invalid date format. Expected dd/MM/yyyy (e.g., 20/01/2026). Received: '$Date'" -ErrorAction Stop
            return
        }
    }
    Invoke-Scripts @cliArgs @args
}

function Sync-All {
    <#
    .SYNOPSIS
    Sync both YouTube playlists and Last.fm scrobbles.

    .DESCRIPTION
    Runs YouTube sync followed by Last.fm sync in sequence. Useful for scheduled tasks
    or manual full syncs. Always builds from source.

    .EXAMPLE
    Sync-All
    Syncs YouTube then Last.fm.

    .EXAMPLE
    sync
    Same as above using the 'sync' alias.
    #>
    [Alias('sync')]
    [CmdletBinding()]
    param()

    $cliArgs = @('sync', 'all')

    Invoke-Scripts @cliArgs @args
}
#endregion CLI Wrappers

#region Whisper Transcription
function Invoke-Whisper {
    <#
    .SYNOPSIS
    Transcribe audio/video files using whisper-ctranslate2.

    .DESCRIPTION
    Transcribes audio or video files to SRT subtitle format using whisper-ctranslate2.
    Supports automatic language detection, multiple models, translation, and batch processing
    of directories. Skips files that already have corresponding SRT files.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Language
    Language code for transcription (e.g., 'en', 'ja', 'de'). Auto-detected if not specified.

    .PARAMETER Model
    Whisper model to use. Default is 'medium'. Options include tiny, base, small, medium,
    large-v3, distil-large-v3.5, etc.

    .PARAMETER Translate
    Translate output to English instead of transcribing in original language.

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-Whisper video.mp4
    Transcribes video.mp4 with auto language detection using medium model.

    .EXAMPLE
    whisper . -Language ja -Model large-v3
    Transcribes all media files in current directory as Japanese using large-v3.

    .EXAMPLE
    Invoke-Whisper interview.mp3 -Translate
    Transcribes and translates to English.
    #>
    [Alias('whisper')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('l')]
        [ValidateSet([WhisperLanguageCompleter])]
        [string]$Language,

        [Alias('m')]
        [ValidateSet([WhisperModelCompleter])]
        [string]$Model = 'medium',

        [Alias('t')]
        [switch]$Translate,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    begin {
        Assert-CommandExists 'whisper-ctranslate2'
        $toProcess = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
        $skipped = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    }

    process {
        $item = Get-Item $InputPath
        $files = if ($item.PSIsContainer) { Get-MediaFiles $item } else { @($item) }

        foreach ($file in $files) {
            if (Test-SrtExists $file.FullName) { $skipped.Add($file) }
            else { $toProcess.Add($file) }
        }
    }

    end {
        if ($skipped.Count -gt 0) {
            Write-Information "$(Get-Timestamp) Skipped $($skipped.Count) (SRT exists)" -InformationAction Continue
        }

        if ($toProcess.Count -eq 0) {
            Write-Information "$(Get-Timestamp) Nothing to transcribe" -InformationAction Continue
            return
        }

        $langDisplay = if ($Language) { $Language } else { 'auto' }
        Write-Information "$(Get-Timestamp) Transcribing $($toProcess.Count) file(s) | Model: $Model | Language: $langDisplay" -InformationAction Continue

        $i = 0
        foreach ($file in $toProcess) {
            $i++
            Write-Information "$(Get-Timestamp) [$i/$($toProcess.Count)] $($file.Name)" -InformationAction Continue

            $whisperArgs = @('--model', $Model, '--output_format', 'srt', '--verbose', $(if ($Quiet) { 'False' } else { 'True' }))
            if ($Language) { $whisperArgs += '--language', $Language }
            if ($Translate) { $whisperArgs += '--task', 'translate' }
            if ($ExtraArgs) { $whisperArgs += $ExtraArgs }
            $whisperArgs += $file.FullName

            $env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::UserWarning'
            try { & whisper-ctranslate2 @whisperArgs }
            finally { $env:PYTHONWARNINGS = $null }
        }

        Write-Information "$(Get-Timestamp) Completed $i file(s)" -InformationAction Continue
    }
}

function Invoke-WhisperJapanese {
    <#
    .SYNOPSIS
    Transcribe Japanese audio/video content.

    .DESCRIPTION
    Wrapper for Invoke-Whisper with Japanese language pre-configured. Uses the default
    medium model which provides good accuracy for Japanese.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Translate
    Translate Japanese audio to English subtitles.

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-WhisperJapanese anime.mkv
    Transcribes Japanese audio to Japanese subtitles.

    .EXAMPLE
    wpj video.mp4 -Translate
    Transcribes Japanese and translates to English using 'wpj' alias.
    #>
    [Alias('wpj')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('t')]
        [switch]$Translate,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    process {
        Invoke-Whisper $InputPath -Language ja -Translate:$Translate -Quiet:$Quiet @ExtraArgs
    }
}
#endregion Whisper Transcription

#region YouTube Utilities
function Save-YouTubeDownload {
    <#
    .SYNOPSIS
    Download YouTube videos and optionally transcribe them.

    .DESCRIPTION
    Downloads YouTube videos using yt-dlp and optionally transcribes them using Whisper.
    By default, downloads are transcribed with Invoke-Whisper unless -NoTranscribe is specified.

    .PARAMETER Urls
    One or more YouTube URLs to download.

    .PARAMETER NoTranscribe
    Skip automatic transcription after download.

    .PARAMETER Translate
    Translate transcription to English (for non-English content).

    .EXAMPLE
    Save-YouTubeDownload 'https://youtube.com/watch?v=dQw4w9WgXcQ'
    Downloads and transcribes the video.

    .EXAMPLE
    ytdl 'https://youtube.com/watch?v=abc123' -NoTranscribe
    Downloads without transcription.

    .EXAMPLE
    Save-YouTubeDownload $url1, $url2 -Translate
    Downloads multiple videos and translates non-English audio.
    #>
    [Alias('ytdl')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]]$Urls,

        [Alias('n')]
        [switch]$NoTranscribe,

        [Alias('t')]
        [switch]$Translate
    )

    foreach ($url in $Urls) {
        Write-Information "$(Get-Timestamp) Downloading: $url" -InformationAction Continue

        $filePath = & yt-dlp --print filename $url --windows-filenames -o '%(title)s.%(ext)s'
        if (Test-Path $filePath) { Remove-Item $filePath -Force }
        & yt-dlp $url --windows-filenames -o '%(title)s.%(ext)s'

        if ($NoTranscribe -or -not (Test-Path $filePath)) { continue }

        if ($Translate) { Invoke-Whisper $filePath -Translate }
        else { Invoke-Whisper $filePath -Language en -Model 'distil-large-v3.5' }
    }
}
#endregion YouTube Utilities

#region Scheduled Tasks
function Register-LastFmSyncTask {
    <#
    .SYNOPSIS
    Register Last.fm sync scheduled task.

    .DESCRIPTION
    Creates a Windows Scheduled Task for Last.fm sync at 09:00 daily.
    Catches up missed runs automatically.

    .EXAMPLE
    Register-LastFmSyncTask
    Registers Last.fm sync at 09:00 AM.
    #>
    [Alias('reglfm')]
    [CmdletBinding()]
    param()

    Register-SyncTaskInternal -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00' -Description 'Sync Last.fm scrobbles (09:00 daily)'
}

function Register-YouTubeSyncTask {
    <#
    .SYNOPSIS
    Register YouTube sync scheduled task.

    .DESCRIPTION
    Creates a Windows Scheduled Task for YouTube sync at 09:00 daily.
    Catches up missed runs automatically.

    .EXAMPLE
    Register-YouTubeSyncTask
    Registers YouTube sync at 09:00 AM.
    #>
    [Alias('regyt')]
    [CmdletBinding()]
    param()

    Register-SyncTaskInternal -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '09:00' -Description 'Sync YouTube playlists (09:00 daily)'
}

function Register-StateCommitTask {
    <#
    .SYNOPSIS
    Register scheduled task to auto-commit state and log changes.

    .DESCRIPTION
    Creates a Windows Scheduled Task that runs daily at 09:10 to check for
    uncommitted changes in state/ and logs/ directories, then commits and
    pushes using gh CLI with the Bearmancer account. Pauses on failure.

    .PARAMETER DailyTime
    Time of day to run. Default is 09:10.

    .PARAMETER GitAccount
    GitHub account for commits. Default is 'Bearmancer'.

    .EXAMPLE
    Register-StateCommitTask
    Registers state commit task at 09:10 AM.
    #>
    [Alias('regcommit')]
    [CmdletBinding()]
    param(
        [TimeSpan]$DailyTime = '09:10',
        [string]$GitAccount = 'Bearmancer'
    )

    $taskName = 'StateAutoCommit'

    $taskScript = @"
`$ErrorActionPreference = 'Continue'
Set-Location '$Script:RepositoryRoot'

try {
    # Check for uncommitted changes in state and logs directories
    `$status = git status --porcelain -- state/ logs/
    if (-not `$status) {
        Write-Host 'No changes to commit'
        exit 0
    }

    # Count changes by directory
    `$stateChanges = (`$status | Where-Object { `$_ -match 'state/' }).Count
    `$logChanges = (`$status | Where-Object { `$_ -match 'logs/' }).Count

    # Stage all data file changes
    git add state/ logs/

    # Build dynamic commit message
    `$parts = @()
    if (`$stateChanges -gt 0) { `$parts += "state (`$stateChanges)" }
    if (`$logChanges -gt 0) { `$parts += "logs (`$logChanges)" }
    `$summary = `$parts -join ', '
    `$timestamp = Get-Date -Format 'yyyy/MM/dd HH:mm'
    `$message = "Auto-sync [`$summary] `$timestamp"

    git commit -m `$message

    # Push using gh (ensures correct account: $GitAccount)
    `$authStatus = gh auth status --hostname github.com 2>&1
    if (`$LASTEXITCODE -ne 0) {
        Write-Host 'GitHub auth required. Opening browser...'
        gh auth login --hostname github.com --git-protocol https --web
    }

    git push origin main
    Write-Host "Pushed: `$message"
}
catch {
    Write-Host "ERROR: `$_" -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($taskScript))
    $pwsh = (Get-Command pwsh).Source

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -EncodedCommand $encoded" -WorkingDirectory $Script:RepositoryRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $dailyTrigger = New-ScheduledTaskTrigger -Daily -At $start
    $description = "Auto-commit state/logs changes daily at $($start.ToString('HH:mm')) (Account: $GitAccount)"

    $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction Ignore
    if ($existing) {
        Set-ScheduledTask -TaskName $taskName -Action $action -Trigger $dailyTrigger -Settings $settings | Out-Null
        Write-Information "$(Get-Timestamp) Updated: $taskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
    else {
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $dailyTrigger -Settings $settings -Description $description | Out-Null
        Write-Information "$(Get-Timestamp) Registered: $taskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
}

function Register-AllSyncTasks {
    <#
    .SYNOPSIS
    Compile scripts.exe and register all sync scheduled tasks at 9 AM.

    .DESCRIPTION
    Compiles the C# project to scripts.exe, then registers all three tasks at 9 AM:
    - LastFmSync:      09:00 (runs first)
    - YouTubeSync:     09:00 (same time as LastFm)
    - StateAutoCommit: 09:10 (after syncs complete)

    Tasks use StartWhenAvailable to catch up missed runs. All tasks pause on failure.

    .EXAMPLE
    Register-AllSyncTasks
    Compiles and registers all sync tasks.

    .EXAMPLE
    regall
    Same as above.
    #>
    [Alias('regall')]
    [CmdletBinding()]
    param()

    Assert-NetworkAvailable
    Build-Scripts
    Register-LastFmSyncTask
    Register-YouTubeSyncTask
    Register-StateCommitTask
}

function Build-Scripts {
    <#
    .SYNOPSIS
    Compile the C# project to scripts.exe.

    .DESCRIPTION
    Runs dotnet publish to create a single-file executable at csharp/publish/scripts.exe.

    .EXAMPLE
    Build-Scripts
    Compiles the C# project.
    #>
    [CmdletBinding()]
    param()

    Write-Information "$(Get-Timestamp) Compiling scripts.exe..." -InformationAction Continue
    $result = dotnet publish $Script:CSharpRoot -c Release -r win-x64 -o $Script:CSharpPublish 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Compilation failed: $result"
        return
    }
    Write-Information "$(Get-Timestamp) Compiled: $Script:ScriptsExe" -InformationAction Continue
}

function Unregister-AllSyncTasks {
    <#
    .SYNOPSIS
    Unregister all sync scheduled tasks.

    .DESCRIPTION
    Removes LastFmSync, YouTubeSync, and StateAutoCommit scheduled tasks.

    .EXAMPLE
    Unregister-AllSyncTasks
    Removes all registered sync tasks.

    .EXAMPLE
    unreg
    Same as above.
    #>
    [Alias('unreg')]
    [CmdletBinding()]
    param()

    @('LastFmSync', 'YouTubeSync', 'StateAutoCommit') | ForEach-Object {
        $task = Get-ScheduledTask -TaskName $_ -ErrorAction Ignore
        if ($task) {
            Unregister-ScheduledTask -TaskName $_ -Confirm:$false
            Write-Information "$(Get-Timestamp) Removed: $_" -InformationAction Continue
        }
    }
}

function Register-SyncTaskInternal {
    <#
    .SYNOPSIS
    Internal helper to register or update a sync scheduled task.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskName,
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][TimeSpan]$DailyTime,
        [Parameter(Mandatory)][string]$Description
    )

    if (-not (Test-Path $Script:ScriptsExe)) {
        throw "scripts.exe not found: $Script:ScriptsExe. Run Build-Scripts first."
    }

    $taskScript = @"
`$ErrorActionPreference = 'Continue'
try {
    & '$Script:ScriptsExe' $Command
    if (`$LASTEXITCODE -ne 0) {
        Write-Host 'Sync failed with exit code:' `$LASTEXITCODE -ForegroundColor Red
        Read-Host 'Press Enter to close'
    }
}
catch {
    Write-Host "ERROR: `$_" -ForegroundColor Red
    Read-Host 'Press Enter to close'
}
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($taskScript))
    $pwsh = (Get-Command pwsh).Source

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -EncodedCommand $encoded" -WorkingDirectory $Script:RepositoryRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $dailyTrigger = New-ScheduledTaskTrigger -Daily -At $start

    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existing) {
        Set-ScheduledTask -TaskName $TaskName -Action $action -Trigger $dailyTrigger -Settings $settings | Out-Null
        Write-Information "$(Get-Timestamp) Updated: $TaskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
    else {
        Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $dailyTrigger -Settings $settings -Description $Description | Out-Null
        Write-Information "$(Get-Timestamp) Registered: $TaskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
}
#endregion Scheduled Tasks

#region Log Files
function Get-SyncLog {
    <#
    .SYNOPSIS
    Get sync session logs in a formatted table.

    .DESCRIPTION
    Displays sync session summaries from JSONL log files. Use -Verbose for entry-level
    detail showing individual log events.

    .PARAMETER Service
    Filter by service: 'youtube', 'lastfm', or 'all' (default).

    .PARAMETER Size
    Number of rows to display. Default is 10.

    .PARAMETER Level
    Filter entries by log level. Applies to both entry and session aggregation.

    .PARAMETER SessionId
    Filter to a specific session by ID prefix (first 8 characters).

    .EXAMPLE
    Get-SyncLog
    Shows the 10 most recent sync sessions for all services.

    .EXAMPLE
    viewlog -Service youtube -Size 20
    Shows the 20 most recent YouTube sync sessions.

    .EXAMPLE
    Get-SyncLog -SessionId 'a1b2c3d4'
    Shows details for a specific session.

    .EXAMPLE
    viewlog -Verbose -Size 50
    Shows 50 recent entries in table format with wrapped details.

    .EXAMPLE
    viewlog -Verbose -Format List -Level Error
    Shows error-level entries in detailed list format.

    .EXAMPLE
    viewlog -Verbose -Service lastfm
    Shows detailed Last.fm sync entries.
    #>
    [Alias('viewlog')]
    [CmdletBinding()]
    param(
        [ValidateSet('youtube', 'lastfm', 'all')]
        [string]$Service = 'all',

        [Alias('n')]
        [int]$Size = 10,

        [ValidateSet('Table', 'List')]
        [string]$Format = 'Table',

        [switch]$Descending,

        [string]$SessionId,

        [ValidateSet('All', 'Debug', 'Info', 'Success', 'Warning', 'Error', 'Fatal')]
        [string]$Level = 'All'
    )

    $logFiles = @{
        youtube = Join-Path $Script:LogDirectory 'youtube.jsonl'
        lastfm  = Join-Path $Script:LogDirectory 'lastfm.jsonl'
    }
    $services = if ($Service -eq 'all') { @('youtube', 'lastfm') } else { @($Service) }

    $allEntries = Get-SyncLogEntriesFromFiles $services $logFiles

    if (-not $allEntries) {
        Write-Information "$(Get-Timestamp) No log entries found" -InformationAction Continue
        return
    }
    if ($SessionId) { $allEntries = @($allEntries | Where-Object { $_.SessionId -and $_.SessionId -like "$SessionId*" }) }

    $allEntries = Add-SyncLogParsedTimestamp $allEntries

    if ($Level -ne 'All') {
        $allEntries = @($allEntries | Where-Object { $_.Level -eq $Level })
    }

    $sortDescending = $PSBoundParameters['Descending'] ?? $true
    $verbose = $VerbosePreference -eq 'Continue' -or $PSBoundParameters.ContainsKey('Verbose')

    if ($verbose) {
        $entriesSorted = Select-SyncLogRows -Items $allEntries -Size $Size -Descending:$sortDescending -SortProperty 'ParsedTimestamp'
        $entriesOutput = ConvertTo-SyncLogEntryOutput $entriesSorted

        if ($Format -eq 'List') {
            $entriesOutput | Format-List -Property Time, Service, Level, Event, Session, Details
        }
        else {
            $entriesOutput | Format-Table -Property Time, Service, Level, Event, Session, Details -AutoSize -Wrap
        }
        return
    }

    $sessions = ConvertTo-SyncLogSessionOutput $allEntries

    if (-not $sessions) {
        Write-Error "$(Get-Timestamp) No sessions found"
        return
    }

    $display = Select-SyncLogRows -Items $sessions -Size $Size -Descending:$sortDescending -SortProperty '_Sort'
    $display | Format-Table -Property Session, Service, Time, Status, Summary -AutoSize -Wrap
}

#region SyncLog Helpers
function Get-SyncLogEntriesFromFiles {
    param(
        [string[]]$Services,
        [hashtable]$LogFiles
    )

    foreach ($svc in $Services) {
        $path = $LogFiles[$svc]
        if (Test-Path $path) {
            Get-Content $path | ForEach-Object {
                $entry = $_ | ConvertFrom-Json
                $entry | Add-Member -NotePropertyName 'Service' -NotePropertyValue $svc -Force -PassThru
            }
        }
    }
}

function Add-SyncLogParsedTimestamp {
    param([object[]]$Entries)

    $Entries | Where-Object { $_.SessionId -and $_.Timestamp } | ForEach-Object {
        $parsed = try { [datetime]::ParseExact($_.Timestamp, 'yyyy-MM-dd HH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture) } catch { try { [datetime]$_.Timestamp } catch { $null } }
        $_ | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsed -Force -PassThru
    } | Where-Object ParsedTimestamp
}

function Get-SyncLogEntryDetails {
    param($Entry)

    $entryData = $Entry.Data
    if (-not $entryData) { return '' }

    $text = $entryData.PSObject?.Properties['Text']?.Value
    if ($text) { return $text }

    $parts = $entryData.PSObject.Properties | Where-Object { $_.Name -notin 'Service', 'ProcessId' } | ForEach-Object {
        $val = if ($_.Value -is [array]) { $_.Value -join ', ' } else { $_.Value }
        "$($_.Name): $val"
    }

    $parts -join "`n"
}

function ConvertTo-SyncLogEntryOutput {
    param([object[]]$Entries)

    $Entries | ForEach-Object {
        [PSCustomObject]@{
            Time    = $_.ParsedTimestamp.ToString('yyyy/MM/dd HH:mm:ss')
            Service = $_.Service
            Level   = $_.Level
            Event   = $_.Event
            Session = $_.SessionId?.Substring(0, 8) ?? ''
            Details = Get-SyncLogEntryDetails $_
        }
    }
}

function ConvertTo-SyncLogSessionOutput {
    param([object[]]$Entries)

    $Entries | Group-Object SessionId | ForEach-Object {
        $group = @($_.Group | Sort-Object ParsedTimestamp)
        $first = $group[0]
        if (-not $first -or -not $first.SessionId) { return }

        $startTime = $first.ParsedTimestamp
        # PSScriptAnalyzer doesn't recognize usage in if-expressions
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'endEvent')]
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'endData')]
        $endEvent = @($group | Where-Object { $_.Event -eq 'SessionEnd' })[0]
        $endData = if ($endEvent) { $endEvent.Data } else { $null }
        $endTime = if ($endEvent) { $endEvent.ParsedTimestamp } else { $null }
        $summary = if ($endData) { $endData.Summary } else { '-' }

        $hasError = [bool]($group | Where-Object { $_.Level -eq 'Error' })
        $interrupted = [bool]($group | Where-Object { $_.Event -eq 'SessionInterrupted' })
        $crashed = [bool]($group | Where-Object { $_.Event -eq 'SessionCrashed' })
        $endStatus = if ($endData) { $endData.Status } else { $null }

        $status = switch ($true) {
            { -not $endTime -and ((Get-Date) - $startTime).TotalHours -lt 2 } { 'Running'; break }
            { -not $endTime } { 'Crashed'; break }
            { $crashed } { 'Crashed'; break }
            { $interrupted } { 'Interrupted'; break }
            { $hasError } { 'Failed'; break }
            { $endStatus } { $endStatus; break }
            default { 'Completed' }
        }

        [PSCustomObject]@{
            Session   = $first.SessionId.Substring(0, 8)
            Service   = $first.Service
            Time      = $startTime.ToString('yyyy/MM/dd HH:mm:ss')
            Status    = $status
            Summary   = $summary
            StartTime = $startTime
            EndTime   = $endTime
            Duration  = if ($endTime) { ($endTime - $startTime).ToString() } else { '-' }
            Events    = $group.Count
            HasError  = $hasError
            _Sort     = $startTime
        }
    }
}

function Select-SyncLogRows {
    param(
        [object[]]$Items,
        [int]$Size,
        [switch]$Descending,
        [string]$SortProperty
    )

    $sorted = $Items | Sort-Object -Property $SortProperty -Descending:$Descending
    $sorted | Select-Object -First $Size
}
#endregion SyncLog Helpers
#endregion Log Files

#region Utilities
function Invoke-Propolis {
    <#
    .SYNOPSIS
    Optimize images using Propolis.

    .DESCRIPTION
    Runs the Propolis image optimizer on a directory. Propolis compresses images
    while maintaining quality.

    .PARAMETER Directory
    Directory containing images to optimize. Defaults to current directory.

    .EXAMPLE
    Invoke-Propolis
    Optimizes images in the current directory.

    .EXAMPLE
    propolis C:\Photos\Vacation
    Optimizes images in the specified directory.
    #>
    [Alias('propolis')]
    [CmdletBinding()]
    param([System.IO.DirectoryInfo]$Directory = (Get-Item .))

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}

function Get-ScriptsToolkitCommand {
    <#
    .SYNOPSIS
    List all available ScriptsToolkit commands and their aliases.

    .DESCRIPTION
    Displays a formatted table of all exported functions, their aliases, and descriptions
    from the ScriptsToolkit module.

    .EXAMPLE
    Get-ScriptsToolkitCommand
    Lists all commands in the toolkit.

    .EXAMPLE
    stk
    Same as above using the alias.
    #>
    [Alias('stk')]
    [CmdletBinding()]
    param()

    $module = Get-Module ScriptsToolkit
    $module.ExportedFunctions.Keys | Sort-Object | ForEach-Object {
        $func = $_
        $aliases = ($module.ExportedAliases.Values | Where-Object { $_.Definition -eq $func }).Name -join ', '
        $help = Get-Help $func -ErrorAction Ignore
        $synopsis = if ($help.Synopsis) { $help.Synopsis.Trim() } else { '' }
        [PSCustomObject]@{ Alias = $aliases; Function = $func; Description = $synopsis }
    } | Format-Table -AutoSize
}
#endregion Utilities